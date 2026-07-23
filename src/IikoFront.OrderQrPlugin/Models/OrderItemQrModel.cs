using System.Collections.Generic;

namespace IikoFront.OrderQrPlugin.Models
{
    public sealed class OrderItemQrModel
    {
        public int SequenceNumber { get; set; }
        public string ItemId { get; set; } = "-";
        public string ProductId { get; set; } = "-";
        public string SourceType { get; set; } = "UNKNOWN";
        public string Name { get; set; } = "-";
        public string Quantity { get; set; } = "-";
        public string Size { get; set; } = "-";
        public NutritionQrModel Nutrition { get; set; } = NutritionQrModel.Missing(FoodValueStatuses.Null);
        public string AllergensText { get; set; } = "-";
        public string AllergenStatus { get; set; } = AllergenStatuses.EmptyOrNone;
        public int AllergenCount { get; set; }
        public List<ModifierQrModel> Modifiers { get; } = new List<ModifierQrModel>();
    }
}
