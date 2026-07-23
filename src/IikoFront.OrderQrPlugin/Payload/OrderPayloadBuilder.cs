using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IikoFront.OrderQrPlugin.Configuration;
using IikoFront.OrderQrPlugin.Models;

namespace IikoFront.OrderQrPlugin.Payload
{
    public sealed class OrderPayloadBuilder
    {
        public string Build(OrderQrModel order, PluginSettings settings)
        {
            var lines = new List<string> { settings.PayloadVersion };
            lines.Add(buildOrderLine(order, settings));

            foreach (var item in order.Items)
            {
                lines.Add(buildItemLine(item));
                lines.Add(buildModifiersLine(item, settings));
            }

            return string.Join("\n", lines);
        }

        private static string buildOrderLine(OrderQrModel order, PluginSettings settings)
        {
            var fields = new List<string>
            {
                $"O:{PayloadEscaper.EscapeOrDash(order.OrderNumber)}"
            };

            if (settings.IncludePrintTime && order.PrintTime.HasValue)
            {
                fields.Add($"D:{order.PrintTime.Value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)}");
            }

            fields.Add($"R:{order.RepeatBillNumber.ToString(CultureInfo.InvariantCulture)}");
            fields.Add($"C:{order.ActiveItemCount.ToString(CultureInfo.InvariantCulture)}");

            if (settings.IncludeOrderGuidInPayload)
            {
                fields.Add($"G:{order.OrderId:D}");
            }

            return string.Join(";", fields);
        }

        private static string buildItemLine(OrderItemQrModel item)
        {
            return string.Join(
                ";",
                item.SequenceNumber.ToString(CultureInfo.InvariantCulture),
                $"N:{PayloadEscaper.EscapeOrDash(item.Name)}",
                $"Q:{item.Quantity}",
                $"S:{PayloadEscaper.EscapeOrDash(item.Size)}",
                $"K:{item.Nutrition.Caloricity}",
                $"B:{item.Nutrition.Protein}",
                $"J:{item.Nutrition.Fat}",
                $"U:{item.Nutrition.Carbohydrate}",
                $"A:{PayloadEscaper.EscapeOrDash(item.AllergensText)}");
        }

        private static string buildModifiersLine(OrderItemQrModel item, PluginSettings settings)
        {
            if (!settings.IncludeModifiers || item.Modifiers.Count == 0)
            {
                return "M:-";
            }

            var modifiers = item.Modifiers
                .Select(modifier =>
                    $"{PayloadEscaper.EscapeOrDash(modifier.Name)}[Q:{modifier.Quantity};K:{modifier.Nutrition.Caloricity};B:{modifier.Nutrition.Protein};J:{modifier.Nutrition.Fat};U:{modifier.Nutrition.Carbohydrate}]");

            return "M:" + string.Join("|", modifiers);
        }
    }
}
