using Smartstore.Domain;

namespace Smartstore.Core.Catalog.Attributes
{
    /// <inheritdoc/>
    public class ProductVariantAttributeSelection : AttributeSelection
    {
        /// <inheritdoc cref="AttributeSelection(string, string, string)"/>
        public ProductVariantAttributeSelection(string attributesRaw)
            : base(attributesRaw, "ProductVariantAttribute")
        {
        }
    }
}