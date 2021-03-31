using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Core.Catalog.Pricing
{
    // TODO: (mg) (core) Describe pricing pipeline when ready.

    public partial class PriceCalculationAttributes
    {
        public PriceCalculationAttributes(ProductVariantAttributeSelection selection, int productId)
        {
            Guard.NotNull(selection, nameof(selection));
            Guard.NotZero(productId, nameof(productId));

            Selection = selection;
            ProductId = productId;
        }

        public ProductVariantAttributeSelection Selection { get; private set; }
        public int ProductId { get; private set; }
        public int? BundleItemId { get; init; }
    }
}
