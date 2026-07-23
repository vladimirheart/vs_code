using System;
using IikoFront.OrderQrPlugin.Configuration;
using IikoFront.OrderQrPlugin.Logging;
using IikoFront.OrderQrPlugin.Models;
using IikoFront.OrderQrPlugin.Payload;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IikoFront.OrderQrPlugin.Tests
{
    [TestClass]
    public class OrderPayloadBuilderTests
    {
        [TestMethod]
        public void Build_WhenFieldsMissing_UsesDashes()
        {
            var builder = new OrderPayloadBuilder();
            var settings = new PluginSettings();
            var order = new OrderQrModel
            {
                OrderId = Guid.NewGuid(),
                OrderNumber = "1542",
                PrintTime = new DateTime(2026, 7, 23, 18, 42, 11),
                RepeatBillNumber = 1,
                ActiveItemCount = 1
            };

            order.Items.Add(new OrderItemQrModel
            {
                SequenceNumber = 1,
                Name = "-",
                Quantity = "-",
                Size = "-",
                Nutrition = NutritionQrModel.Missing(FoodValueStatuses.Null),
                AllergensText = "-"
            });

            var payload = builder.Build(order, settings);

            Assert.IsTrue(payload.Contains("1;N:-;Q:-;S:-;K:-;B:-;J:-;U:-;A:-"));
            Assert.IsTrue(payload.Contains("M:-"));
        }

        [TestMethod]
        public void Build_PreservesCyrillic()
        {
            var builder = new OrderPayloadBuilder();
            var settings = new PluginSettings();
            var order = new OrderQrModel
            {
                OrderId = Guid.NewGuid(),
                OrderNumber = "1542",
                PrintTime = new DateTime(2026, 7, 23, 18, 42, 11),
                RepeatBillNumber = 2,
                ActiveItemCount = 1
            };

            order.Items.Add(new OrderItemQrModel
            {
                SequenceNumber = 1,
                Name = "Борщ",
                Quantity = "1",
                Size = "-",
                Nutrition = NutritionQrModel.Missing(FoodValueStatuses.Null),
                AllergensText = "Соя"
            });

            var payload = builder.Build(order, settings);

            Assert.IsTrue(payload.Contains("Борщ"));
            Assert.IsTrue(payload.Contains("Соя"));
        }

        [TestMethod]
        public void Build_DoesNotTrimLargeOrder()
        {
            var builder = new OrderPayloadBuilder();
            var settings = new PluginSettings();
            var order = new OrderQrModel
            {
                OrderId = Guid.NewGuid(),
                OrderNumber = "9999",
                PrintTime = new DateTime(2026, 7, 23, 18, 42, 11),
                RepeatBillNumber = 0,
                ActiveItemCount = 100
            };

            for (var index = 1; index <= 100; index++)
            {
                order.Items.Add(new OrderItemQrModel
                {
                    SequenceNumber = index,
                    Name = "Позиция " + index,
                    Quantity = "1",
                    Size = "-",
                    Nutrition = NutritionQrModel.Missing(FoodValueStatuses.Null),
                    AllergensText = "-"
                });
            }

            var payload = builder.Build(order, settings);

            Assert.IsTrue(payload.Contains("Позиция 1"));
            Assert.IsTrue(payload.Contains("Позиция 100"));
        }

        [TestMethod]
        public void AttemptIdGenerator_ReturnsDifferentIds()
        {
            var first = AttemptIdGenerator.Next("1542");
            var second = AttemptIdGenerator.Next("1542");

            Assert.AreNotEqual(first, second);
        }
    }
}
