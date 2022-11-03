using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Catalog.Pricing
{
    public interface IPriceLabelService
    {
        /// <summary>
        /// Gets the system default compare price label as defined by <see cref="PriceSettings.DefaultComparePriceLabelId"/>.
        /// </summary>
        PriceLabel GetDefaultComparePriceLabel();

        /// <summary>
        /// Gets the system default regular price label as defined by <see cref="PriceSettings.DefaultRegularPriceLabelId"/>.
        /// </summary>
        PriceLabel GetDefaultRegularPriceLabel();

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
        /// <remarks>
        /// Currently this method just returns the default label by calling <see cref="GetDefaultRegularPriceLabel"/>.
        /// </remarks>
        PriceLabel GetRegularPriceLabel(Product product);

        /// <summary>
        /// Gets a promotion badge for the given calculated price as defined by the badge configuration.
        /// </summary>
        /// <param name="price">The calculated price to get a badge for.</param>
        /// <returns>A value tuple: (localized badge label, badge variant).</returns>
        (LocalizedValue<string>, string) GetPricePromoBadge(CalculatedPrice price);

        /// <summary>
        /// Gets a promotion countdown text for the given calculated price as defined by countdown configuration.
        /// </summary>
        /// <param name="price">The calculated price to get a countdown text for.</param>
        /// <returns>
        /// The localized and humanized countdown text, e.g.: "Ends in 3 h, 12 min.".
        /// Returns <c>null</c> if offer is not limited or remaining time is larger than threshold.
        /// </returns>
        LocalizedString GetPromoCountdownText(CalculatedPrice price);
    }
}
