using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IikoFront.OrderQrPlugin.Configuration;
using IikoFront.OrderQrPlugin.Logging;
using IikoFront.OrderQrPlugin.Models;
using IikoFront.OrderQrPlugin.Payload;
using Resto.Front.Api;
using Resto.Front.Api.Data.Assortment;
using Resto.Front.Api.Data.Common;
using Resto.Front.Api.Data.Orders;

namespace IikoFront.OrderQrPlugin.Extraction
{
    public sealed class ProductItemExtractor
    {
        private readonly IOperationService operations;
        private readonly PluginSettings settings;
        private readonly FoodValueExtractor foodValueExtractor;
        private readonly PrintAttemptLogger logger;

        public ProductItemExtractor(
            IOperationService operations,
            PluginSettings settings,
            FoodValueExtractor foodValueExtractor,
            PrintAttemptLogger logger)
        {
            this.operations = operations;
            this.settings = settings;
            this.foodValueExtractor = foodValueExtractor;
            this.logger = logger;
        }

        public OrderItemQrModel ExtractProduct(IOrderProductItem item, string attemptId)
        {
            var result = new OrderItemQrModel
            {
                ItemId = entityId(item),
                ProductId = entityId(item.Product as IEntity),
                SourceType = "PRODUCT",
                Name = resolveName(item.ProductCustomName, item.Product),
                Quantity = formatAmount(() => item.Amount, attemptId, entityId(item)),
                Size = resolveSize(() => item.Size),
                Nutrition = foodValueExtractor.Extract(() => item.FoodValue)
            };

            applyAllergens(result, item);
            applyModifiers(result.Modifiers, item.AssignedModifiers, attemptId);
            logger.LogItem(result, attemptId);
            return result;
        }

        public IReadOnlyList<OrderItemQrModel> ExtractCompound(IOrderCompoundItem item, string attemptId)
        {
            var items = new List<OrderItemQrModel>();
            var rootName = firstNonEmpty(item.Template?.Name, "-");
            var rootItem = new OrderItemQrModel
            {
                ItemId = entityId(item),
                ProductId = "-",
                SourceType = "COMPOUND_ROOT",
                Name = rootName,
                Quantity = formatAmount(() => item.Amount, attemptId, entityId(item)),
                Size = resolveSize(() => item.Size),
                Nutrition = NutritionQrModel.Missing(FoodValueStatuses.Null)
            };

            applyAllergens(rootItem, item);
            applyModifiers(rootItem.Modifiers, item.CommonModifiers, attemptId);
            logger.LogItem(rootItem, attemptId);
            items.Add(rootItem);

            if (item.PrimaryComponent != null)
            {
                items.Add(extractCompoundComponent(rootName, item.PrimaryComponent, rootItem.Quantity, rootItem.Size, attemptId));
            }

            if (item.SecondaryComponent != null)
            {
                items.Add(extractCompoundComponent(rootName, item.SecondaryComponent, rootItem.Quantity, rootItem.Size, attemptId));
            }

            return items;
        }

        public OrderItemQrModel ExtractUnsupported(IOrderRootItem item, string attemptId)
        {
            logger.LogUnsupportedItem(attemptId, entityId(item), item.GetType().FullName ?? item.GetType().Name);

            var result = new OrderItemQrModel
            {
                ItemId = entityId(item),
                ProductId = "-",
                SourceType = "UNSUPPORTED",
                Name = fallbackName(item),
                Quantity = "-",
                Size = "-",
                Nutrition = NutritionQrModel.Missing(FoodValueStatuses.Error),
                AllergensText = "-",
                AllergenStatus = AllergenStatuses.EmptyOrNone,
                AllergenCount = 0
            };

            logger.LogItem(result, attemptId);
            return result;
        }

        public OrderItemQrModel ExtractPlaceholder(IOrderRootItem item, string attemptId, Exception exception)
        {
            logger.LogFailure(
                new PrintAttemptRecord(Guid.Empty)
                {
                    AttemptId = attemptId,
                    OrderId = "-",
                    OrderNumber = "-"
                },
                "FAILED_DATA_EXTRACTION",
                exception);

            return new OrderItemQrModel
            {
                ItemId = entityId(item),
                ProductId = "-",
                SourceType = "PLACEHOLDER",
                Name = fallbackName(item),
                Quantity = "-",
                Size = "-",
                Nutrition = NutritionQrModel.Missing(FoodValueStatuses.Error),
                AllergensText = "-",
                AllergenStatus = AllergenStatuses.Error,
                AllergenCount = 0
            };
        }

