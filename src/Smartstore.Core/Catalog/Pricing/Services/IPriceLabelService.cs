using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Catalog.Pricing
{
    public interface IPriceLabelService
    {
        /// <summary>
        /// Gets the compare price label for a given product. Falls back to
        /// system default compare label if product has no label.
        /// </summary>
        /// <param name="product">The product to get label for.</param>
        PriceLabel GetComparePriceLabel(Product product);

        /// <summary>
        /// Gets the regular price label for a given product. Falls back to
        /// system default price label if product has no label.
        /// </summary>
        /// <param name="product">The product to get label for.</param>
        PriceLabel GetRegularPriceLabel(Product product);

        /// <summary>
        /// Gets a promotion badge for the calculated price as defined by the badge configuration.
        /// </summary>
        /// <param name="price">The calculated price to get a badge for.</param>
        /// <returns>A value tuple: (localized badge label, badge variant).</returns>
        (LocalizedValue<string>, string) GetPricePromoBadge(CalculatedPrice price);
    }
}
