using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Pricing
{
    public static partial class IPriceCalculationServiceExtensions
    {
        /// <summary>
        /// Calculates the price adjustments of product attributes, usually <see cref="ProductVariantAttributeValue.PriceAdjustment"/>.
        /// Typically used to display price adjustments of selected attributes on the cart page.
        /// The calculated adjustment is always a unit price.
        /// </summary>
        /// <param name="priceCalculationService">Price calculation service.</param>
        /// <param name="product">The product.</param>
        /// <param name="selection">Attribute selection. If <c>null</c> then the price adjustments of all attributes of <paramref name="product"/> are determined.</param>
        /// <param name="quantity">
        /// The product quantity. May have impact on the price, e.g. if tier prices are applied to price adjustments.
        /// Note that the calculated price is always the unit price.
        /// </param>
        /// <param name="options">Price calculation options. The default options are used if <c>null</c>.</param>
        /// <returns>Price adjustments of selected attributes. Key: <see cref="BaseEntity.Id"/>, value: attribute price adjustment.</returns>
        public static async Task<IDictionary<int, CalculatedPriceAdjustment>> CalculateAttributePriceAdjustmentsAsync(
            this IPriceCalculationService priceCalculationService,
            Product product,
            ProductVariantAttributeSelection selection = null,
            int quantity = 1,
            PriceCalculationOptions options = null)
        {
            Guard.NotNull(priceCalculationService, nameof(priceCalculationService));

            options ??= priceCalculationService.CreateDefaultOptions(false);

            var context = new PriceCalculationContext(product, quantity, options);
            context.Options.DeterminePriceAdjustments = true;
            context.Options.TaxFormat = null;

            context.AddSelectedAttributes(selection, product.Id);

            var price = await priceCalculationService.CalculatePriceAsync(context);
            return price.AttributePriceAdjustments.ToDictionarySafe(x => x.AttributeValue.Id);
        }

        /// <summary>
        /// Gets the base price info for a product.
        /// </summary>
        /// <param name="priceCalculationService">Price calculation service.</param>
        /// <param name="product">The product to get the base price info for.</param>
        /// <param name="options">Price calculation options. The default options are used if <c>null</c>.</param>
        /// <returns>Base price info.</returns>
        public static async Task<string> GetBasePriceInfoAsync(this IPriceCalculationService priceCalculationService, Product product, PriceCalculationOptions options = null)
        {
            Guard.NotNull(priceCalculationService, nameof(priceCalculationService));
            Guard.NotNull(product, nameof(product));

            if (!product.BasePriceHasValue || product.BasePriceAmount == 0)
            {
                return string.Empty;
            }

            options ??= priceCalculationService.CreateDefaultOptions(false);

            var context = new PriceCalculationContext(product, options);
            var price = await priceCalculationService.CalculatePriceAsync(context);

            return priceCalculationService.GetBasePriceInfo(
                product,
                price.FinalPrice,
                options.TargetCurrency,
                displayTaxSuffix: options.TaxFormat.IsEmpty() ? false : null);
        }
    }
}
