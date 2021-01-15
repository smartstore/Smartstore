using System.Threading.Tasks;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Customers;

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
        /// <param name="attributes">Attribute selection</param>
        /// <param name="product">Product entity.</param>
        /// <param name="customer">Customer entity.</param>
        /// <param name="separator">Separator between the formatted attributes.</param>
        /// <param name="htmlEncode">A value indicating whether to HTML encode values.</param>
        /// <param name="includePrices">A value indicating whether to include prices.</param>
        /// <param name="includeProductAttributes">A value indicating whether to include product attributes.</param>
        /// <param name="includeGiftCardAttributes">A value indicating whether to include gift card attributes.</param>
        /// <param name="includeHyperlinks">A value indicating whether to include HTML hyperlinks.</param>
        /// <returns>Formatted attributes.</returns>
        Task<string> FormatAttributesAsync(
            ProductVariantAttributeSelection attributes,
            Product product,
            Customer customer,
            string separator = "<br />",
            bool htmlEncode = true,
            bool includePrices = true,
            bool includeProductAttributes = true,
            bool includeGiftCardAttributes = true,
            bool includeHyperlinks = true);
    }
}
