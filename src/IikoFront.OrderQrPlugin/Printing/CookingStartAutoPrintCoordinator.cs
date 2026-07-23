using System;
using System.Collections.Concurrent;
using System.Globalization;
using IikoFront.OrderQrPlugin.Configuration;
using Resto.Front.Api;
using Resto.Front.Api.Data.Common;
using Resto.Front.Api.Data.Kitchen;
using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.Data.Print;

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

            try
            {
                var order = operations.GetOrderById(kitchenOrder.BaseOrderId);
                if (order == null)
                {
                    log.Warn(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "event=COOKING_START_PRINT_SKIPPED reason=ORDER_NOT_FOUND orderId={0}",
                            kitchenOrder.BaseOrderId));
                    inFlightOrPrinted.TryRemove(kitchenOrderId, out _);
                    return;
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
                        inFlightOrPrinted.TryRemove(kitchenOrderId, out _);
                        return;
                    }

                    log.Info(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "event=COOKING_START_PRINT_STARTED mode=DELIVERY orderId={0} orderNumber={1}",
                            order.Id,
                            order.Number));

                    operations.PrintDeliveryBill(deliveryOrder, operations.GetDefaultCredentials());
                    markAsPrinted(kitchenOrder, "DELIVERY");

                    log.Info(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "event=COOKING_START_PRINT_REQUESTED mode=DELIVERY orderId={0} orderNumber={1}",
                            order.Id,
                            order.Number));
                    return;
                }

                if (!settings.PrintTableBillOnCookingStart)
                {
                    log.Info(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "event=COOKING_START_PRINT_SKIPPED reason=TABLE_DISABLED orderId={0} orderNumber={1}",
                            order.Id,
                            order.Number));
                    inFlightOrPrinted.TryRemove(kitchenOrderId, out _);
                    return;
                }

                log.Info(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=COOKING_START_PRINT_STARTED mode=TABLE orderId={0} orderNumber={1}",
                        order.Id,
                        order.Number));

                operations.PrintBillCheque(order, operations.GetDefaultCredentials(), PrinterSelectionMode.Default);
                markAsPrinted(kitchenOrder, "TABLE");

                log.Info(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=COOKING_START_PRINT_REQUESTED mode=TABLE orderId={0} orderNumber={1}",
                        order.Id,
                        order.Number));
            }
            catch (Exception ex)
            {
                inFlightOrPrinted.TryRemove(kitchenOrderId, out _);
                log.Error(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=COOKING_START_PRINT_FAILED orderId={0}",
                        kitchenOrder.BaseOrderId),
                    ex);
            }
        }

        private bool isAlreadyPrinted(IKitchenOrder kitchenOrder)
        {
            if (inFlightOrPrinted.ContainsKey(kitchenOrder.BaseOrderId))
            {
                return true;
            }

            return operations.TryGetKitchenOrderExternalDataByKey(kitchenOrder, ExternalDataKey) != null;
        }

        private void markAsPrinted(IKitchenOrder kitchenOrder, string mode)
        {
            var value = string.Format(
                CultureInfo.InvariantCulture,
                "{0}|{1:O}",
                mode,
                DateTimeOffset.Now);

            operations.AddOrUpdateKitchenOrderExternalData(
                kitchenOrder,
                ExternalDataKey,
                new ExternalDataItem(value, false));
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
