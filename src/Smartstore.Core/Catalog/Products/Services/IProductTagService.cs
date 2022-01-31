using Smartstore.Core.Identity;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Products
{
    /// <summary>
    /// Service interface for product tags.
    /// </summary>
    public partial interface IProductTagService
    {
        /// <summary>
        /// Updates product tags. This method commits to database.
        /// It uses a <see cref="HookImportance.Important"/> hook scope and therefore all changes to entities should have been committed to the database before calling it.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="tagNames">Tag names.</param>
        Task UpdateProductTagsAsync(Product product, IEnumerable<string> tagNames);

        /// <summary>
        /// Gets the number of products associated with a product tag.
        /// </summary>
        /// <param name="productTagId">Product tag identifier.</param>
        /// <param name="customer">Customer entity. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="storeId">Store identifier. 0 to ignore store mappings.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden product tags. Hidden products are always ignored.</param>
        /// <returns>Number of products.</returns>
        Task<int> CountProductsByTagIdAsync(int productTagId, Customer customer = null, int storeId = 0, bool includeHidden = false);
    }
}
