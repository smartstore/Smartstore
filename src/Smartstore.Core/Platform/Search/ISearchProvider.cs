using Smartstore.Core.Search.Facets;

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
        SearchSort GetSorting(SearchSort sort, ISearchQuery query);

        /// <summary>
        /// Gets fields to be searched.
        /// Allows additional fields (e.g. of localized data) to be searched as well.
        /// </summary>
        /// <param name="query">Search query.</param>
        /// <param name="languageCulture">Language culture.</param>
        /// <returns>Search fields.</returns>
        IList<SearchField> GetFields(ISearchQuery query, string languageCulture);

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
