using System;
using System.Text;
using System.Xml.Linq;
using IikoFront.OrderQrPlugin.Configuration;
using Resto.Front.Api.Data.Cheques;

namespace IikoFront.OrderQrPlugin.Printing
{
    public sealed class QrMarkupFactory
    {
        public ChequeExtensions Create(string payload, string attemptId, PluginSettings settings)
        {
            var qrPayload = encodePayload(payload, settings);
            var qrElement = new XElement(
                Tags.QRCode,
                new XAttribute(Attributes.Size, normalizeSize(settings.QrSize)),
                new XAttribute(Attributes.Correction, normalizeCorrection(settings.QrCorrection)),
                qrPayload);

            var markup = new XElement(
                Tags.Center,
                qrElement,
                new XElement(Tags.Br),
                new XElement(Tags.SmallFont, "КБЖУ и аллергены заказа"),
                new XElement(Tags.Br),
                new XElement(Tags.SmallFont, "QR-ID: " + attemptId));

            return new ChequeExtensions
            {
                AfterFooter = markup
            };
        }

        private static string normalizeSize(string value)
        {
            if (string.Equals(value, AttributeValues.Extralarge, StringComparison.OrdinalIgnoreCase))
            {
                return AttributeValues.Extralarge;
            }

            if (string.Equals(value, AttributeValues.Large, StringComparison.OrdinalIgnoreCase))
            {
                return AttributeValues.Large;
            }

            if (string.Equals(value, AttributeValues.Medium, StringComparison.OrdinalIgnoreCase))
            {
                return AttributeValues.Medium;
            }

            if (string.Equals(value, AttributeValues.Small, StringComparison.OrdinalIgnoreCase))
            {
                return AttributeValues.Small;
            }

            if (string.Equals(value, AttributeValues.Tiny, StringComparison.OrdinalIgnoreCase))
            {
                return AttributeValues.Tiny;
            }

            if (string.Equals(value, AttributeValues.Ultra, StringComparison.OrdinalIgnoreCase))
            {
                return AttributeValues.Ultra;
            }

            return AttributeValues.Extralarge;
        }

        private static string normalizeCorrection(string value)
        {
            if (string.Equals(value, AttributeValues.High, StringComparison.OrdinalIgnoreCase))
            {
                return AttributeValues.High;
            }

            return AttributeValues.Low;
        }

        private static string encodePayload(string payload, PluginSettings settings)
        {
            if (string.IsNullOrEmpty(payload))
            {
                return string.Empty;
            }

            if (string.Equals(settings.QrPayloadEncodingMode, "Raw", StringComparison.OrdinalIgnoreCase))
            {
                return payload;
            }

            if (string.Equals(settings.QrPayloadEncodingMode, "Utf8ViaPrinterCodePage", StringComparison.OrdinalIgnoreCase))
            {
                var printerEncoding = resolvePrinterEncoding(settings.QrPayloadPrinterCodePage);
                return printerEncoding.GetString(Encoding.UTF8.GetBytes(payload));
            }

            return payload;
        }

        private static Encoding resolvePrinterEncoding(int codePage)
        {
            try
            {
                return Encoding.GetEncoding(codePage);
            }
            catch (ArgumentException)
            {
                return Encoding.GetEncoding(866);
            }
        }
    }
}
