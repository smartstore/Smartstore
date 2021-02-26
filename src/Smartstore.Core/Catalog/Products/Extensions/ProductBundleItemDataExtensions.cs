using System.Collections.Generic;
using Smartstore.Core.Seo;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Catalog.Products
{
    public static partial class ProductBundleItemDataExtensions
    {
        public static void ToOrderData(
            this ProductBundleItemData bundleItemData, 
            IList<ProductBundleItemOrderData> bundleData,
            decimal priceWithDiscount = decimal.Zero,
            string attributesXml = null,
            string attributesInfo = null)
        {
            var item = bundleItemData.ToOrderData(priceWithDiscount, attributesXml, attributesInfo);

            if (item != null && item.ProductId != 0 && item.BundleItemId != 0)
            {
                bundleData.Add(item);
            }
        }

        public static ProductBundleItemOrderData ToOrderData(
            this ProductBundleItemData bundleItemData,
            decimal priceWithDiscount = decimal.Zero,
            string attributesXml = null,
            string attributesInfo = null)
        {
            if (bundleItemData?.Item == null)
            {
                return null;
            }

            var item = bundleItemData.Item;
            string bundleItemName = item.GetLocalized(x => x.Name);

            var bundleData = new ProductBundleItemOrderData
            {
                BundleItemId = item.Id,
                ProductId = item.ProductId,
                Sku = item.Product.Sku,
                ProductName = bundleItemName ?? item.Product.GetLocalized(x => x.Name),
                ProductSeName = item.Product.GetActiveSlug(),
                VisibleIndividually = item.Product.Visibility != ProductVisibility.Hidden,
                Quantity = item.Quantity,
                DisplayOrder = item.DisplayOrder,
                PriceWithDiscount = priceWithDiscount,
                RawAttributes = attributesXml,
                AttributesInfo = attributesInfo,
                PerItemShoppingCart = item.BundleProduct.BundlePerItemShoppingCart
            };

            return bundleData;
        }
    }
}
