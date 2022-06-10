namespace Smartstore.Core.Catalog.Products
{
    public static partial class ProductMediaFileQueryExtensions
    {
        /// <summary>
        /// Applies a filter for a product and sorts by <see cref="ProductMediaFile.DisplayOrder"/>.
        /// </summary>
        /// <param name="query">Product media file query.</param>
        /// <param name="productId">Product identifier to be filtered.</param>
        /// <returns>Product media file query.</returns>
        public static IQueryable<ProductMediaFile> ApplyProductFilter(this IQueryable<ProductMediaFile> query, int productId)
        {
            Guard.NotNull(query, nameof(query));

            return query
                .Where(x => x.ProductId == productId)
                .OrderBy(x => x.DisplayOrder);
        }
    }
}
