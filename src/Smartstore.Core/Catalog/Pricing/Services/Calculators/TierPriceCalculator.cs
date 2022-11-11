using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// Calculates the minimum tier price and applies it if it is lower than the FinalPrice.
    /// Tier prices of bundle items are ignored if per-item pricing is activated for the bundle.
    /// </summary>
    [CalculatorUsage(CalculatorTargets.Product | CalculatorTargets.Bundle, CalculatorOrdering.Default + 100)]
    public class TierPriceCalculator : IPriceCalculator
    {
        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            var product = context.Product;
            var options = context.Options;
            var tierPriceEntity = (TierPrice)null;
            var processTierPrices = !options.IgnoreTierPrices
                && !options.IgnoreDiscounts
                && product.HasTierPrices
                && context.BundleItem == null
                && !(product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing);

            if (processTierPrices)
            {
                var tierPrices = await context.GetTierPricesAsync();

                // Put minimum tier price to context because it's required for discount calculation.
                var (amount, tp) = GetMinimumTierPrice(product, tierPrices, context.Quantity);
                context.MinTierPrice = amount;
                tierPriceEntity = tp;

                if (context.Options.DetermineLowestPrice && !context.HasPriceRange)
                {
                    context.HasPriceRange = tierPrices.Any() && !(tierPrices.Count == 1 && tierPrices.First().Quantity <= 1);
                }
            }

            // Process the whole pipeline. We need the result of discount calculation.
            await next(context);

            if (processTierPrices && context.MinTierPrice.HasValue)
            {
                // Wrong result:
                //context.FinalPrice = Math.Min(context.FinalPrice, context.MinTierPrice.Value);

                // Apply the minimum tier price if it achieves a lower price than the discounted FinalPrice
                // but exclude additional charge from comparing.
                context.FinalPrice -= context.AdditionalCharge;

                if (context.MinTierPrice.Value < context.FinalPrice)
                {
                    context.FinalPrice = context.MinTierPrice.Value;
                    context.AppliedTierPrice = tierPriceEntity;

                    // Apply discount on minimum tier price (if any).
                    var discountOnTierPrice = context.CalculatedDiscounts
                        .Where(x => x.Origin == nameof(context.MinTierPrice))
                        .OrderByDescending(x => x.DiscountAmount)
                        .FirstOrDefault();

                    if (discountOnTierPrice != null)
                    {
                        context.AppliedDiscounts.Add(discountOnTierPrice.Discount);
                        context.DiscountAmount += discountOnTierPrice.DiscountAmount;
                        context.FinalPrice -= discountOnTierPrice.DiscountAmount;
                    }
                }

                context.FinalPrice += context.AdditionalCharge;
            }
        }

        protected virtual (decimal? Amount, TierPrice TierPrice) GetMinimumTierPrice(Product product, IEnumerable<TierPrice> tierPrices, int quantity)
        {
            var amount = (decimal?)null;
            var tierPrice = (TierPrice)null;
            var previousQty = 1;

            foreach (var tp in tierPrices)
            {
                if (quantity < tp.Quantity || tp.Quantity < previousQty)
                {
                    continue;
                }

                if (tp.CalculationMethod == TierPriceCalculationMethod.Fixed)
                {
                    amount = tp.Price;
                }
                else if (tp.CalculationMethod == TierPriceCalculationMethod.Percental)
                {
                    amount = product.Price - (product.Price / 100m * tp.Price);
                }
                else
                {
                    amount = product.Price - tp.Price;
                }

                tierPrice = tp;
                previousQty = tp.Quantity;
            }

            return (amount, tierPrice);
        }
    }
}
