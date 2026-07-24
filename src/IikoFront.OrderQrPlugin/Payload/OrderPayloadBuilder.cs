using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IikoFront.OrderQrPlugin.Configuration;
using IikoFront.OrderQrPlugin.Models;

namespace IikoFront.OrderQrPlugin.Payload
{
    public sealed class OrderPayloadBuilder
    {
        private const string FieldSeparator = "; ";

        public string Build(OrderQrModel order, PluginSettings settings)
        {
            var lines = new List<string> { settings.PayloadVersion };
            lines.Add(buildOrderLine(order, settings));

            foreach (var item in order.Items)
            {
                lines.AddRange(buildItemLines(item));
                var modifiersLine = buildModifiersLine(item, settings);
                if (!string.IsNullOrWhiteSpace(modifiersLine))
                {
                    lines.Add(modifiersLine);
                }
            }

            return string.Join("\n", lines);
        }

        private static string buildOrderLine(OrderQrModel order, PluginSettings settings)
        {
            var fields = new List<string>
            {
                $"ЗАК:{PayloadEscaper.EscapeOrDash(order.OrderNumber)}"
            };

            if (settings.IncludePrintTime && order.PrintTime.HasValue)
            {
                fields.Add($"Д.:{order.PrintTime.Value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)}");
            }

            fields.Add($"ПОЗ.:{order.ActiveItemCount.ToString(CultureInfo.InvariantCulture)}");

            if (settings.IncludeOrderGuidInPayload)
            {
                fields.Add($"GUID:{order.OrderId:D}");
            }

            return string.Join(FieldSeparator, fields);
        }

        private static IReadOnlyList<string> buildItemLines(OrderItemQrModel item)
        {
            var quantityFields = new List<string>
            {
                $"КОЛ:{item.Quantity}"
            };

            if (!isDash(item.Size))
            {
                quantityFields.Add($"РАЗМЕР:{PayloadEscaper.EscapeOrDash(item.Size)}");
            }

            return new[]
            {
                string.Concat(
                    item.SequenceNumber.ToString(CultureInfo.InvariantCulture),
                    ";",
                    PayloadEscaper.EscapeOrDash(item.Name)),
                string.Join(FieldSeparator, quantityFields),
                string.Join(
                    FieldSeparator,
                    $"ККАЛ:{item.Nutrition.Caloricity}",
                    $"Б:{item.Nutrition.Protein}",
                    $"Ж:{item.Nutrition.Fat}",
                    $"У:{item.Nutrition.Carbohydrate}"),
                $"АЛГ:{PayloadEscaper.EscapeOrDash(item.AllergensText)}"
            };
        }

        private static string buildModifiersLine(OrderItemQrModel item, PluginSettings settings)
        {
            if (!settings.IncludeModifiers || item.Modifiers.Count == 0)
            {
                return string.Empty;
            }

            var modifiers = item.Modifiers
                .Select(modifier =>
                    $"{PayloadEscaper.EscapeOrDash(modifier.Name)}[КОЛ:{modifier.Quantity}; ККАЛ:{modifier.Nutrition.Caloricity}; Б:{modifier.Nutrition.Protein}; Ж:{modifier.Nutrition.Fat}; У:{modifier.Nutrition.Carbohydrate}]");

            return "МОД.:" + string.Join("|", modifiers);
        }

        private static bool isDash(string value)
        {
            return string.IsNullOrWhiteSpace(value) || value.Trim() == "-";
        }
    }
}
