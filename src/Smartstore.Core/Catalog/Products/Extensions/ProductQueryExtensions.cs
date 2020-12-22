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

        /// <summary>
        /// Applies a filter for system names.
        /// </summary>
        /// <param name="query">Product query.</param>
        /// <param name="systemName">Product system name.</param>
        /// <returns>Product query.</returns>
        public static IQueryable<Product> ApplySystemNameFilter(this IQueryable<Product> query, string systemName)
        {
            Guard.NotNull(query, nameof(query));

            return query.Where(x => x.SystemName == systemName && x.IsSystemProduct);
        }

        /// <summary>
        /// Applies a filter for SKU and sorts by <see cref="Product.DisplayOrder"/>, then by <see cref="Product.Id"/>.
        /// </summary>
        /// <param name="query">Product query.</param>
        /// <param name="sku">Stock keeping unit (SKU).</param>
        /// <returns>Ordered product query.</returns>
        public static IOrderedQueryable<Product> ApplySkuFilter(this IQueryable<Product> query, string sku)
        {
            Guard.NotNull(query, nameof(query));

            sku = sku.TrimSafe();

            query = query.Where(x => x.Sku == sku);

            return query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id);
        }

        /// <summary>
        /// Applies a filter for GTIN and sorts by <see cref="Product.Id"/>.
        /// </summary>
        /// <param name="query">Product query.</param>
        /// <param name="gtin">Global Trade Item Number (GTIN).</param>
        /// <returns>Ordered product query.</returns>
        public static IOrderedQueryable<Product> ApplyGtinFilter(this IQueryable<Product> query, string gtin)
        {
            Guard.NotNull(query, nameof(query));

            gtin = gtin.TrimSafe();

            query = query.Where(x => x.Gtin == gtin);

            return query.OrderBy(x => x.Id);
        }

        /// <summary>
        /// Applies a filter for MPN and sorts by <see cref="Product.Id"/>.
        /// </summary>
        /// <param name="query">Product query.</param>
        /// <param name="manufacturerPartNumber">Manufacturer Part Number (MPN).</param>
        /// <returns>Ordered product query.</returns>
        public static IOrderedQueryable<Product> ApplyMpnFilter(this IQueryable<Product> query, string manufacturerPartNumber)
        {
            Guard.NotNull(query, nameof(query));

            manufacturerPartNumber = manufacturerPartNumber.TrimSafe();

            query = query.Where(x => x.ManufacturerPartNumber == manufacturerPartNumber);

            return query.OrderBy(x => x.Id);
        }
    }
}
