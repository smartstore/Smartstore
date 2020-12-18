using System.Linq;

namespace Smartstore.Core.Catalog.Products
{
    public static partial class ProductQueryExtensions
    {
        /// <summary>
        /// Apply standard filter for a product query.
        /// Filters out <see cref="Product.IsSystemProduct"/>.
        /// </summary>
        /// <param name="query">Product query.</param>
        /// <param name="includeHidden">Applies filter by <see cref="Product.Published"/>.</param>
        /// <returns>Product query.</returns>
        public static IQueryable<Product> ApplyStandardFilter(this IQueryable<Product> query, bool includeHidden = false)
        {
            Guard.NotNull(query, nameof(query));

            if (!includeHidden)
            {
                query = query.Where(x => x.Published);
            }

            query = query.Where(x => !x.IsSystemProduct);

            return query;
        }
    }
}
