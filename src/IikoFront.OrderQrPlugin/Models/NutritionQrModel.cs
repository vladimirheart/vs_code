namespace IikoFront.OrderQrPlugin.Models
{
    public sealed class NutritionQrModel
    {
        public string Status { get; set; } = FoodValueStatuses.Null;
        public string Caloricity { get; set; } = "-";
        public string Protein { get; set; } = "-";
        public string Fat { get; set; } = "-";
        public string Carbohydrate { get; set; } = "-";

        public static NutritionQrModel Missing(string status)
        {
            return new NutritionQrModel
            {
                Status = status,
                Caloricity = "-",
                Protein = "-",
                Fat = "-",
                Carbohydrate = "-"
            };
        }
    }
}
