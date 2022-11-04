using Smartstore.Core.Data;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// Calculates the regular price and the retail unit price (MSRP), if any specified for the product.
    /// The regular price is usually <see cref="Product.Price"/>, <see cref="Product.ComparePrice"/> or <see cref="Product.SpecialPrice"/>.
    /// A retail price is given if <see cref="Product.ComparePrice"/> is not null and 'Product.ComparePriceLabelId' referes to an MSRP label.
    /// </summary>
    [CalculatorUsage(CalculatorTargets.Product | CalculatorTargets.Bundle, CalculatorOrdering.Default)]
    public class ComparePriceCalculator : IPriceCalculator
    {
        private readonly SmartDbContext _db;
        private readonly PriceSettings _priceSettings;

        public ComparePriceCalculator(SmartDbContext db, PriceSettings priceSettings)
        {
            _db = db;
            _priceSettings = priceSettings;
        }

        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            // TODO: (mg) (pricing)
            // Your first approach was correct. There is nothing to calculate here, just post-processing,
            // which shouldn't be done in a calculator, but in PriceCalculationService.CreateCalculatedPrice().
            // Here are the rules:
            // - RetailPrice is: ComparePrice, if > Price and Label = MSRP
            // - RegularPrice is: Special or Discount price, but not RetailPrice (see "Spickzettel" for details)
            // - Saving refers to: RegularPrice. If no RegularPrice exists, then to RetailPrice.
            // - ValidUntilUtc is: Either SpecialPriceEndDate or the applied discount's EndDate.
            // Remove this class when done.

            // Process the whole pipeline.
            await next(context);

            context.RegularPrice = GetRegularPrice(context);

            // TODO: (mg) (pricing) comment out Retail Price code when Product entity is ready.

            // Retail price.
            //// INFO: (mg) (pricing) we cannot determine labels here. Only IPriceLabelService can compute/provide labels.
            //// INFO: (mg) (pricing) The setting "AlwaysDisplayRetailPrice" is UI-bound. No need to check for it here.
            //if (product.ComparePrice > product.Price
            //    && product.ComparePriceLabelId.HasValue
            //    && (!context.RegularPrice.HasValue || _priceSettings.AlwaysDisplayRetailPrice))
            //{
            //    await _db.LoadReferenceAsync(product, x => x.ComparePriceLabel);

            //    if (product.ComparePriceLabel?.IsRetailPrice ?? false)
            //    {
            //        context.RetailPrice = product.ComparePrice;
            //    }
            //}

            #region Temp only (remove later)

            var product = context.Product;

            if (product.ComparePrice > product.Price && (context.RegularPrice == null || product.ComparePrice != context.RegularPrice))
            {
                context.RetailPrice = product.ComparePrice;
            }

            #endregion
        }

        protected virtual decimal? GetRegularPrice(CalculatorContext context)
        {
            var product = context.Product;

            if (context.DiscountAmount > 0)
            {
                if (context.OfferPrice.HasValue)
                {
                    if (product.ComparePrice > 0)
                    {
                        return Math.Min(context.OfferPrice.Value, product.ComparePrice);
                    }
                    else
                    {
                        return Math.Min(context.OfferPrice.Value, product.Price);
                    }
                }
                else
                {
                    if (product.ComparePrice > 0)
                    {
                        return Math.Min(product.Price, product.ComparePrice);
                    }
                    else
                    {
                        return product.Price;
                    }
                }
            }

            if (context.OfferPrice.HasValue)
            {
                if (product.ComparePrice > 0)
                {
                    // PAngV: "Price" would not be allowed if greater than "ComparePrice".
                    return Math.Min(product.Price, product.ComparePrice);
                }
                else
                {
                    return product.Price;
                }
            }

            if (product.ComparePrice > product.Price)
            {
                return product.ComparePrice;
            }

            return null;
        }
    }
}
