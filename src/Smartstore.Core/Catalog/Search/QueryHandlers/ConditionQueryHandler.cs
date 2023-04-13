using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Search.QueryHandlers
{
    internal class ConditionQueryHandler : CatalogSearchQueryHandler
    {
        protected override IQueryable<Product> Apply(IQueryable<Product> query, CatalogSearchQueryContext ctx)
        {
            var conditions = GetIdList("condition", ctx);
            if (conditions.Length > 0)
            {
                query = query.Where(x => conditions.Contains((int)x.Condition));
            }

            return query;
        }
    }
}
