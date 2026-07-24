using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using IikoFront.OrderQrPlugin.Configuration;
using Resto.Front.Api;
using Resto.Front.Api.Data.Common;
using Resto.Front.Api.Data.Kitchen;
using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.Data.Print;
using Resto.Front.Api.Exceptions;

namespace IikoFront.OrderQrPlugin.Printing
{
    public sealed class CookingStartAutoPrintCoordinator
    {
        private const string ExternalDataKey = "IikoFront.OrderQrPlugin.CookingStartPrinted";

        private readonly IOperationService operations;
        private readonly PluginSettings settings;
        private readonly ILog log;
        private readonly ConcurrentDictionary<Guid, byte> inFlightOrPrinted =
            new ConcurrentDictionary<Guid, byte>();

        public CookingStartAutoPrintCoordinator(
            IOperationService operations,
            PluginSettings settings,
            ILog log)
        {
            this.operations = operations;
            this.settings = settings;
            this.log = log;
        }

        public IDisposable Subscribe(INotificationService notifications)
        {
            return notifications
                .GetKitchenOrderChanged(false)
                .Subscribe(new ActionObserver<EntityChangedEventArgs<IKitchenOrder>>(OnKitchenOrderChanged));
        }

        private void OnKitchenOrderChanged(EntityChangedEventArgs<IKitchenOrder> args)
        {
            if (!settings.Enabled || !settings.PrintOnCookingStart || args.Entity == null)
            {
                return;
            }

            var kitchenOrder = args.Entity;
            var kitchenOrderId = kitchenOrder.BaseOrderId;

            if (kitchenOrder.ProcessingStatus != KitchenOrderProcessingStatus.CookingStarted)
            {
                return;
            }

            if (isAlreadyPrinted(kitchenOrder))
            {
                return;
            }

            if (!inFlightOrPrinted.TryAdd(kitchenOrderId, 0))
            {
                return;
            }

            log.Info(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "event=COOKING_START_PRINT_SCHEDULED orderId={0} initialDelayMs={1} retryDelayMs={2} maxAttempts={3}",
                    kitchenOrderId,
                    settings.CookingStartInitialDelayMs,
                    settings.CookingStartRetryDelayMs,
                    settings.CookingStartMaxAttempts));

            Task.Run(() => ProcessCookingStartPrintAsync(kitchenOrder.BaseOrderId));
        }

        private bool isAlreadyPrinted(IKitchenOrder kitchenOrder)
        {
            if (inFlightOrPrinted.ContainsKey(kitchenOrder.BaseOrderId))
            {
                return true;
            }

            return operations.TryGetKitchenOrderExternalDataByKey(kitchenOrder, ExternalDataKey) != null;
        }

