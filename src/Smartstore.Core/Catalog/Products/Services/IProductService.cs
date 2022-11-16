using Smartstore.Collections;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

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
        /// Gets product tags by product identifiers.
        /// </summary>
        /// <param name="productIds">Product identifiers.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden products and product tags.</param>
        /// <returns>Map of product tags.</returns>
        Task<Multimap<int, ProductTag>> GetProductTagsByProductIdsAsync(int[] productIds, bool includeHidden = false);

        /// <summary>
        /// Gets cross-sell products by shopping cart.
        /// </summary>
        /// <param name="productIds">Product identifiers.</param>
        /// <param name="numberOfProducts">Number of products to return.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden products.</param>
        /// <returns>List of products.</returns>
        Task<IList<Product>> GetCrossSellProductsByProductIdsAsync(int[] productIds, int numberOfProducts, bool includeHidden = false);

        /// <summary>
        /// Applies the product review totals to a product entity. The caller is responsible for database commit.
        /// </summary>
        /// <param name="product">Product entity.</param>
        void ApplyProductReviewTotals(Product product);

        /// <summary>
        /// Adjusts product inventory. The caller is responsible for database commit.
        /// </summary>
        /// <param name="orderItem">Order item.</param>
        /// <param name="decrease">A value indicating whether to increase or descrease product stock quantity.</param>
        /// <param name="quantity">The quantity to adjust.</param>
        /// <returns>Adjust inventory result.</returns>
        Task<AdjustInventoryResult> AdjustInventoryAsync(OrderItem orderItem, bool decrease, int quantity);

        /// <summary>
        /// Adjusts product inventory. The caller is responsible for database commit.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <param name="selection">Selected attributes.</param>
        /// <param name="decrease">A value indicating whether to increase or descrease product stock quantity.</param>
        /// <param name="quantity">The quantity to adjust.</param>
        /// <returns>Adjust inventory result.</returns>
        Task<AdjustInventoryResult> AdjustInventoryAsync(Product product, ProductVariantAttributeSelection selection, bool decrease, int quantity);

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
        /// Creates a product batch context for fast retrieval (eager loading) of product navigation properties.
        /// </summary>
        /// <param name="products">Products. <c>null</c> to lazy load data if required.</param>
        /// <param name="store">Store. If <c>null</c>, store will be obtained via <see cref="IStoreContext.CurrentStore"/>.</param>
        /// <param name="customer">Customer. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden records.</param>
        /// <param name="loadMainMediaOnly">
        /// A value indicating whether to load the main media per product only.
        /// The main media file is determined by <see cref="Product.MainPictureId"/>.
        /// </param>
        /// <returns>Product batch context.</returns>
        ProductBatchContext CreateProductBatchContext(
            IEnumerable<Product> products = null,
            Store store = null,
            Customer customer = null,
            bool includeHidden = true,
            bool loadMainMediaOnly = false);
    }
}
