namespace Smartstore.Core.Catalog.Brands
{
    public static partial class ProductManufacturerQueryExtensions
    {
        /// <summary>
        /// Applies a manufacturer filter and sorts by <see cref="ProductManufacturer.DisplayOrder"/>, then by <c>ProductManufacturer.Id</c>
        /// Includes <see cref="ProductManufacturer.Manufacturer"/> and <see cref="ProductManufacturer.Product"/>.
        /// </summary>
        /// <param name="query">Product manufacturer query.</param>
        /// <param name="manufacturerId">Applies filter by <see cref="ProductManufacturer.ManufacturerId"/>.</param>
        /// <returns>Product manufacturer query.</returns>
        public static IOrderedQueryable<ProductManufacturer> ApplyManufacturerFilter(this IQueryable<ProductManufacturer> query, int manufacturerId)
        {
            Guard.NotNull(query, nameof(query));

            query = query
                .Include(x => x.Manufacturer)
                .Include(x => x.Product)
                .Where(x => x.ManufacturerId == manufacturerId && x.Manufacturer != null && x.Product != null);

            return query
                .OrderBy(pc => pc.DisplayOrder)
                .ThenBy(pc => pc.Id);
        }
    }
}
