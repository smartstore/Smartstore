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
    }
}
