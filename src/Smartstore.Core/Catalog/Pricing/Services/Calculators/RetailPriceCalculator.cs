using Smartstore.Core.Data;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// Calculates the retail unit price (MSRP), if any specified for the product.
    /// A retail price is given if <see cref="Product.ComparePrice"/> is not null and 'Product.ComparePriceLabelId' referes to an MSRP label.
    /// </summary>
    [CalculatorUsage(CalculatorTargets.Product | CalculatorTargets.Bundle, CalculatorOrdering.Late + 2)]
    public class RetailPriceCalculator : IPriceCalculator
    {
        private readonly SmartDbContext _db;
        private readonly PriceSettings _priceSettings;

        public RetailPriceCalculator(SmartDbContext db, PriceSettings priceSettings)
        {
            _db = db;
            _priceSettings = priceSettings;
        }

        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            // TODO: (mg) (pricing) comment out RetailPrice code when Product entity is ready.
            //var product = context.Product;

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

            await next(context);
        }
    }
}
