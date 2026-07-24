using System;
using System.Globalization;
using System.Linq;
using Resto.Front.Api;
using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.Data.Print;
using Resto.Front.Api.Exceptions;
using Resto.Front.Api.UI;

namespace IikoFront.OrderQrPlugin.Printing
{
    public sealed class TableOrderManualPrintButton
    {
        private const string ButtonCaption = "QR-счет";
        private const string PopupTitle = "Печать QR-счета";
        private const string OkButtonText = "OK";

        private readonly ILog log;

        public TableOrderManualPrintButton(ILog log)
        {
            this.log = log;
        }

        public IDisposable Subscribe(IOperationService operations)
        {
            return operations.AddButtonToOrderEditScreen(
                ButtonCaption,
                context => OnButtonClick(context.Item1, context.Item2, context.Item3),
                null);
        }

        private void OnButtonClick(IOrder order, IOperationService operations, IViewManager viewManager)
        {
            if (order == null)
            {
                return;
            }

            try
            {
                var activeItems = order.Items
                    .Where(item => item != null && !item.Deleted)
                    .ToList();

                if (activeItems.Count == 0)
                {
                    log.Info(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "event=TABLE_MANUAL_PRINT_SKIPPED reason=EMPTY_ORDER orderId={0} orderNumber={1}",
                            order.Id,
                            order.Number));

                    viewManager.ShowErrorPopup(
                        "В заказе нет позиций для печати гостевого счета.",
                        OkButtonText);
                    return;
                }

                log.Info(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=TABLE_MANUAL_PRINT_STARTED orderId={0} orderNumber={1}",
                        order.Id,
                        order.Number));

                PrintMissingKitchenItems(order, operations);
                operations.PrintBillCheque(order, operations.GetDefaultCredentials(), PrinterSelectionMode.Default);

                log.Info(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=TABLE_MANUAL_PRINT_REQUESTED orderId={0} orderNumber={1}",
                        order.Id,
                        order.Number));

                viewManager.ShowOkPopup(
                    PopupTitle,
                    "Гостевой счет с QR отправлен на печать.",
                    OkButtonText);
            }
            catch (ConstraintViolationException ex) when (IsClosedOrderViolation(order, ex))
            {
                log.Info(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=TABLE_MANUAL_PRINT_SKIPPED reason=ORDER_CLOSED orderId={0} orderNumber={1} status={2}",
                        order.Id,
                        order.Number,
                        order.Status));

                viewManager.ShowErrorPopup(
                    "Заказ уже закрыт. Откройте актуальный заказ и повторите печать.",
                    OkButtonText);
            }
            catch (ConstraintViolationException ex) when (IsEmptyOrderViolation(ex))
            {
                log.Info(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=TABLE_MANUAL_PRINT_SKIPPED reason=EMPTY_ORDER orderId={0} orderNumber={1}",
                        order.Id,
                        order.Number));

                viewManager.ShowErrorPopup(
                    "iikoFront считает заказ пустым для печати гостевого счета. Проверьте, что в заказе есть позиции.",
                    OkButtonText);
            }
            catch (ConstraintViolationException ex) when (IsNonPrintedItemsViolation(ex))
            {
                log.Info(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=TABLE_MANUAL_PRINT_SKIPPED reason=NON_PRINTED_ITEMS orderId={0} orderNumber={1}",
                        order.Id,
                        order.Number));

                viewManager.ShowErrorPopup(
                    "В заказе остались неотпечатанные позиции. Сначала отправьте позиции на сервисную печать, затем повторите QR-счет.",
                    OkButtonText);
            }
            catch (Exception ex)
            {
                log.Error(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=TABLE_MANUAL_PRINT_FAILED orderId={0}",
                        order.Id),
                    ex);

                viewManager.ShowErrorPopup(
                    "Не удалось отправить гостевой счет с QR на печать. Подробности есть в логе плагина.",
                    OkButtonText);
            }
        }

        private void PrintMissingKitchenItems(IOrder order, IOperationService operations)
        {
            var newCookingItems = order.Items
                .OfType<IOrderCookingItem>()
                .Where(item => !item.Deleted && item.Status == OrderItemStatus.Added)
                .ToList();

            if (newCookingItems.Count == 0)
            {
                return;
            }

            log.Info(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "event=TABLE_MANUAL_KITCHEN_PRINT_STARTED orderId={0} orderNumber={1} items={2}",
                    order.Id,
                    order.Number,
                    newCookingItems.Count));

            operations.PrintOrderItems(order, newCookingItems, operations.GetDefaultCredentials());

            log.Info(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "event=TABLE_MANUAL_KITCHEN_PRINT_REQUESTED orderId={0} orderNumber={1} items={2}",
                    order.Id,
                    order.Number,
                    newCookingItems.Count));
        }

        private static bool IsClosedOrderViolation(IOrder order, ConstraintViolationException exception)
        {
            if (order.Status == OrderStatus.Closed || order.Status == OrderStatus.Deleted)
            {
                return true;
            }

            var message = exception?.Message;
            return !string.IsNullOrWhiteSpace(message)
                && (message.IndexOf("Closed", StringComparison.OrdinalIgnoreCase) >= 0
                    || message.IndexOf("Р·Р°РєСЂС‹С‚", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsEmptyOrderViolation(ConstraintViolationException exception)
        {
            var message = exception?.Message;
            return !string.IsNullOrWhiteSpace(message)
                && message.IndexOf("empty order", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsNonPrintedItemsViolation(ConstraintViolationException exception)
        {
            var message = exception?.Message;
            return !string.IsNullOrWhiteSpace(message)
                && message.IndexOf("non-printed items", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
