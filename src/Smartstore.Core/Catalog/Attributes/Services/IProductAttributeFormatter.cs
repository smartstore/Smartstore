using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Catalog.Attributes
{
    /// <summary>
    /// Product attribute formatter.
    /// </summary>
    public partial interface IProductAttributeFormatter
    {
        /// <summary>
        /// Formats product and gift card attributes.
        /// </summary>
        /// <param name="attributes">Attribute selection.</param>
        /// <param name="product">Product entity.</param>
        /// <param name="customer">Customer entity. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="separator">Separator between the formatted attributes.</param>
        /// <param name="htmlEncode">A value indicating whether to HTML encode values.</param>
        /// <param name="includePrices">A value indicating whether to include prices.</param>
        /// <param name="includeProductAttributes">A value indicating whether to include product attributes.</param>
        /// <param name="includeGiftCardAttributes">A value indicating whether to include gift card attributes.</param>
        /// <param name="includeHyperlinks">A value indicating whether to include HTML hyperlinks.</param>
        /// <param name="batchContext">The product batch context. For example used to load all products of a shopping cart in one go. Will be created internally if <c>null</c>.</param>
        /// <returns>Formatted attributes.</returns>
        Task<string> FormatAttributesAsync(
            ProductVariantAttributeSelection attributes,
            Product product,
            Customer customer = null,
            string separator = "<br />",
            bool htmlEncode = true,
            bool includePrices = true,
            bool includeProductAttributes = true,
            bool includeGiftCardAttributes = true,
            bool includeHyperlinks = true,
            ProductBatchContext batchContext = null);
    }
}