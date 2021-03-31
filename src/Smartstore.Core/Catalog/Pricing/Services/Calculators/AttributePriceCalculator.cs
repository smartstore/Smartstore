using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// Calculates the price of product attributes specified by <see cref="PriceCalculationContext.Attributes"/>.
    /// These are usually attributes selected on the product detail page, whose price adjustments must be included in the shopping cart.
    /// Also applies attributes pre-selected by merchant if <see cref="PriceCalculationContext.ApplyPreSelectedAttributes"/> is <c>true</c>.
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
            var options = context.Options;
            var product = context.Product;

            if (options.IgnoreAttributes || !(context.Attributes?.Any() ?? false))
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

            var attributes = await context.Options.BatchContext.Attributes.GetOrLoadAsync(product.Id);
            var attributeValues = GetSelectedAttributeValues(context, attributes);

            var linkedProductIds = attributeValues
                .Where(x => x.ValueType == ProductVariantAttributeValueType.ProductLinkage && x.LinkedProductId != 0)
                .Select(x => x.LinkedProductId)
                .Distinct()
                .ToArray();

            var linkedProducts = linkedProductIds.Any()
                ? await _db.Products.AsNoTracking().Where(x => linkedProductIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id)
                : new Dictionary<int, Product>();

            // Add attribute price adjustment to final price.
            foreach (var value in attributeValues)
            {
                if (value.LinkedProductId == 0)
                {
                    if (processTierPrices && value.PriceAdjustment > decimal.Zero)
                    {
                        var tierPrices = await context.GetTierPricesAsync();
                        var priceAdjustment = GetTierPriceAttributeAdjustment(product, tierPrices, context.Quantity, value.PriceAdjustment);
                        context.FinalPrice += priceAdjustment;
                    }
                    else
                    {
                        context.FinalPrice += value.PriceAdjustment;
                    }
                }
                else if (linkedProducts.TryGetValue(value.LinkedProductId, out var linkedProduct))
                {
                    var childCalculation = await CalculateChildPriceAsync(linkedProduct, context, c =>
                    {
                        c.Options.IgnoreDiscounts = false;
                        c.Quantity = 1;
                        c.AssociatedProducts = null;
                        c.BundleItems = null;
                        c.BundleItem = null;
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

        protected virtual List<ProductVariantAttributeValue> GetSelectedAttributeValues(CalculatorContext context, IEnumerable<ProductVariantAttribute> attributes)
        {
            var result = new List<ProductVariantAttributeValue>();
            var bundleItem = context?.BundleItem?.Item;

            // Apply attributes selected by customer.
            var selections = context.Attributes
                .Where(x => x.ProductId == context.Product.Id && x.BundleItemId == context.BundleItem?.Item?.Id)
                .Select(x => x.Selection)
                .ToList();

            foreach (var selection in selections)
            {
                var attributeValues = selection.MaterializeProductVariantAttributeValues(attributes);

                // Ignore attributes that have no relevance for pricing.
                var pricingValues = attributeValues
                    .Where(x => x.PriceAdjustment != decimal.Zero || x.ValueType == ProductVariantAttributeValueType.ProductLinkage);

                // Ignore attributes that are filtered out for a bundle item.
                if (bundleItem?.FilterAttributes ?? false)
                {
                    pricingValues = pricingValues
                        .Where(x => bundleItem.AttributeFilters.Any(af => af.AttributeId == x.ProductVariantAttributeId && af.AttributeValueId == x.Id));
                }

                result.AddRange(pricingValues);
            }

            // Apply attributes pre-selected by merchant.
            if (context.Options.ApplyPreSelectedAttributes)
            {
                // Ignore already applied values.
                var appliedValueIds = result.Select(x => x.Id).ToArray();

                var preSelectedValues = attributes
                    .SelectMany(x => x.ProductVariantAttributeValues)
                    .Where(x =>
                        !appliedValueIds.Contains(x.Id) &&
                        x.IsPreSelected &&
                        (x.PriceAdjustment != decimal.Zero || x.ValueType == ProductVariantAttributeValueType.ProductLinkage));

                // Ignore attributes that are filtered out for a bundle item.
                if (bundleItem?.FilterAttributes ?? false)
                {
                    preSelectedValues = preSelectedValues
                        .Where(x => bundleItem.AttributeFilters.Any(af => af.IsPreSelected && af.AttributeId == x.ProductVariantAttributeId && af.AttributeValueId == x.Id));
                }

                result.AddRange(preSelectedValues);
            }

            return result;
        }
    }
}
