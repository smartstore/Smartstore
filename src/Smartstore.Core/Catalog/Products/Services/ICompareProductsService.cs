using System.Collections.Generic;
using System.Threading.Tasks;

namespace Smartstore.Core.Catalog.Products
{
    /// <summary>
    /// Compare products service.
    /// </summary>
    public partial interface ICompareProductsService
    {
        /// <summary>
        /// Gets the number of compared products.
        /// </summary>
        /// <returns>Number of compared products.</returns>
        int GetComparedProductsCount();

        /// <summary>
        /// Gets the list of compared products.
        /// </summary>
        /// <returns>List of compared products.</returns>
        Task<IList<Product>> GetComparedProductsAsync();

        /// <summary>
        /// Clears the list of compared products.
        /// </summary>
        void ClearComparedProducts();

        /// <summary>
        /// Removes a product from the list of compared products.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        void RemoveProductFromComparedProducts(int productId);

        /// <summary>
        /// Adds a product to the list of compared products.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        void AddProductToComparedProducts(int productId);
    }
}
