using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Pricing
{
    public interface IPriceLabelService
    {
        /// <summary>
        /// Gets the compare price label for a given product. Falls back to
        /// system default compare label if product has no label.
        /// </summary>
        /// <param name="product">The product to get label for.</param>
        Task<PriceLabel> GetComparePriceLabelAsync(Product product);

        /// <summary>
        /// Gets the regular price label for a given product. Falls back to
        /// system default price label if product has no label.
        /// </summary>
        /// <param name="product">The product to get label for.</param>
        Task<PriceLabel> GetRegularPriceLabelAsync(Product product);
    }
}
