using Smartstore.Core.Search.Facets;
using Smartstore.Core.Search.Indexing;

namespace Smartstore.Core.Search
{
    public interface ISearchProvider
    {
        #region Search

        /// <summary>
        /// Gets the sort order to be applied to a specific sort field.
        /// This is intended for special cases, for example, where a different search index field 
        /// is to be used for sorting than the one created by the query factory.
        /// </summary>
        /// <param name="sort">
        /// Original <see cref="SearchSort"/> created by the query factory.
        /// <c>null</c> if the search engine requests a default sorting.
        /// </param>
        /// <param name="query">Search query.</param>
        /// <returns>New sorting. <c>null</c> to let the caller handle sorting.</returns>
        IList<SearchSort> GetSorting(SearchSort sort, ISearchQuery query);

        /// <summary>
        /// Gets the boost factor. The higher the boost factor, the more relevant the search term will be
        /// and the more in front the search hit will be ranked/scored.
        /// </summary>
        /// <param name="filter">Search filter.</param>
        /// <returns>Boost factor. <see cref="ISearchFilter.Boost"/> by default.</returns>
        float? GetBoost(ISearchFilter filter);

        /// <summary>
        /// Gets the localized field name for a search filter.
        /// See also <seealso cref="IndexField.CreateName"/>.
        /// </summary>
        /// <param name="filter">Search filter.</param>
        /// <param name="languageCulture">Language culture.</param>
        /// <returns>Localized field name. <c>null</c> if no localized field name exists.</returns>
        string GetLocalizedFieldName(ISearchFilter filter, string languageCulture);

        #endregion

        #region Facets

        /// <summary>
        /// Gets a value indicating whether facets exist for an index field.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="query">Search query.</param>
        /// <returns><c>true</c> facets exist otherwise <c>false</c>.</returns>
        bool IsFacetField(string fieldName, ISearchQuery query);

        /// <summary>
        /// Gets the facet map for drilldown navigation.
        /// </summary>
        /// <param name="searchEngine">Search engine.</param>
        /// <param name="storage">Facet metadata storage.</param>
        /// <returns>Map of <see cref="FacetDescriptor.Key"/> to <see cref="FacetGroup"/>.</returns>
        IDictionary<string, FacetGroup> GetFacetMap(ISearchEngine searchEngine, IFacetMetadataStorage storage);

        /// <summary>
        /// Gets the facet map for drilldown navigation.
        /// </summary>
        /// <param name="searchEngine">Search engine.</param>
        /// <param name="storage">Facet metadata storage.</param>
        /// <returns>Map of <see cref="FacetDescriptor.Key"/> to <see cref="FacetGroup"/>.</returns>
        Task<IDictionary<string, FacetGroup>> GetFacetMapAsync(ISearchEngine searchEngine, IFacetMetadataStorage storage);

        #endregion
    }
}
