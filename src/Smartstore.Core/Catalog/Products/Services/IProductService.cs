using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Discounts;

namespace Smartstore.Core.Catalog.Products
{
    /// <summary>
    /// Product service interface.
    /// </summary>
    public partial interface IProductService
    {
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
        /// <param name="includeHidden">A value indicating whether to include hidden product tags.</param>
        /// <returns>Map of product tags.</returns>
        Task<Multimap<int, ProductTag>> GetProductTagsByProductIdsAsync(int[] productIds, bool includeHidden = false);

        /// <summary>
        /// Get applied discounts by product identifiers.
        /// </summary>
        /// <param name="productIds">Product identifiers.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden product tags.</param>
        /// <returns>Map of discounts.</returns>
        Task<Multimap<int, Discount>> GetAppliedDiscountsByProductIdsAsync(int[] productIds, bool includeHidden = false);
    }
}
