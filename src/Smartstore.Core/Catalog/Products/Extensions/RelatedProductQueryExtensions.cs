using Smartstore.Core.Data;

namespace Smartstore.Core.Catalog.Products
{
    public static partial class RelatedProductQueryExtensions
    {
        /// <summary>
        /// Applies a filter for <see cref="RelatedProduct.ProductId1"/> and sorts by <see cref="RelatedProduct.DisplayOrder"/>.
        /// </summary>
        /// <param name="query">Related product query.</param>
        /// <param name="productId1">Product identifier.</param>
        /// <param name="includeHidden">Applies a filter for <see cref="Product.Published"/>.</param>
        /// <returns>Related product query.</returns>
        public static IQueryable<RelatedProduct> ApplyProductId1Filter(this IQueryable<RelatedProduct> query, int productId1, bool includeHidden = false)
        {
            Guard.NotNull(query, nameof(query));

            if (productId1 == 0)
            {
                return query;
            }

            var db = query.GetDbContext<SmartDbContext>();

            // Always join products to ignore deleted products.
            query =
                from rp in query
                join p in db.Products.AsNoTracking() on rp.ProductId2 equals p.Id
                where rp.ProductId1 == productId1 && (includeHidden || p.Published)
                orderby rp.DisplayOrder
                select rp;

            return query;
        }
    }
}
