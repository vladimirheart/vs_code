using System;
using System.Globalization;
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
                log.Info(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=TABLE_MANUAL_PRINT_STARTED orderId={0} orderNumber={1}",
                        order.Id,
                        order.Number));

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
    }
}
