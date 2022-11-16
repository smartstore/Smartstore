using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// Calculates the price of product attributes specified by <see cref="PriceCalculationContext.SelectedAttributes"/>.
    /// These are usually attributes selected on the product detail page, whose price adjustments must be included in the shopping cart.
    /// Also applies attributes preselected by merchant if <see cref="PriceCalculationOptions.ApplyPreselectedAttributes"/> is <c>true</c>.
    /// </summary>
    [CalculatorUsage(CalculatorTargets.Product, CalculatorOrdering.Default + 10)]
    public class AttributePriceCalculator : PriceCalculator
    {
        private readonly SmartDbContext _db;

        public AttributePriceCalculator(IPriceCalculatorFactory calculatorFactory, SmartDbContext db)
            : base(calculatorFactory)
        {
            _db = db;
        }

        public override async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            var options = context.Options;
            var product = context.Product;

            if (!context.SelectedAttributes.Any() && !options.ApplyPreselectedAttributes && !options.DeterminePriceAdjustments)
            {
                // No selected attributes provided and no preselected attributes should be applied and no price adjustments should be determined,
                // then proceed with pipeline and omit this calculator.
                await next(context);
                return;
            }

            var includeTierPriceAttributePriceAdjustment =
                !options.IgnoreTierPrices &&
                !options.IgnorePercentageTierPricesOnAttributePriceAdjustments &&
                product.HasTierPrices &&
                context.BundleItem == null &&
                context.Quantity > 1;

            var attributes = await options.BatchContext.Attributes.GetOrLoadAsync(product.Id);
            var attributeValues = await GetSelectedAttributeValuesAsync(context, attributes);
            var hasSelectedValues = attributeValues.Any();

            if (!hasSelectedValues && options.DeterminePriceAdjustments)
            {
                // Get price adjustments of ALL attribute values. Do not apply anything to FinalPrice, just return them via context.AttributePriceAdjustments.
                attributeValues = attributes.SelectMany(x => x.ProductVariantAttributeValues).ToList();
            }

            // Ignore attributes that have no relevance for pricing.
            attributeValues = attributeValues
                .Where(x => x.PriceAdjustment != decimal.Zero || x.ValueType == ProductVariantAttributeValueType.ProductLinkage)
                .ToList();

            var linkedProductIds = attributeValues
                .Where(x => x.ValueType == ProductVariantAttributeValueType.ProductLinkage && x.LinkedProductId != 0)
                .Select(x => x.LinkedProductId)
                .Distinct()
                .ToArray();

            var linkedProducts = linkedProductIds.Any()
                ? await _db.Products.AsNoTracking().Where(x => linkedProductIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id)
                : new Dictionary<int, Product>();

            foreach (var value in attributeValues)
            {
                // Calculate price adjustment.
                var adjustment = decimal.Zero;

                if (value.ValueType == ProductVariantAttributeValueType.Simple)
                {
                    if (includeTierPriceAttributePriceAdjustment && value.PriceAdjustment > 0m)
                    {
                        var tierPrices = await context.GetTierPricesAsync();
                        adjustment = GetTierPriceAttributeAdjustment(product, tierPrices, context.Quantity, value.PriceAdjustment);
                    }

                    if (adjustment == 0m)
                    {
                        adjustment = value.PriceAdjustment;
                    }
                }
                else if (value.ValueType == ProductVariantAttributeValueType.ProductLinkage && linkedProducts.TryGetValue(value.LinkedProductId, out var linkedProduct))
                {
                    var childCalculation = await CalculateChildPriceAsync(linkedProduct, context, c =>
                    {
                        c.Quantity = 1;
                        c.Options.IgnoreDiscounts = true;
                    });

                    // Add price of linked product to root final price (unit price * linked product quantity).
                    adjustment = decimal.Multiply(childCalculation.FinalPrice, value.Quantity);
                }

                if (adjustment != 0m)
                {
                    // Apply the adjustment only if selected attributes have been provided.
                    if (hasSelectedValues)
                    {
                        context.FinalPrice += adjustment;
                        context.RegularPrice += adjustment;
                        context.AdditionalCharge += adjustment;
                    }

                    if (options.DeterminePriceAdjustments)
                    {
                        context.AttributePriceAdjustments.Add(new CalculatedPriceAdjustment
                        {
                            RawPriceAdjustment = adjustment,
                            AttributeValue = value,
                            ProductId = product.Id,
                            BundleItemId = context?.BundleItem?.Id
                        });
                    }
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

        protected virtual async Task<List<ProductVariantAttributeValue>> GetSelectedAttributeValuesAsync(CalculatorContext context, IEnumerable<ProductVariantAttribute> attributes)
        {
            var result = new List<ProductVariantAttributeValue>();
            var bundleItem = context?.BundleItem;

            // Apply attributes selected by customer.
            var selections = context.SelectedAttributes
                .Where(x => x.ProductId == context.Product.Id && x.BundleItemId == bundleItem?.Id)
                .Select(x => x.Selection)
                .ToList();

            foreach (var selection in selections)
            {
                var selectedValues = selection.MaterializeProductVariantAttributeValues(attributes);

                // Ignore attributes that are filtered out for a bundle item.
                if (bundleItem?.FilterAttributes ?? false)
                {
                    var filteredValues = selectedValues
                        .Where(x => bundleItem.AttributeFilters.Any(af => af.AttributeId == x.ProductVariantAttributeId && af.AttributeValueId == x.Id));

                    result.AddRange(filteredValues);
                }
                else
                {
                    result.AddRange(selectedValues);
                }
            }

            // Apply attributes preselected by merchant.
            if (context.Options.ApplyPreselectedAttributes)
            {
                // Ignore already applied values.
                var appliedValueIds = result.Select(x => x.Id).ToArray();
                var preselectedValues = await context.GetPreselectedAttributeValuesAsync();

                result.AddRange(preselectedValues.Where(x => !appliedValueIds.Contains(x.Id)));
            }

            return result;
        }
    }
}
