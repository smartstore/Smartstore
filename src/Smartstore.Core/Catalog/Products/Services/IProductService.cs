using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Catalog.Products
{
    /// <summary>
    /// Product service interface.
    /// </summary>
    public partial interface IProductService
    {
        /// <summary>
        /// Gets a product by SKU, GTIN or MPN.
        /// </summary>
        /// <param name="identificationNumber">SKU, GTIN or MPN.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden products.</param>
        /// <param name="tracked">A value indicating whether to put prefetched entities to EF change tracker.</param>
        /// <returns>Found product or variant combination.</returns>
        Task<(Product Product, ProductVariantAttributeCombination VariantCombination)> GetProductByIdentificationNumberAsync(
            string identificationNumber,
            bool includeHidden = false,
            bool tracked = false);

        /// <summary>
        /// Gets low stock products.
        /// </summary>
        /// <param name="tracked">A value indicating whether to put prefetched entities to EF change tracker.</param>
        /// <returns>List of low stock products.</returns>
        Task<IList<Product>> GetLowStockProductsAsync(bool tracked = false);

        /// <summary>
        /// Gets product tags by product identifiers.
        /// </summary>
        /// <param name="productIds">Product identifiers.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden products and product tags.</param>
        /// <returns>Map of product tags.</returns>
        Task<Multimap<int, ProductTag>> GetProductTagsByProductIdsAsync(int[] productIds, bool includeHidden = false);

        /// <summary>
        /// Get applied discounts by product identifiers.
        /// </summary>
        /// <param name="productIds">Product identifiers.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden products.</param>
        /// <returns>Map of discounts.</returns>
        Task<Multimap<int, Discount>> GetAppliedDiscountsByProductIdsAsync(int[] productIds, bool includeHidden = false);

        /// <summary>
        /// Applies the product review totals to a product entity. The caller is responsible for database commit.
        /// </summary>
        /// <param name="product">Product entity.</param>
        void ApplyProductReviewTotals(Product product);

        /// <summary>
        /// Adjusts product inventory. The caller is responsible for database commit.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <param name="decrease">A value indicating whether to increase or descrease product stock quantity.</param>
        /// <param name="quantity">The quantity to adjust.</param>
        /// <param name="attributesXml">Attributes XML data.</param>
        /// <returns>Adjust inventory result.</returns>
        Task<AdjustInventoryResult> AdjustInventoryAsync(Product product, bool decrease, int quantity, string attributesXml);

        /// <summary>
        /// Ensures the existence of all mutually related products.
        /// </summary>
        /// <param name="productId1">First product identifier.</param>
        /// <returns>Number of related products added.</returns>
        Task<int> EnsureMutuallyRelatedProductsAsync(int productId1);

        /// <summary>
        /// Ensures the existence of all mutually cross selling products.
        /// </summary>
        /// <param name="productId1">First product identifier.</param>
        /// <returns>Number of cross sell products added.</returns>
        Task<int> EnsureMutuallyCrossSellProductsAsync(int productId1);

        /// <summary>
        /// Gets a cross-sell products by shopping cart.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="numberOfProducts">Number of products to return.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden products.</param>
        /// <returns>List of products.</returns>
        Task<IList<Product>> GetCrossSellProductsByShoppingCartAsync(
            IList<OrganizedShoppingCartItem> cart,
            int numberOfProducts,
            bool includeHidden = false);

        /// <summary>
        /// Gets bundle items for a bundle product identifier.
        /// </summary>
        /// <param name="bundleProductId">Bundle product identifier.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden products and bundle items.</param>
        /// <param name="tracked">A value indicating whether to put prefetched entities to EF change tracker.</param>
        /// <returns>List of bundle items.</returns>
        Task<IList<ProductBundleItemData>> GetBundleItemsAsync(
            int bundleProductId,
            bool includeHidden = false,
            bool tracked = false);
    }
}
