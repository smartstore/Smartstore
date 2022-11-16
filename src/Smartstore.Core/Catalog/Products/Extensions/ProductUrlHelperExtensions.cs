using System.Runtime.CompilerServices;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Seo;

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
        /// <param name="variantValues">Variant values</param>
        /// <returns>Product URL</returns>
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

            return helper.GetProductUrl(productSlug, query);
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
                await helper.AddAttributesToQueryAsync(query, cartItem.Item.AttributeSelection, product.Id);
            }
            else if (cartItem.ChildItems != null && product.BundlePerItemPricing)
            {
                foreach (var childItem in cartItem.ChildItems.Where(x => x.Item.Id != cartItem.Item.Id))
                {
                    await helper.AddAttributesToQueryAsync(query, childItem.Item.AttributeSelection, childItem.Item.ProductId, childItem.Item.BundleItem.Id);
                }
            }

            return helper.GetProductUrl(productSlug, query);
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
                await helper.AddAttributesToQueryAsync(query, orderItem.AttributeSelection, orderItem.ProductId);
            }
            else if (orderItem.Product.BundlePerItemPricing && orderItem.BundleData.HasValue())
            {
                var bundleData = orderItem.GetBundleData();

                foreach (var item in bundleData)
                {
                    await helper.AddAttributesToQueryAsync(query, item.AttributeSelection, item.ProductId, item.BundleItemId);
                }
            }

            return helper.GetProductUrl(productSlug, query);
        }

    }
}
