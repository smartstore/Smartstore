using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data;

namespace Smartstore.Core.Catalog.Products
{
    public static partial class IProductServiceExtensions
    {
        /// <summary>
        /// Adjusts product inventory.
        /// </summary>
        /// <param name="productService">Product service.</param>
        /// <param name="sci">Shopping cart item.</param>
        /// <param name="decrease">A value indicating whether to increase or descrease product stock quantity.</param>
        /// <returns>Adjust inventory result.</returns>
        public static async Task<AdjustInventoryResult> AdjustInventoryAsync(this IProductService productService, OrganizedShoppingCartItem sci, bool decrease)
        {
            Guard.NotNull(productService, nameof(productService));
            Guard.NotNull(sci, nameof(sci));

            if (sci.Item.Product.ProductType == ProductType.BundledProduct && sci.Item.Product.BundlePerItemShoppingCart)
            {
                if (sci.ChildItems != null)
                {
                    foreach (var child in sci.ChildItems.Where(x => x.Item.Id != sci.Item.Id))
                    {
                        await productService.AdjustInventoryAsync(child.Item.Product, decrease, sci.Item.Quantity * child.Item.Quantity, child.Item.AttributesXml);
                    }
                }

                return new AdjustInventoryResult();
            }
            else
            {
                return await productService.AdjustInventoryAsync(sci.Item.Product, decrease, sci.Item.Quantity, sci.Item.AttributesXml);
            }
        }

        // TODO: (mg) (core) Get SmartDbContext from somewhere in ProductService.AdjustInventoryAsync extension method.
        /// <summary>
        /// Adjusts product inventory.
        /// </summary>
        /// <param name="productService">Product service.</param>
        /// <param name="orderItem">Order item.</param>
        /// <param name="decrease">A value indicating whether to increase or descrease product stock quantity.</param>
        /// <param name="quantity">The quantity to adjust.</param>
        /// <returns>Adjust inventory result.</returns>
        public static async Task<AdjustInventoryResult> AdjustInventoryAsync(this IProductService productService, SmartDbContext db, OrderItem orderItem, bool decrease, int quantity)
        {
            Guard.NotNull(productService, nameof(productService));
            Guard.NotNull(orderItem, nameof(orderItem));

            if (orderItem.Product.ProductType == ProductType.BundledProduct && orderItem.Product.BundlePerItemShoppingCart)
            {
                if (orderItem.BundleData.HasValue())
                {
                    var bundleData = orderItem.GetBundleData();
                    if (bundleData.Any())
                    {
                        var productIds = bundleData
                            .Select(x => x.ProductId)
                            .Distinct()
                            .ToArray();

                        var products = await db.Products
                            .Where(x => productIds.Contains(x.Id))
                            .ToListAsync();

                        var productsDic = products.ToDictionary(x => x.Id);

                        foreach (var item in bundleData)
                        {
                            if (productsDic.TryGetValue(item.ProductId, out var product))
                            {
                                await productService.AdjustInventoryAsync(product, decrease, quantity * item.Quantity, item.AttributesXml);
                            }
                        }
                    }
                }

                return new AdjustInventoryResult();
            }
            else
            {
                return await productService.AdjustInventoryAsync(orderItem.Product, decrease, quantity, orderItem.AttributesXml);
            }
        }
    }
}
