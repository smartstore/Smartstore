using System;
using System.Linq;
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

            if (maxFilesPerProduct == null || maxFilesPerProduct > 999999)
            {
                // Don't group on client
                return query.OrderBy(x => x.ProductId).ThenBy(x => x.DisplayOrder);
            }

            var take = maxFilesPerProduct ?? int.MaxValue;

            // TODO: (mg) (core) This query is slow. Find a better way to group-sort product pictures.
            return query
                .AsEnumerable() // Will throw otherwise
                .GroupBy(x => x.ProductId)
                .SelectMany(pf => pf.OrderBy(x => x.DisplayOrder).Take(take))
                .AsQueryable();
        }
    }
}
