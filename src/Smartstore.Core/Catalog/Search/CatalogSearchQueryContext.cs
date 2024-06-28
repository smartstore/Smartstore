using Smartstore.Core.Search;

namespace Smartstore.Core.Catalog.Search
{
    public class CatalogSearchQueryContext(CatalogSearchQuery query, 
        ICommonServices services) : SearchQueryContext<CatalogSearchQuery>(query)
    {
        public ICommonServices Services { get; } = Guard.NotNull(services);
        public int? CategoryId { get; set; }
        public int? ManufacturerId { get; set; }
    }
}
