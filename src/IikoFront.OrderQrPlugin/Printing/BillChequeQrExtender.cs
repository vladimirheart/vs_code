using System;
using System.Text;
using IikoFront.OrderQrPlugin.Configuration;
using IikoFront.OrderQrPlugin.Extraction;
using IikoFront.OrderQrPlugin.Logging;
using IikoFront.OrderQrPlugin.Payload;
using Resto.Front.Api;
using Resto.Front.Api.Data.Cheques;

namespace IikoFront.OrderQrPlugin.Printing
{
    public sealed class BillChequeQrExtender
    {
        private readonly IOperationService operations;
        private readonly PluginSettings settings;
        private readonly OrderDataExtractor orderDataExtractor;
        private readonly OrderPayloadBuilder payloadBuilder;
        private readonly QrMarkupFactory qrMarkupFactory;
        private readonly PrintAttemptLogger logger;

        public BillChequeQrExtender(
            IOperationService operations,
            PluginSettings settings,
            OrderDataExtractor orderDataExtractor,
            OrderPayloadBuilder payloadBuilder,
            QrMarkupFactory qrMarkupFactory,
            PrintAttemptLogger logger)
        {
            this.operations = operations;
            this.settings = settings;
            this.orderDataExtractor = orderDataExtractor;
            this.payloadBuilder = payloadBuilder;
            this.qrMarkupFactory = qrMarkupFactory;
            this.logger = logger;
        }

        public ChequeExtensions OnBillChequePrinting(Guid orderId)
        {
            if (!settings.Enabled || !settings.PrintOnGuestBill)
            {
                return null;
            }

            var record = new PrintAttemptRecord(orderId);

            try
            {
                var order = operations.GetOrderById(orderId);
                if (order == null)
                {
                    record.AttemptId = AttemptIdGenerator.Next("unknown");
                    record.Complete("FAILED_ORDER_LOAD");
                    logger.LogFailure(record, "FAILED_ORDER_LOAD", new InvalidOperationException("Order not found"));
                    logger.TryWriteAudit(record);
                    return null;
                }

                record.OrderNumber = order.Number.ToString();
                record.RepeatBillNumber = order.RepeatBillNumber;
                record.AttemptId = AttemptIdGenerator.Next(record.OrderNumber);

                logger.LogAttemptStarted(record);
                logger.LogOrderLoaded(record);

                var model = orderDataExtractor.Extract(order, record.AttemptId);
                var payload = payloadBuilder.Build(model, settings);
                record.Payload = payload;
                record.Items = model.Items.Count;
                record.ModifierCount = model.TotalModifierCount;
                record.Characters = payload.Length;
                record.Utf8Bytes = Encoding.UTF8.GetByteCount(payload);

                logger.LogPayloadBuilt(record);

                if (record.Utf8Bytes > settings.MaxPayloadUtf8BytesWarning)
                {
                    logger.LogPayloadSizeWarning(record);
                }

                if (settings.WriteFullPayloadToStandardLog)
                {
                    logger.LogFullPayload(record);
                }

                var result = qrMarkupFactory.Create(payload, record.AttemptId, settings);
                record.Complete("EXTENSION_RETURNED");
                logger.LogExtensionReturned(record);
                logger.TryWriteAudit(record);
                return result;
            }
            catch (Exception ex)
            {
                record.AttemptId = string.IsNullOrWhiteSpace(record.AttemptId)
                    ? AttemptIdGenerator.Next("unknown")
                    : record.AttemptId;
                record.Complete("FAILED_DATA_EXTRACTION");
                logger.LogFailure(record, "FAILED_DATA_EXTRACTION", ex);
                logger.TryWriteAudit(record);
                return null;
            }
        }
    }
}
