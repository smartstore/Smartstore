using System.Threading.Tasks;

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
        /// <param name="product">Product to copy.</param>
        /// <param name="cloneName">The product name of the copied product.</param>
        /// <param name="isPublished">A value indicating whether to publish the copied product.</param>
        /// <param name="copyAssociatedProducts">A value indicating whether to copy associated products.</param>
        /// <returns>The copied product.</returns>
        Task<Product> CloneProductAsync(
            Product product,
            string cloneName,
            bool isPublished,
            bool copyAssociatedProducts = true);
    }
}
