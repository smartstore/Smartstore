using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Catalog.Products
{
    public static partial class IProductServiceExtensions
    {
        /// <summary>
        /// Adjusts product inventory. The caller is responsible for database commit.
        /// </summary>
        /// <param name="productService">Product service.</param>
        /// <param name="item">Shopping cart item.</param>
        /// <param name="decrease">A value indicating whether to increase or descrease product stock quantity.</param>
        /// <returns>Adjust inventory result.</returns>
        public static async Task<AdjustInventoryResult> AdjustInventoryAsync(this IProductService productService, OrganizedShoppingCartItem item, bool decrease)
        {
            Guard.NotNull(productService, nameof(productService));
            Guard.NotNull(item, nameof(item));

            if (item.Item.Product.ProductType == ProductType.BundledProduct && item.Item.Product.BundlePerItemShoppingCart)
            {
                if (item.ChildItems != null)
                {
                    foreach (var child in item.ChildItems.Where(x => x.Item.Id != item.Item.Id))
                    {
                        await productService.AdjustInventoryAsync(
                            child.Item.Product,
                            child.Item.AttributeSelection,
                            decrease,
                            item.Item.Quantity * child.Item.Quantity);
                    }
                }

                return new AdjustInventoryResult();
            }
            else
            {
                return await productService.AdjustInventoryAsync(
                    item.Item.Product,
                    item.Item.AttributeSelection,
                    decrease,
                    item.Item.Quantity);
            }
        }
    }
}
