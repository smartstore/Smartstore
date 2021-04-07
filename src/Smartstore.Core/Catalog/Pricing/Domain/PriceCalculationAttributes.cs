using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Represents product attributes to be taken into account when calculating a product price.
    /// </summary>
    public partial class PriceCalculationAttributes
    {
        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="selection">The selected product attributes.</param>
        /// <param name="productId">The identifier of the related product.</param>
        public PriceCalculationAttributes(ProductVariantAttributeSelection selection, int productId)
        {
            Guard.NotNull(selection, nameof(selection));
            Guard.NotZero(productId, nameof(productId));

            Selection = selection;
            ProductId = productId;
        }

        /// <summary>
        /// Gets the selected product attributes.
        /// </summary>
        public ProductVariantAttributeSelection Selection { get; private set; }

        /// <summary>
        /// Gets the identifier of the related product.
        /// </summary>
        public int ProductId { get; private set; }

        /// <summary>
        /// Gets the bundle item identifier if the related product is a bundle item.
        /// </summary>
        public int? BundleItemId { get; init; }
    }
}
