using Smartstore.Core.Data;

namespace Smartstore.Core.Catalog.Products
{
    public static partial class ProductBundleItemQueryExtensions
    {
        /// <summary>
        /// Applies a filter for bundled products and sorts by <see cref="ProductBundleItem.DisplayOrder"/>.
        /// </summary>
        /// <param name="query">Product bundle item query.</param>
        /// <param name="bundledProductIds">Identifiers of bundled products to be filtered.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden products and bundle items.</param>
        /// <returns>Product bundle item query.</returns>
        public static IQueryable<ProductBundleItem> ApplyBundledProductsFilter(this IQueryable<ProductBundleItem> query, int[] bundledProductIds, bool includeHidden = false)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(bundledProductIds, nameof(bundledProductIds));

            var db = query.GetDbContext<SmartDbContext>();

            query =
                from pbi in query
                join p in db.Products on pbi.ProductId equals p.Id
                where bundledProductIds.Contains(pbi.BundleProductId) && (includeHidden || (pbi.Published && p.Published))
                orderby pbi.DisplayOrder
                select pbi;

            return query;
        }
    }
}
