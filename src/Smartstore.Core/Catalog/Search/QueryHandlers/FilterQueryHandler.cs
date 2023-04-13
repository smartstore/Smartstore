using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Search.QueryHandlers
{
    internal class FilterQueryHandler : CatalogSearchQueryHandler
    {
        protected override IQueryable<Product> Apply(IQueryable<Product> query, CatalogSearchQueryContext ctx)
        {
            // TODO: Create concrete handler for every filter

            return query;
        }
    }
}
