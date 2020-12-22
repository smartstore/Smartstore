using System.Threading.Tasks;

namespace Smartstore.Core.Catalog.Products
{
    public static partial class ProductServiceExtensions
    {
        // TODO: (mg) (core) Complete ProductService.AdjustInventoryAsync method.
        /// <summary>
        /// Adjusts product inventory.
        /// </summary>
        /// <param name="productService">Product service.</param>
        /// <param name="sci">Shopping cart item.</param>
        /// <param name="decrease">A value indicating whether to increase or descrease product stock quantity.</param>
        /// <returns>Adjust inventory result.</returns>
        public static async Task<AdjustInventoryResult> AdjustInventoryAsync(this IProductService productService, object sci, bool decrease)
        {
            Guard.NotNull(productService, nameof(productService));
            Guard.NotNull(sci, nameof(sci));

            return await Task.FromResult(new AdjustInventoryResult());
        }

        // TODO: (mg) (core) Complete ProductService.AdjustInventoryAsync method.
        /// <summary>
        /// Adjusts product inventory.
        /// </summary>
        /// <param name="productService">Product service.</param>
        /// <param name="orderItem">Order item.</param>
        /// <param name="decrease">A value indicating whether to increase or descrease product stock quantity.</param>
        /// <param name="quantity">The quantity to adjust.</param>
        /// <returns>Adjust inventory result.</returns>
        public static async Task<AdjustInventoryResult> AdjustInventoryAsync(this IProductService productService, object orderItem, bool decrease, int quantity)
        {
            Guard.NotNull(productService, nameof(productService));
            Guard.NotNull(orderItem, nameof(orderItem));


            return await Task.FromResult(new AdjustInventoryResult());
        }
    }
}
