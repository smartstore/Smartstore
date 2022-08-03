using Smartstore.Core.Data;

namespace Smartstore.Core.Catalog.Products
{
    public static partial class CrossSellProductQueryExtensions
    {
        /// <summary>
        /// Applies a filter for <see cref="CrossSellProduct.ProductId1"/>.
        /// </summary>
        /// <param name="query">Cross sell product query.</param>
        /// <param name="productId1">Product identifier.</param>
        /// <param name="includeHidden">Applies a filter for <see cref="Product.Published"/>.</param>
        /// <returns>Cross sell product query.</returns>
        public static IQueryable<CrossSellProduct> ApplyProductId1Filter(this IQueryable<CrossSellProduct> query, int productId1, bool includeHidden = false)
        {
            Guard.NotNull(query, nameof(query));

            if (productId1 == 0)
            {
                return query;
            }

            var db = query.GetDbContext<SmartDbContext>();

            // Always join products to ignore deleted products.
            query =
                from csp in query
                join p in db.Products.AsNoTracking() on csp.ProductId2 equals p.Id
                where csp.ProductId1 == productId1 && (includeHidden || p.Published)
                orderby csp.Id
                select csp;

            return query;
        }
    }
}
