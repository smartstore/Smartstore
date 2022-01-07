using Microsoft.AspNetCore.Http;

namespace Smartstore.Core.Catalog.Search.Modelling
{
    /// <summary>
    /// Query factory for catalog search.
    /// </summary>
    public partial interface ICatalogSearchQueryFactory
    {
        /// <summary>
        /// The last created query instance. The model binder uses this property to avoid repeated binding.
        /// </summary>
        CatalogSearchQuery Current { get; }

        /// <summary>
        /// Creates a <see cref="CatalogSearchQuery"/> instance from the current <see cref="HttpContext"/> 
        /// by looking up corresponding keys in posted form and/or query string.
        /// </summary>
        /// <returns>The query object.</returns>
        Task<CatalogSearchQuery> CreateFromQueryAsync();
    }
}
