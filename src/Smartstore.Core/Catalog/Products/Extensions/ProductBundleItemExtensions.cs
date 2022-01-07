using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;

namespace Smartstore.Core.Catalog.Products
{
    public static partial class ProductBundleItemExtensions
    {
        /// <summary>
        /// Gets a value indicating whether an attribute value is filtered out.
        /// </summary>
        /// <param name="bundleItem">Bundle item.</param>
        /// <param name="value">Variant attribute value.</param>
        /// <param name="filter">Matched filter.</param>
        /// <returns>A value indicating whether an attribute value is filtered out.</returns>
        public static bool IsFilteredOut(this ProductBundleItem bundleItem, ProductVariantAttributeValue value, out ProductBundleItemAttributeFilter filter)
        {
            if (bundleItem != null && value != null && bundleItem.FilterAttributes)
            {
                filter = bundleItem.AttributeFilters.FirstOrDefault(x => x.AttributeId == value.ProductVariantAttributeId && x.AttributeValueId == value.Id);
                return filter == null;
            }

            filter = null;
            return false;
        }

        /// <summary>
        /// Gets the localized name of a bundle item. Falls back to the localized product name if empty.
        /// </summary>
        /// <param name="bundleItem">Bundle item.</param>
        /// <returns>The localized name of the bundle item.</returns>
        public static string GetLocalizedName(this ProductBundleItem bundleItem)
        {
            if (bundleItem != null)
            {
                string name = bundleItem.GetLocalized(x => x.Name);
                return name.HasValue() ? name : bundleItem.Product.GetLocalized(x => x.Name);
            }

            return null;
        }

        /// <summary>
        /// Gets a <see cref="ProductBundleItemOrderData"/> for a given <see cref="ProductBundleItem"/>.
        /// </summary>
        /// <param name="bundleItem">Bundle item.</param>
        /// <param name="priceWithDiscount">The price with discount.</param>
        /// <param name="rawAttributes">The raw attributes string in XML or JSON format.</param>
        /// <param name="attributesInfo">A localized and formatted description of selected attributes.</param>
        /// <returns><see cref="ProductBundleItemOrderData"/>.</returns>
        public static ProductBundleItemOrderData ToOrderData(
            this ProductBundleItem bundleItem,
            decimal priceWithDiscount = decimal.Zero,
            string rawAttributes = null,
            string attributesInfo = null)
        {
            if (bundleItem == null)
            {
                return null;
            }

            string bundleItemName = bundleItem.GetLocalized(x => x.Name);

            var bundleData = new ProductBundleItemOrderData
            {
                BundleItemId = bundleItem.Id,
                ProductId = bundleItem.ProductId,
                Sku = bundleItem.Product.Sku,
                ProductName = bundleItemName ?? bundleItem.Product.GetLocalized(x => x.Name),
                ProductSeName = bundleItem.Product.GetActiveSlug(),
                VisibleIndividually = bundleItem.Product.Visibility != ProductVisibility.Hidden,
                Quantity = bundleItem.Quantity,
                DisplayOrder = bundleItem.DisplayOrder,
                PriceWithDiscount = priceWithDiscount,
                RawAttributes = rawAttributes,
                AttributesInfo = attributesInfo,
                PerItemShoppingCart = bundleItem.BundleProduct.BundlePerItemShoppingCart
            };

            return bundleData;
        }
    }
}
