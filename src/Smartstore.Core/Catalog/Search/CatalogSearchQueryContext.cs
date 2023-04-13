using Smartstore.Core.Search;

namespace Smartstore.Core.Catalog.Search
{
    public class CatalogSearchQueryContext : SearchQueryContext<CatalogSearchQuery>
    {
        public CatalogSearchQueryContext(CatalogSearchQuery query, ICommonServices services)
            : base(query)
        {
            Services = Guard.NotNull(services);
        }

        public ICommonServices Services { get; }
        public int? CategoryId { get; set; }
        public int? ManufacturerId { get; set; }
    }
}
