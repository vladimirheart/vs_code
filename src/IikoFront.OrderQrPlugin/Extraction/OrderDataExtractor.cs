using System;
using System.Globalization;
using System.Linq;
using IikoFront.OrderQrPlugin.Configuration;
using IikoFront.OrderQrPlugin.Logging;
using IikoFront.OrderQrPlugin.Models;
using Resto.Front.Api.Data.Orders;

namespace IikoFront.OrderQrPlugin.Extraction
{
    public sealed class OrderDataExtractor
    {
        private readonly PluginSettings settings;
        private readonly ProductItemExtractor productItemExtractor;
        private readonly PrintAttemptLogger logger;

        public OrderDataExtractor(
            PluginSettings settings,
            ProductItemExtractor productItemExtractor,
            PrintAttemptLogger logger)
        {
            this.settings = settings;
            this.productItemExtractor = productItemExtractor;
            this.logger = logger;
        }

        public OrderQrModel Extract(IOrder order, string attemptId)
        {
            var activeRootItems = order.Items
                .Where(item => item != null && !item.Deleted)
                .ToList();

            var model = new OrderQrModel
            {
                OrderId = order.Id,
                OrderNumber = order.Number.ToString(CultureInfo.InvariantCulture),
                PrintTime = settings.IncludePrintTime ? (order.BillTime ?? DateTime.Now) : (DateTime?)null,
                RepeatBillNumber = order.RepeatBillNumber,
                ActiveItemCount = activeRootItems.Count
            };

            foreach (var rootItem in activeRootItems)
            {
                try
                {
                    if (rootItem is IOrderProductItem productItem)
                    {
                        add(model, productItemExtractor.ExtractProduct(productItem, attemptId));
                        continue;
                    }

                    if (rootItem is IOrderCompoundItem compoundItem)
                    {
                        foreach (var item in productItemExtractor.ExtractCompound(compoundItem, attemptId))
                        {
                            add(model, item);
                        }

                        continue;
                    }

                    add(model, productItemExtractor.ExtractUnsupported(rootItem, attemptId));
                }
                catch (Exception ex)
                {
                    logger.LogItemWarning(attemptId, rootItem.Id.ToString("D"), "ROOT_ITEM_READ_ERROR", ex.Message);
                    add(model, productItemExtractor.ExtractPlaceholder(rootItem, attemptId, ex));
                }
            }

            return model;
        }

        private static void add(OrderQrModel order, OrderItemQrModel item)
        {
            item.SequenceNumber = order.Items.Count + 1;
            order.TotalModifierCount += item.Modifiers.Count;
            order.Items.Add(item);
        }
    }
}
