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
                lines.Add(buildModifiersLine(item, settings));
            }

            return string.Join("\n", lines);
        }

        private static string buildOrderLine(OrderQrModel order, PluginSettings settings)
        {
            var fields = new List<string>
            {
                $"ЗАКАЗ:{PayloadEscaper.EscapeOrDash(order.OrderNumber)}"
            };

            if (settings.IncludePrintTime && order.PrintTime.HasValue)
            {
                fields.Add($"ДАТА:{order.PrintTime.Value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)}");
            }

            fields.Add($"ПОВТОР:{order.RepeatBillNumber.ToString(CultureInfo.InvariantCulture)}");
            fields.Add($"ПОЗИЦИЙ:{order.ActiveItemCount.ToString(CultureInfo.InvariantCulture)}");

            if (settings.IncludeOrderGuidInPayload)
            {
                fields.Add($"GUID:{order.OrderId:D}");
            }

            return string.Join(FieldSeparator, fields);
        }

        private static IReadOnlyList<string> buildItemLines(OrderItemQrModel item)
        {
            return new[]
            {
                string.Join(
                    FieldSeparator,
                    item.SequenceNumber.ToString(CultureInfo.InvariantCulture),
                    $"Н:{PayloadEscaper.EscapeOrDash(item.Name)}"),
                string.Join(
                    FieldSeparator,
                    $"КОЛ:{item.Quantity}",
                    $"РАЗМЕР:{PayloadEscaper.EscapeOrDash(item.Size)}"),
                string.Join(
                    FieldSeparator,
                    $"ККАЛ:{item.Nutrition.Caloricity}",
                    $"Б:{item.Nutrition.Protein}",
                    $"Ж:{item.Nutrition.Fat}",
                    $"У:{item.Nutrition.Carbohydrate}"),
                $"АЛЛЕРГЕНЫ:{PayloadEscaper.EscapeOrDash(item.AllergensText)}"
            };
        }

        private static string buildModifiersLine(OrderItemQrModel item, PluginSettings settings)
        {
            if (!settings.IncludeModifiers || item.Modifiers.Count == 0)
            {
                return "МОДИФИКАТОРЫ:-";
            }

            var modifiers = item.Modifiers
                .Select(modifier =>
                    $"{PayloadEscaper.EscapeOrDash(modifier.Name)}[КОЛ:{modifier.Quantity}; ККАЛ:{modifier.Nutrition.Caloricity}; Б:{modifier.Nutrition.Protein}; Ж:{modifier.Nutrition.Fat}; У:{modifier.Nutrition.Carbohydrate}]");

            return "МОДИФИКАТОРЫ:" + string.Join("|", modifiers);
        }
    }
}
