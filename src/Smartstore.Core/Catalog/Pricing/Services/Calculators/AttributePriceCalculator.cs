using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Common;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// Calculates the price of product attributes specified by <see cref="PriceCalculationContext.AttributeValues"/>.
    /// These are usually attributes selected on the product detail page, whose price adjustments must be included in the shopping cart.
    /// </summary>
    [CalculatorUsage(CalculatorTargets.Product, CalculatorOrdering.Default + 10)]
    public class AttributePriceCalculator : PriceCalculator
    {
        private readonly SmartDbContext _db;
        private readonly CatalogSettings _catalogSettings;

        public AttributePriceCalculator(IPriceCalculatorFactory calculatorFactory, SmartDbContext db, CatalogSettings catalogSettings)
            : base(calculatorFactory)
        {
            _db = db;
            _catalogSettings = catalogSettings;
        }

        public override async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            var product = context.Product;
            var options = context.Options;
            // TODO: (mg) (core) And who sets these Values? The caller? That would make things really complicated.
            var values = context.AttributeValues;

            if (options.IgnoreAttributes || !(values?.Any() ?? false))
            {
                // Proceed with pipeline and omit this calculator, it is made for attributes price calculation only.
                await next(context);
                return;
            }

            var processTierPrices = 
                !options.IgnoreTierPrices && 
                product.HasTierPrices && 
                context.BundleItem?.Item == null &&
                context.Quantity > 1 &&
                _catalogSettings.ApplyTierPricePercentageToAttributePriceAdjustments;

            var linkedProductIds = values
                .Where(x => x.ValueType == ProductVariantAttributeValueType.ProductLinkage)
                .Select(x => x.LinkedProductId)
                .Distinct()
                .ToArray();

            var linkedProducts = await _db.Products.GetManyAsync(linkedProductIds);
            var linkedProductsDic = linkedProducts.ToDictionary(x => x.Id);

            foreach (var value in values)
            {
                if (value.ValueType == ProductVariantAttributeValueType.Simple)
                {
                    if (processTierPrices && value.PriceAdjustment > decimal.Zero)
                    {
                        var tierPrices = await options.BatchContext.TierPrices.GetOrLoadAsync(product.Id);
                        // TODO: (mg) (core) I don't like the fact that everytime we fetch TierPrices, dupe removal is performed.
                        //       Find a way to make this only once during pipeline execution.
                        tierPrices = tierPrices.RemoveDuplicatedQuantities();

                        var priceAdjustment = GetTierPriceAttributeAdjustment(product, tierPrices, context.Quantity, value.PriceAdjustment);
                        context.FinalPrice += priceAdjustment;
                    }
                    else
                    {
                        context.FinalPrice += value.PriceAdjustment;
                    }
                }
                else if (value.ValueType == ProductVariantAttributeValueType.ProductLinkage && 
                    linkedProductsDic.TryGetValue(value.LinkedProductId, out var linkedProduct))
                {
                    var childCalculation = await CalculateChildPriceAsync(linkedProduct, context, c =>
                    {
                        c.Options.IgnoreDiscounts = false;
                        c.Quantity = 1;
                        c.AssociatedProducts = null;
                        c.BundleItems = null;
                        c.BundleItem = null;
                        c.AttributeValues = null;
                        c.AdditionalCharge = decimal.Zero;
                        c.MinTierPrice = null;
                    });

                    // Add price of linked product to root final price (unit price * linked product quantity).
                    context.FinalPrice += decimal.Multiply(childCalculation.FinalPrice, value.Quantity);
                }
            }

            await next(context);
        }

        protected virtual decimal GetTierPriceAttributeAdjustment(Product product, IEnumerable<TierPrice> tierPrices, int quantity, decimal adjustment)
        {
            var result = decimal.Zero;
            var previousQty = 1;

            foreach (var tierPrice in tierPrices)
            {
                if (quantity < tierPrice.Quantity || tierPrice.Quantity < previousQty)
                {
                    continue;
                }

                if (tierPrice.CalculationMethod == TierPriceCalculationMethod.Percental)
                {
                    result = adjustment - (adjustment / 100m * tierPrice.Price);
                }

                previousQty = tierPrice.Quantity;
            }

            return result;
        }
    }
}
