using Smartstore.Core.Search.Facets;

namespace Smartstore.Core.Search
{
    public interface ISearchProvider
    {
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

        #region Facets

        /// <summary>
        /// Gets a value indicating whether facets exist for an index field.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <param name="query">Search query.</param>
        /// <returns><c>true</c> facets exist otherwise <c>false</c>.</returns>
        bool IsFacetField(string fieldName, ISearchQuery query);

        /// <summary>
        /// Gets facet metadata for a <see cref="FacetDescriptor"/>. 
        /// </summary>
        /// <remarks>
        /// Metadata defines which data should be faceted. It can be generated on-the-fly (e.g. for a 1-to-5 stars product rating)
        /// or loaded via <see cref="IFacetMetadataStorage"/> from a medium (e.g. a file-based search index).
        /// </remarks>
        /// <param name="descriptor"><see cref="FacetDescriptor"/> to get metadata for.</param>
        /// <param name="searchEngine">Search engine.</param>
        /// <param name="storage">Storage to get the metadata from, e.g. to load from a search index.</param>
        /// <returns>Dictionary of <see cref="FacetValue.Value"/> to <see cref="FacetMetadata"/>.</returns>
        Task<IDictionary<object, FacetMetadata>> GetFacetMetadataAsync(FacetDescriptor descriptor, ISearchEngine searchEngine, IFacetMetadataStorage storage);

        #endregion
    }
}
