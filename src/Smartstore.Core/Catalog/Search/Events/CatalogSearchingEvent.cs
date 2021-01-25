namespace Smartstore.Core.Catalog.Search
{
    public class CatalogSearchingEvent
    {
        public CatalogSearchingEvent(CatalogSearchQuery query, bool direct)
        {
            Guard.NotNull(query, nameof(query));

            Query = query;
            Direct = direct;
        }

        public bool Direct { get; init; }
        public CatalogSearchQuery Query { get; init; }
    }

    public class CatalogSearchedEvent
    {
        public CatalogSearchedEvent(CatalogSearchQuery query, CatalogSearchResult result)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(result, nameof(result));

            Query = query;
            Result = result;
        }

        public CatalogSearchQuery Query { get; init; }
        public CatalogSearchResult Result { get; init; }
    }
}
