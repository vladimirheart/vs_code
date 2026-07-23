using System;
using IikoFront.OrderQrPlugin.Configuration;
using IikoFront.OrderQrPlugin.Models;
using IikoFront.OrderQrPlugin.Payload;
using Resto.Front.Api.Data.DataTransferObjects;

namespace IikoFront.OrderQrPlugin.Extraction
{
    public sealed class FoodValueExtractor
    {
        private readonly PluginSettings settings;

        public FoodValueExtractor(PluginSettings settings)
        {
            this.settings = settings;
        }

        public NutritionQrModel Extract(Func<FoodValue> valueAccessor)
        {
            try
            {
                var foodValue = valueAccessor();
                return Extract(foodValue == null ? null : new FoodValueSnapshot
                {
                    Caloricity = foodValue.Caloricity,
                    Protein = foodValue.Protein,
                    Fat = foodValue.Fat,
                    Carbohydrate = foodValue.Carbohydrate
                });
            }
            catch
            {
                return NutritionQrModel.Missing(FoodValueStatuses.Error);
            }
        }

        public NutritionQrModel Extract(FoodValueSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return NutritionQrModel.Missing(FoodValueStatuses.Null);
            }

            var allZero =
                snapshot.Caloricity == 0m &&
                snapshot.Protein == 0m &&
                snapshot.Fat == 0m &&
                snapshot.Carbohydrate == 0m;

            if (allZero && settings.TreatAllZeroFoodValueAsMissing)
            {
                return NutritionQrModel.Missing(FoodValueStatuses.AllZero);
            }

            return new NutritionQrModel
            {
                Status = allZero ? FoodValueStatuses.AllZero : FoodValueStatuses.Available,
                Caloricity = NumberFormatter.Format(snapshot.Caloricity),
                Protein = NumberFormatter.Format(snapshot.Protein),
                Fat = NumberFormatter.Format(snapshot.Fat),
                Carbohydrate = NumberFormatter.Format(snapshot.Carbohydrate)
            };
        }
    }
}
