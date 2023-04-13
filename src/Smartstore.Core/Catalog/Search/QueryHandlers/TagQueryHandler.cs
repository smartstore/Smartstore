using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Search.QueryHandlers
{
    internal class TagQueryHandler : CatalogSearchQueryHandler
    {
        protected override IQueryable<Product> Apply(IQueryable<Product> query, CatalogSearchQueryContext ctx)
        {
            var tagIds = GetIdList("tagid", ctx);
            if (tagIds.Length > 0)
            {
                ctx.IsGroupingRequired = true;
                query =
                    from p in query
                    from pt in p.ProductTags.Where(pt => tagIds.Contains(pt.Id))
                    select p;
            }

            return query;
        }
    }
}