        private async Task ProcessCookingStartPrintAsync(Guid orderId)
        {
            try
            {
                if (settings.CookingStartInitialDelayMs > 0)
                {
                    await Task.Delay(settings.CookingStartInitialDelayMs).ConfigureAwait(false);
                }

                for (var attempt = 1; attempt <= settings.CookingStartMaxAttempts; attempt++)
                {
                    log.Info(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "event=COOKING_START_PRINT_ATTEMPT orderId={0} attempt={1} maxAttempts={2}",
                            orderId,
                            attempt,
                            settings.CookingStartMaxAttempts));

                    try
                    {
                        if (TryPrint(orderId))
                        {
                            return;
                        }

                        inFlightOrPrinted.TryRemove(orderId, out _);
                        return;
                    }
                    catch (EntityAlreadyInUseException ex) when (attempt < settings.CookingStartMaxAttempts)
                    {
                        log.Warn(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "event=COOKING_START_PRINT_RETRY orderId={0} attempt={1} delayMs={2} reason=ENTITY_ALREADY_IN_USE lockedTerminal={3} lockedUser={4}",
                                orderId,
                                attempt,
                                settings.CookingStartRetryDelayMs,
                                normalizeLogValue(ex.LockedTerminalName),
                                normalizeLogValue(ex.LockedUser?.Code)));
                        await Task.Delay(settings.CookingStartRetryDelayMs).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        inFlightOrPrinted.TryRemove(orderId, out _);
                        log.Error(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "event=COOKING_START_PRINT_FAILED orderId={0}",
                                orderId),
                            ex);
                        return;
                    }
                }

                inFlightOrPrinted.TryRemove(orderId, out _);
                log.Error(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=COOKING_START_PRINT_FAILED orderId={0} reason=ENTITY_ALREADY_IN_USE_RETRY_LIMIT maxAttempts={1}",
                        orderId,
                        settings.CookingStartMaxAttempts));
            }
            catch (Exception ex)
            {
                inFlightOrPrinted.TryRemove(orderId, out _);
                log.Error(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=COOKING_START_PRINT_FAILED orderId={0}",
                        orderId),
                    ex);
            }
        }

        private bool TryPrint(Guid orderId)
        {
            var order = operations.GetOrderById(orderId);
            if (order == null)
            {
                log.Warn(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=COOKING_START_PRINT_SKIPPED reason=ORDER_NOT_FOUND orderId={0}",
                        orderId));
                return false;
            }

            if (order is IDeliveryOrder deliveryOrder)
            {
                if (!settings.PrintDeliveryBillOnCookingStart)
                {
                    log.Info(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "event=COOKING_START_PRINT_SKIPPED reason=DELIVERY_DISABLED orderId={0} orderNumber={1}",
                            order.Id,
                            order.Number));
                    return false;
                }

                log.Info(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=COOKING_START_PRINT_STARTED mode=DELIVERY orderId={0} orderNumber={1}",
                        order.Id,
                        order.Number));

                operations.PrintDeliveryBill(deliveryOrder, operations.GetDefaultCredentials());
                markAsPrinted(order, "DELIVERY");

                log.Info(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=COOKING_START_PRINT_REQUESTED mode=DELIVERY orderId={0} orderNumber={1}",
                        order.Id,
                        order.Number));
                return true;
            }

            if (!settings.PrintTableBillOnCookingStart)
            {
                log.Info(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=COOKING_START_PRINT_SKIPPED reason=TABLE_DISABLED orderId={0} orderNumber={1}",
                        order.Id,
                        order.Number));
                return false;
            }

            log.Info(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "event=COOKING_START_PRINT_STARTED mode=TABLE orderId={0} orderNumber={1}",
                    order.Id,
                    order.Number));

            operations.PrintBillCheque(order, operations.GetDefaultCredentials(), PrinterSelectionMode.Default);
            markAsPrinted(order, "TABLE");

            log.Info(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "event=COOKING_START_PRINT_REQUESTED mode=TABLE orderId={0} orderNumber={1}",
                    order.Id,
                    order.Number));
            return true;
        }

        private void markAsPrinted(IOrder order, string mode)
        {
            var value = string.Format(
                CultureInfo.InvariantCulture,
                "{0}|{1:O}",
                mode,
                DateTimeOffset.Now);

            try
            {
                var kitchenOrder = operations.TryGetKitchenOrderByOrder(order);
                if (kitchenOrder != null)
                {
                    operations.AddOrUpdateKitchenOrderExternalData(
                        kitchenOrder,
                        ExternalDataKey,
                        new ExternalDataItem(value, false));
                }
            }
            catch (Exception ex)
            {
                log.Warn(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=COOKING_START_PRINT_MARK_FAILED orderId={0} mode={1} errorType={2} errorMessage={3}",
                        order.Id,
                        mode,
                        ex.GetType().Name,
                        normalizeLogValue(ex.Message)));
            }
        }

        private static string normalizeLogValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Replace(' ', '_');
        }

        private sealed class ActionObserver<T> : IObserver<T>
        {
            private readonly Action<T> onNext;

            public ActionObserver(Action<T> onNext)
            {
                this.onNext = onNext;
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(T value)
            {
                onNext(value);
            }
        }
    }
}
