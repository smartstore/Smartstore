using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Search.QueryHandlers
{
    internal class CategoryIdQueryHandler : CatalogSearchQueryHandler
    {
        protected override IQueryable<Product> Apply(IQueryable<Product> query, CatalogSearchQueryContext ctx)
        {
            var categoryIds = GetIdList("categoryid", ctx);
            if (categoryIds.Length > 0)
            {
                ctx.CategoryId ??= categoryIds.First();
                if (categoryIds.Length == 1 && ctx.CategoryId == 0)
                {
                    // Has no category.
                    query = query.Where(x => x.ProductCategories.Count == 0);
                }
                else
                {
                    ctx.IsGroupingRequired = true;
                    query = ApplyCategoriesFilter(query, categoryIds, null);
                }
            }

            var featuredCategoryIds = GetIdList("featuredcategoryid", ctx);
            if (featuredCategoryIds.Length > 0)
            {
                ctx.IsGroupingRequired = true;
                ctx.CategoryId ??= featuredCategoryIds.First();
                query = ApplyCategoriesFilter(query, featuredCategoryIds, true);
            }

            var notFeaturedCategoryIds = GetIdList("notfeaturedcategoryid", ctx);
            if (notFeaturedCategoryIds.Length > 0)
            {
                ctx.IsGroupingRequired = true;
                ctx.CategoryId ??= notFeaturedCategoryIds.First();
                query = ApplyCategoriesFilter(query, notFeaturedCategoryIds, false);
            }

            return query;
        }

        private static IQueryable<Product> ApplyCategoriesFilter(IQueryable<Product> query, int[] ids, bool? featuredOnly)
        {
            return
                from p in query
                from pc in p.ProductCategories.Where(pc => ids.Contains(pc.CategoryId))
                where !featuredOnly.HasValue || featuredOnly.Value == pc.IsFeaturedProduct
                select p;
        }
    }
}
