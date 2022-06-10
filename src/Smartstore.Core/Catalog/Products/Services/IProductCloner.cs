namespace Smartstore.Core.Catalog.Products
{
    /// <summary>
    /// Product cloner interface.
    /// </summary>
    public partial interface IProductCloner
    {
        /// <summary>
        /// Copies a product.
        /// </summary>
        /// <param name="product">Product to copy. Important: Pass a tracked entity because the navigation properties must be available in order to be copied.</param>
        /// <param name="cloneName">The product name of the copied product.</param>
        /// <param name="isPublished">A value indicating whether to publish the copied product.</param>
        /// <param name="copyAssociatedProducts">A value indicating whether to copy associated products.</param>
        /// <returns>The copied product.</returns>
        /// <remarks>
        /// The caller is responsible for fast retrieval (eager loading) of product navigation properties.
        /// </remarks>
        Task<Product> CloneProductAsync(
            Product product,
            string cloneName,
            bool isPublished,
            bool copyAssociatedProducts = true);
    }
}
