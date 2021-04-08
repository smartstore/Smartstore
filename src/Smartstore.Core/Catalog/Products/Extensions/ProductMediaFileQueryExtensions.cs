using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;

namespace Smartstore.Core.Catalog.Products
{
    public static partial class ProductMediaFileQueryExtensions
    {
        /// <summary>
        /// Applies a filter for products and loads <paramref name="maxFilesPerProduct"/> media files sorted by <see cref="ProductMediaFile.DisplayOrder"/>.
        /// </summary>
        /// <param name="query">Product media file query.</param>
        /// <param name="productIds">Product identifiers to be filtered.</param>
        /// <param name="maxFilesPerProduct">Maximum number of files to be loaded per product. <c>null</c> to load all media files.</param>
        /// <returns>Product media file query.</returns>
        public static IQueryable<ProductMediaFile> ApplyProductFilter(this IQueryable<ProductMediaFile> query, int[] productIds, int? maxFilesPerProduct = null)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(productIds, nameof(productIds));

            query = query.Where(x => productIds.Contains(x.ProductId));

            if (maxFilesPerProduct == null || maxFilesPerProduct == int.MaxValue)
            {
                return query.OrderBy(x => x.DisplayOrder);
            }

            var db = query.GetDbContext<SmartDbContext>();

            // TODO: (mg) (core) Perf. Again, too slow. Do not overshoot the target.
            // It's definitely faster to omit maxFilesPerProduct entirely and load all records -> refactor method and callers.
            query = query
                .Select(x => x.ProductId)
                .Distinct()
                .SelectMany(key => db.ProductMediaFiles
                    .AsNoTracking()
                    .Where(x => x.ProductId == key)
                    .OrderBy(x => x.DisplayOrder)
                    .Take(maxFilesPerProduct.Value));

            return query;
        }
    }
}
