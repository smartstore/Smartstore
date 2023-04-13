using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Search.QueryHandlers
{
    internal class ManufacturerIdQueryHandler : CatalogSearchQueryHandler
    {
        protected override IQueryable<Product> Apply(IQueryable<Product> query, CatalogSearchQueryContext ctx)
        {
            var manufacturerIds = GetIdList("manufacturerid", ctx);
            if (manufacturerIds.Length > 0)
            {
                ctx.ManufacturerId ??= manufacturerIds.First();
                if (manufacturerIds.Length == 1 && ctx.ManufacturerId == 0)
                {
                    // Has no manufacturer.
                    query = query.Where(x => x.ProductManufacturers.Count == 0);
                }
                else
                {
                    ctx.IsGroupingRequired = true;
                    query = ApplyManufacturersFilter(query, manufacturerIds, null);
                }
            }

            var featuredManuIds = GetIdList("featuredmanufacturerid", ctx);
            if (featuredManuIds.Length > 0)
            {
                ctx.IsGroupingRequired = true;
                ctx.ManufacturerId ??= featuredManuIds.First();
                query = ApplyManufacturersFilter(query, featuredManuIds, true);
            }

            var notFeaturedManuIds = GetIdList("notfeaturedmanufacturerid", ctx);
            if (notFeaturedManuIds.Length > 0)
            {
                ctx.IsGroupingRequired = true;
                ctx.ManufacturerId ??= notFeaturedManuIds.First();
                query = ApplyManufacturersFilter(query, notFeaturedManuIds, false);
            }

            return query;
        }

        private static IQueryable<Product> ApplyManufacturersFilter(IQueryable<Product> query, int[] ids, bool? featuredOnly)
        {
            return
                from p in query
                from pm in p.ProductManufacturers.Where(pm => ids.Contains(pm.ManufacturerId))
                where !featuredOnly.HasValue || featuredOnly.Value == pm.IsFeaturedProduct
                select p;
        }
    }
}
