using Smartstore.Core.Search;

namespace Smartstore.Core.Catalog.Search
{
    public class CatalogSearchQueryContext : SearchQueryContext<CatalogSearchQuery>
    {
        public CatalogSearchQueryContext(CatalogSearchQuery query, ICommonServices services, SearchSettings searchSettings)
            : base(query)
        {
            Services = Guard.NotNull(services);
            SearchSettings = Guard.NotNull(searchSettings);
        }

        public ICommonServices Services { get; }
        public SearchSettings SearchSettings { get; }

        public int? CategoryId { get; set; }
        public int? ManufacturerId { get; set; }
    }
}
