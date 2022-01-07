using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Search
{
    /// <summary>
    /// Catalog search interface.
    /// </summary>
    public partial interface ICatalogSearchService
    {
        /// <summary>
        /// Builds a product query using LINQ search.
        /// </summary>
        /// <param name="searchQuery">Search term, filters and other parameters used for searching.</param>
        /// <param name="baseQuery">Optional query used to build the product query.</param>
        /// <returns>Product queryable.</returns>
        IQueryable<Product> PrepareQuery(CatalogSearchQuery searchQuery, IQueryable<Product> baseQuery = null);

        /// <summary>
        /// Searches for products.
        /// </summary>
        /// <param name="searchQuery">Search term, filters and other parameters used for searching.</param>
        /// <param name="direct">Bypasses the index provider (if available) and directly searches in the database.</param>
        /// <returns>Catalog search result.</returns>
        Task<CatalogSearchResult> SearchAsync(CatalogSearchQuery searchQuery, bool direct = false);
    }
}
