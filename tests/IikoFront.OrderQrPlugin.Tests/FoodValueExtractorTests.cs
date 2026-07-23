using IikoFront.OrderQrPlugin.Configuration;
using IikoFront.OrderQrPlugin.Extraction;
using IikoFront.OrderQrPlugin.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IikoFront.OrderQrPlugin.Tests
{
    [TestClass]
    public class FoodValueExtractorTests
    {
        [TestMethod]
        public void Extract_WhenFoodValueIsNull_ReturnsDashes()
        {
            var extractor = new FoodValueExtractor(new PluginSettings());

            var result = extractor.Extract((FoodValueSnapshot)null);

            Assert.AreEqual(FoodValueStatuses.Null, result.Status);
            Assert.AreEqual("-", result.Caloricity);
            Assert.AreEqual("-", result.Protein);
            Assert.AreEqual("-", result.Fat);
            Assert.AreEqual("-", result.Carbohydrate);
        }

        [TestMethod]
        public void Extract_WhenAllZeroAndTreatAsMissing_ReturnsDashes()
        {
            var extractor = new FoodValueExtractor(new PluginSettings { TreatAllZeroFoodValueAsMissing = true });

            var result = extractor.Extract(new FoodValueSnapshot());

            Assert.AreEqual(FoodValueStatuses.AllZero, result.Status);
            Assert.AreEqual("-", result.Caloricity);
            Assert.AreEqual("-", result.Protein);
            Assert.AreEqual("-", result.Fat);
            Assert.AreEqual("-", result.Carbohydrate);
        }

        [TestMethod]
        public void Extract_WhenAllZeroAndTreatAsMissingDisabled_ReturnsZeros()
        {
            var extractor = new FoodValueExtractor(new PluginSettings { TreatAllZeroFoodValueAsMissing = false });

            var result = extractor.Extract(new FoodValueSnapshot());

            Assert.AreEqual(FoodValueStatuses.AllZero, result.Status);
            Assert.AreEqual("0", result.Caloricity);
            Assert.AreEqual("0", result.Protein);
            Assert.AreEqual("0", result.Fat);
            Assert.AreEqual("0", result.Carbohydrate);
        }

        [TestMethod]
        public void Extract_WhenPartialZero_ReturnsAvailableValues()
        {
            var extractor = new FoodValueExtractor(new PluginSettings());

            var result = extractor.Extract(new FoodValueSnapshot
            {
                Caloricity = 420m,
                Protein = 18m,
                Fat = 0m,
                Carbohydrate = 43m
            });

            Assert.AreEqual(FoodValueStatuses.Available, result.Status);
            Assert.AreEqual("420", result.Caloricity);
            Assert.AreEqual("18", result.Protein);
            Assert.AreEqual("0", result.Fat);
            Assert.AreEqual("43", result.Carbohydrate);
        }
    }
}
