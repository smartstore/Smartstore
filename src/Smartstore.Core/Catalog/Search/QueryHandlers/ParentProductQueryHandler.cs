using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Search.QueryHandlers
{
    internal class ParentProductQueryHandler : CatalogSearchQueryHandler
    {
        protected override IQueryable<Product> Apply(IQueryable<Product> query, CatalogSearchQueryContext ctx)
        {
            var parentProductIds = GetIdList("parentid", ctx);
            if (parentProductIds.Length > 0)
            {
                query = query.Where(x => parentProductIds.Contains(x.ParentGroupedProductId));
            }

            return query;
        }
    }
}
