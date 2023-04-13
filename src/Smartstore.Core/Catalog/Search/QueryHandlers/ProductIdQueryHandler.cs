using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Search.QueryHandlers
{
    internal class ProductIdQueryHandler : CatalogSearchQueryHandler
    {
        protected override IQueryable<Product> Apply(IQueryable<Product> query, CatalogSearchQueryContext ctx)
        {
            var productIds = GetIdList("id", ctx);
            if (productIds.Length > 0)
            {
                query = query.Where(x => productIds.Contains(x.Id));
            }

            return query;
        }
    }
}
