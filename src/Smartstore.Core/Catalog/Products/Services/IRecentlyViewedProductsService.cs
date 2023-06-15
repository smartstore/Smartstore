namespace Smartstore.Core.Catalog.Products
{
    /// <summary>
    /// Recently viewed products service interface.
    /// </summary>
    public partial interface IRecentlyViewedProductsService
    {
        /// <summary>
        /// Gets a list of recently viewed products.
        /// </summary>
        /// <param name="count">Number of products to return.</param>
        /// <param name="excludedProductIds">Array of product identifiers to be excluded.</param>
        /// <returns>List of recently viewed products.</returns>
        Task<IList<Product>> GetRecentlyViewedProductsAsync(int count, params int[] excludedProductIds);

        /// <summary>
        /// Adds a product identifier to the recently viewed products list.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        void AddProductToRecentlyViewedList(int productId);
    }
}
