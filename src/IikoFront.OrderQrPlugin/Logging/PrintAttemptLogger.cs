using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using IikoFront.OrderQrPlugin.Configuration;
using IikoFront.OrderQrPlugin.Models;
using Resto.Front.Api;

namespace IikoFront.OrderQrPlugin.Logging
{
    public sealed class PrintAttemptLogger
    {
        private static readonly object JsonlSync = new object();
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

        private readonly ILog log;
        private readonly IPluginIntegrationService integrationService;
        private readonly PluginSettings settings;

        public PrintAttemptLogger(ILog log, IPluginIntegrationService integrationService, PluginSettings settings)
        {
            this.log = log;
            this.integrationService = integrationService;
            this.settings = settings;
        }

        public void FlushNotice()
        {
            log.Info("event=LOGGER_DISPOSED");
        }

        public void LogAttemptStarted(PrintAttemptRecord record)
        {
            InfoEvent(
                "QR_PRINT_ATTEMPT",
                ("status", "STARTED"),
                ("attemptId", record.AttemptId),
                ("orderId", record.OrderId),
                ("orderNumber", record.OrderNumber));
        }

        public void LogOrderLoaded(PrintAttemptRecord record)
        {
            InfoEvent(
                "QR_PRINT_ATTEMPT",
                ("status", "ORDER_LOADED"),
                ("attemptId", record.AttemptId),
                ("orderId", record.OrderId),
                ("orderNumber", record.OrderNumber),
                ("repeatBillNumber", record.RepeatBillNumber));
        }

        public void LogPayloadBuilt(PrintAttemptRecord record)
        {
            InfoEvent(
                "QR_PRINT_ATTEMPT",
                ("status", "PAYLOAD_BUILT"),
                ("attemptId", record.AttemptId),
                ("orderId", record.OrderId),
                ("orderNumber", record.OrderNumber),
                ("items", record.Items),
                ("modifierCount", record.ModifierCount),
                ("chars", record.Characters),
                ("utf8Bytes", record.Utf8Bytes));
        }

        public void LogPayloadSizeWarning(PrintAttemptRecord record)
        {
            WarnEvent(
                "QR_PRINT_ATTEMPT",
                ("status", "PAYLOAD_SIZE_WARNING"),
                ("attemptId", record.AttemptId),
                ("orderId", record.OrderId),
                ("orderNumber", record.OrderNumber),
                ("chars", record.Characters),
                ("utf8Bytes", record.Utf8Bytes),
                ("limit", settings.MaxPayloadUtf8BytesWarning));
        }

        public void LogExtensionReturned(PrintAttemptRecord record)
        {
            InfoEvent(
                "QR_PRINT_ATTEMPT",
                ("status", "EXTENSION_RETURNED"),
                ("attemptId", record.AttemptId),
                ("orderId", record.OrderId),
                ("orderNumber", record.OrderNumber),
                ("items", record.Items),
                ("modifierCount", record.ModifierCount),
                ("chars", record.Characters),
                ("utf8Bytes", record.Utf8Bytes),
                ("elapsedMs", record.ElapsedMs));
        }

        public void LogFailure(PrintAttemptRecord record, string status, Exception exception)
        {
            record.ErrorType = exception.GetType().FullName ?? exception.GetType().Name;
            record.ErrorMessage = exception.Message ?? string.Empty;

            ErrorEvent(
                "QR_PRINT_ATTEMPT",
                exception,
                ("status", status),
                ("attemptId", record.AttemptId),
                ("orderId", record.OrderId),
                ("orderNumber", record.OrderNumber),
                ("errorType", record.ErrorType),
                ("message", record.ErrorMessage));
        }

        public void LogItem(OrderItemQrModel item, string attemptId)
        {
            InfoEvent(
                "QR_ITEM",
                ("attemptId", attemptId),
                ("itemId", item.ItemId),
                ("productId", item.ProductId),
                ("name", item.Name),
                ("amount", item.Quantity),
                ("foodValueStatus", item.Nutrition.Status),
                ("caloricity", item.Nutrition.Caloricity),
                ("protein", item.Nutrition.Protein),
                ("fat", item.Nutrition.Fat),
                ("carbohydrate", item.Nutrition.Carbohydrate),
                ("allergenStatus", item.AllergenStatus),
                ("allergenCount", item.AllergenCount),
                ("modifierCount", item.Modifiers.Count));
        }

        public void LogItemWarning(string attemptId, string itemId, string warningCode, string message)
        {
            WarnEvent(
                "QR_ITEM_WARN",
                ("attemptId", attemptId),
                ("itemId", itemId),
                ("warning", warningCode),
                ("message", message));
        }

        public void LogUnsupportedItem(string attemptId, string itemId, string itemType)
        {
            WarnEvent(
                "QR_ITEM_WARN",
                ("attemptId", attemptId),
                ("itemId", itemId),
                ("warning", "UNSUPPORTED_ORDER_ITEM_TYPE"),
                ("itemType", itemType));
        }

        public void LogFullPayload(PrintAttemptRecord record)
        {
            InfoEvent(
                "QR_PAYLOAD",
                ("attemptId", record.AttemptId),
                ("payload", record.Payload.Replace("\r", string.Empty).Replace("\n", "\\n")));
        }

        public void TryWriteAudit(PrintAttemptRecord record)
        {
            if (!settings.WriteJsonlAuditLog)
            {
                return;
            }

            try
            {
                var directory = integrationService.GetDataStorageDirectoryPath();
                Directory.CreateDirectory(directory);
                var filePath = Path.Combine(directory, $"qr-print-attempts-{DateTime.Now:yyyy-MM-dd}.jsonl");
                var line = Serialize(record);

                lock (JsonlSync)
                {
                    using (var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                    {
                        var bytes = Utf8WithoutBom.GetBytes(line + Environment.NewLine);
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("event=FAILED_LOG_WRITE", ex);
            }
        }

        private void InfoEvent(string eventName, params (string Key, object Value)[] pairs)
        {
            log.Info(FormatLine(eventName, pairs));
        }

        private void WarnEvent(string eventName, params (string Key, object Value)[] pairs)
        {
            log.Warn(FormatLine(eventName, pairs));
        }

        private void ErrorEvent(string eventName, Exception exception, params (string Key, object Value)[] pairs)
        {
            log.Error(FormatLine(eventName, pairs), exception);
        }

        private static string FormatLine(string eventName, IEnumerable<(string Key, object Value)> pairs)
        {
            var parts = new List<string> { $"event={eventName}" };
            parts.AddRange(pairs.Select(pair => $"{pair.Key}={formatValue(pair.Value)}"));
            return string.Join(" ", parts);
        }

        private static string Serialize(PrintAttemptRecord record)
        {
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(PrintAttemptRecord));
                serializer.WriteObject(stream, record);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private static string formatValue(object value)
        {
            if (value == null)
            {
                return "-";
            }

            switch (value)
            {
                case string stringValue:
                    return sanitize(stringValue);
                case bool boolValue:
                    return boolValue ? "true" : "false";
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return sanitize(value.ToString() ?? "-");
            }
        }

        private static string sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "-";
            }

            return value
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Replace('\t', ' ')
                .Replace(' ', '_');
        }
    }
}
