using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Common;

namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Represents a calculated product attribute price adjustment, usually <see cref="ProductVariantAttributeValue.PriceAdjustment"/>.
    /// </summary>
    public partial class CalculatedPriceAdjustment
    {
        /// <summary>
        /// The raw calculated price adjustment in primary currency without tax.
        /// </summary>
        internal decimal RawPriceAdjustment { get; init; }

        /// <summary>
        /// Gets or sets the calculated attribute price converted to <see cref="PriceCalculationOptions.TargetCurrency"/> and included tax.
        /// </summary>
        public Money Price { get; set; }

        /// <summary>
        /// Gets the product attribute value.
        /// </summary>
        public ProductVariantAttributeValue AttributeValue { get; init; }

        /// <summary>
        /// Gets the identifier of the related product.
        /// </summary>
        public int ProductId { get; init; }

        /// <summary>
        /// Gets the bundle item identifier if the related product is a bundle item.
        /// </summary>
        public int? BundleItemId { get; init; }
    }
}
