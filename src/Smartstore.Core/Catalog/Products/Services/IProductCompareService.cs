namespace Smartstore.Core.Catalog.Products
{
    /// <summary>
    /// Compare products service.
    /// </summary>
    public partial interface IProductCompareService
    {
        /// <summary>
        /// Gets the number of compared products.
        /// </summary>
        /// <returns>Number of compared products.</returns>
        Task<int> CountComparedProductsAsync();

        /// <summary>
        /// Gets the list of compared products.
        /// </summary>
        /// <returns>List of compared products.</returns>
        Task<IList<Product>> GetCompareListAsync();

        /// <summary>
        /// Clears the list of compared products.
        /// </summary>
        void ClearCompareList();

        /// <summary>
        /// Removes a product from the list of compared products.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        void RemoveFromList(int productId);

        /// <summary>
        /// Adds a product to the list of compared products.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        void AddToList(int productId);
    }
}