        private OrderItemQrModel extractCompoundComponent(
            string compoundName,
            IOrderCompoundItemComponent component,
            string quantity,
            string size,
            string attemptId)
        {
            var result = new OrderItemQrModel
            {
                ItemId = entityId(component),
                ProductId = entityId(component.Product as IEntity),
                SourceType = "COMPOUND_COMPONENT",
                Name = $"{compoundName} / {resolveName(component.ProductCustomName, component.Product)}",
                Quantity = quantity,
                Size = size,
                Nutrition = foodValueExtractor.Extract(() => component.FoodValue),
                AllergensText = "-",
                AllergenStatus = AllergenStatuses.EmptyOrNone,
                AllergenCount = 0
            };

            applyModifiers(result.Modifiers, component.Modifiers, attemptId);
            logger.LogItem(result, attemptId);
            return result;
        }

        private void applyAllergens(OrderItemQrModel target, IOrderRootItem item)
        {
            if (!settings.IncludeAllergens)
            {
                target.AllergensText = "-";
                target.AllergenStatus = AllergenStatuses.Disabled;
                target.AllergenCount = 0;
                return;
            }

            try
            {
                var allergens = operations
                    .GetAllergenGroupsByOrderRootItem(item)
                    .Where(group => group != null)
                    .Select(group => firstNonEmpty(group.Name, group.Code, "-"))
                    .Where(name => !string.IsNullOrWhiteSpace(name) && name != "-")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (allergens.Count == 0)
                {
                    target.AllergensText = "-";
                    target.AllergenStatus = AllergenStatuses.EmptyOrNone;
                    target.AllergenCount = 0;
                    return;
                }

                target.AllergensText = string.Join(",", allergens);
                target.AllergenStatus = AllergenStatuses.Available;
                target.AllergenCount = allergens.Count;
            }
            catch
            {
                target.AllergensText = "-";
                target.AllergenStatus = AllergenStatuses.Error;
                target.AllergenCount = 0;
            }
        }

        private void applyModifiers(
            ICollection<ModifierQrModel> target,
            IEnumerable<IOrderModifierItem> modifiers,
            string attemptId)
        {
            if (!settings.IncludeModifiers || modifiers == null)
            {
                return;
            }

            foreach (var modifier in modifiers.Where(modifier => modifier != null && !modifier.Deleted))
            {
                try
                {
                    var quantity = formatAmount(() => modifier.Amount, attemptId, entityId(modifier));
                    target.Add(new ModifierQrModel
                    {
                        Name = resolveName(modifier.ProductCustomName, modifier.Product),
                        Quantity = quantity,
                        Nutrition = foodValueExtractor.Extract(() => modifier.FoodValue)
                    });
                }
                catch (Exception ex)
                {
                    logger.LogItemWarning(attemptId, entityId(modifier), "MODIFIER_READ_ERROR", ex.Message);
                    target.Add(new ModifierQrModel
                    {
                        Name = "-",
                        Quantity = "-",
                        Nutrition = NutritionQrModel.Missing(FoodValueStatuses.Error)
                    });
                }
            }
        }

        private string formatAmount(Func<decimal> amountAccessor, string attemptId, string itemId)
        {
            try
            {
                var amount = amountAccessor();
                if (amount == 0m)
                {
                    logger.LogItemWarning(attemptId, itemId, "ZERO_AMOUNT", "Order item amount equals zero");
                }

                return NumberFormatter.Format(amount);
            }
            catch (Exception ex)
            {
                logger.LogItemWarning(attemptId, itemId, "AMOUNT_READ_ERROR", ex.Message);
                return "-";
            }
        }

        private static string resolveSize(Func<IProductSize> sizeAccessor)
        {
            try
            {
                return firstNonEmpty(sizeAccessor()?.Name, "-");
            }
            catch
            {
                return "-";
            }
        }

        private static string resolveName(string customName, IProduct product)
        {
            return firstNonEmpty(customName, product?.Name, "-");
        }

        private static string fallbackName(object source)
        {
            if (source == null)
            {
                return "-";
            }

            try
            {
                var customName = source.GetType().GetProperty("ProductCustomName")?.GetValue(source) as string;
                if (!string.IsNullOrWhiteSpace(customName))
                {
                    return customName.Trim();
                }

                var product = source.GetType().GetProperty("Product")?.GetValue(source) as IProduct;
                if (!string.IsNullOrWhiteSpace(product?.Name))
                {
                    return product.Name.Trim();
                }

                var template = source.GetType().GetProperty("Template")?.GetValue(source) as ICompoundItemTemplate;
                if (!string.IsNullOrWhiteSpace(template?.Name))
                {
                    return template.Name.Trim();
                }
            }
            catch
            {
                return "-";
            }

            return "-";
        }

        private static string entityId(IEntity entity)
        {
            return entity == null ? "-" : entity.Id.ToString("D");
        }

        private static string firstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "-";
        }
    }
}
