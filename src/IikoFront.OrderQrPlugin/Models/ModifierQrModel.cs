namespace IikoFront.OrderQrPlugin.Models
{
    public sealed class ModifierQrModel
    {
        public string Name { get; set; } = "-";
        public string Quantity { get; set; } = "-";
        public NutritionQrModel Nutrition { get; set; } = NutritionQrModel.Missing(FoodValueStatuses.Null);
    }
}
