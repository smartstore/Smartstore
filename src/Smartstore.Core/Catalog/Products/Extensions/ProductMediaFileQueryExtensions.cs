using System;
using System.Linq;

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

            var take = maxFilesPerProduct ?? int.MaxValue;

            query = query
                .Where(pf => productIds.Contains(pf.ProductId))
                .GroupBy(pf => pf.ProductId, x => x)
                .SelectMany(pf => pf.OrderBy(x => x.DisplayOrder).Take(take));

            return query;
        }
    }
}
