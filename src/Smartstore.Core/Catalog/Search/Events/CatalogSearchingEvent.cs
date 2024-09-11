using Smartstore.Core.Search;

namespace Smartstore.Core.Catalog.Search
{
    /// <summary>
    /// An event that is fired before searching the catalog for products.
    /// </summary>
    public class CatalogSearchingEvent
    {
        public CatalogSearchingEvent(CatalogSearchQuery query, bool direct)
        {
            Guard.NotNull(query);

            Query = query;
            Direct = direct;
        }

        /// <summary>
        /// A value indicating whether the database is directly searched.
        /// If <c>false</c> then <see cref="ISearchEngine"/> is used.
        /// </summary>
        public bool Direct { get; init; }

        public CatalogSearchQuery Query { get; init; }
    }

    /// <summary>
    /// An event that is fired after the catalog has been searched for products.
    /// </summary>
    public class CatalogSearchedEvent
    {
        public CatalogSearchedEvent(CatalogSearchQuery query, CatalogSearchResult result)
        {
            Guard.NotNull(query);
            Guard.NotNull(result);

            Query = query;
            Result = result;
        }

        public CatalogSearchQuery Query { get; init; }
        public CatalogSearchResult Result { get; init; }
    }
}
