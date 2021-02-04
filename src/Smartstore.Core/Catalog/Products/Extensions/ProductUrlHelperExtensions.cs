using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Content.Seo;

namespace Smartstore.Core.Catalog.Products
{
    /// <summary>
    /// Extension methods for product URL helper.
    /// </summary>
    public static partial class ProductUrlHelperExtensions
    {
        /// <summary>
        /// Creates a product URL including variant query string.
        /// </summary>
        /// <param name="helper">Product URL helper.</param>
        /// <param name="productId">Product identifier.</param>
        /// <param name="productSlug">Product URL slug.</param>
        /// <param name="bundleItemId">Bundle item identifier. 0 if it's not a bundle item.</param>
        /// <param name="variantValues">Variant values<./param>
        /// <returns>Product URL.</returns>
        public static string GetProductUrl(
            this ProductUrlHelper helper,
            int productId,
            string productSlug,
            int bundleItemId,
            params ProductVariantAttributeValue[] variantValues)
        {
            Guard.NotNull(helper, nameof(helper));
            Guard.NotZero(productId, nameof(productId));

            var query = new ProductVariantQuery();

            foreach (var value in variantValues)
            {
                var attribute = value.ProductVariantAttribute;

                query.AddVariant(new ProductVariantQueryItem(value.Id.ToString())
                {
                    ProductId = productId,
                    BundleItemId = bundleItemId,
                    AttributeId = attribute.ProductAttributeId,
                    VariantAttributeId = attribute.Id,
                    Alias = attribute.ProductAttribute.Alias,
                    ValueAlias = value.Alias
                });
            }

            return helper.GetProductUrl(query, productSlug);
        }

        /// <summary>
        /// Creates a product URL including variant query string.
        /// </summary>
        /// <param name="helper">Product URL helper</param>
        /// <param name="product">Product entity</param>
        /// <param name="variantValues">Variant values</param>
        /// <returns>Product URL</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<string> GetProductUrlAsync(this ProductUrlHelper helper, Product product, params ProductVariantAttributeValue[] variantValues)
        {
            Guard.NotNull(helper, nameof(helper));
            Guard.NotNull(product, nameof(product));

            return helper.GetProductUrl(product.Id, await product.GetActiveSlugAsync(), 0, variantValues);
        }

        /// <summary>
        /// Creates a product URL including variant query string.
        /// </summary>
        /// <param name="helper">Product URL helper.</param>
        /// <param name="productSlug">Product URL slug.</param>
        /// <param name="cartItem">Organized shopping cart item.</param>
        /// <returns>Product URL.</returns>
        public static async Task<string> GetProductUrlAsync(this ProductUrlHelper helper, string productSlug, OrganizedShoppingCartItem cartItem)
        {
            Guard.NotNull(helper, nameof(helper));
            Guard.NotNull(cartItem, nameof(cartItem));

            var query = new ProductVariantQuery();
            var product = cartItem.Item.Product;

            if (product.ProductType != ProductType.BundledProduct)
            {
                await helper.DeserializeQueryAsync(query, product.Id, cartItem.Item.AttributeSelection);
            }
            else if (cartItem.ChildItems != null && product.BundlePerItemPricing)
            {
                foreach (var childItem in cartItem.ChildItems.Where(x => x.Item.Id != cartItem.Item.Id))
                {
                    await helper.DeserializeQueryAsync(query, childItem.Item.ProductId, childItem.Item.AttributeSelection, childItem.BundleItemData.Item.Id);
                }
            }

            return helper.GetProductUrl(query, productSlug);
        }

        /// <summary>
        /// Creates a product URL including variant query string.
        /// </summary>
        /// <param name="helper">Product URL helper.</param>
        /// <param name="productSlug">Product URL slug.</param>
        /// <param name="orderItem">Order item.</param>
        /// <returns>Product URL.</returns>
        public static async Task<string> GetProductUrlAsync(this ProductUrlHelper helper, string productSlug, OrderItem orderItem)
        {
            Guard.NotNull(helper, nameof(helper));
            Guard.NotNull(orderItem, nameof(orderItem));

            var query = new ProductVariantQuery();

            if (orderItem.Product.ProductType != ProductType.BundledProduct)
            {
                await helper.DeserializeQueryAsync(query, orderItem.ProductId, orderItem.AttributeSelection);
            }
            else if (orderItem.Product.BundlePerItemPricing && orderItem.BundleData.HasValue())
            {
                var bundleData = orderItem.GetBundleData();

                foreach (var item in bundleData)
                {
                    await helper.DeserializeQueryAsync(query, item.ProductId, item.AttributeSelection, item.BundleItemId);
                }
            }

            return helper.GetProductUrl(query, productSlug);
        }

    }
}
