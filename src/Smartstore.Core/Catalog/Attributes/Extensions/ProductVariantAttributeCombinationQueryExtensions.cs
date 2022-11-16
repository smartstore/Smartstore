namespace Smartstore.Core.Catalog.Attributes
{
    public static partial class ProductVariantAttributeCombinationQueryExtensions
    {
        /// <summary>
        /// Apply standard filter for a product variant combinations query.
        /// </summary>
        /// <param name="query">Product attribute combinations query.</param>
        /// <param name="includeHidden">Applies filter by <c>Product.Published</c> and <see cref="ProductVariantAttributeCombination.IsActive"/>.</param>
        /// <returns>Product attribute combinations query.</returns>
        public static IQueryable<ProductVariantAttributeCombination> ApplyStandardFilter(this IQueryable<ProductVariantAttributeCombination> query, bool includeHidden = false)
        {
            Guard.NotNull(query, nameof(query));

            if (!includeHidden)
            {
                query = query.Where(x => x.Product.Published && x.IsActive);
            }

            return query;
        }

        /// <summary>
        /// Apply a filter to get the lowest attribute combination price and sorts by <see cref="ProductVariantAttributeCombination.Price"/>.
        /// </summary>
        /// <param name="query">Product attribute combinations query.</param>
        /// <param name="productId">Product identifier. Must not be zero.</param>
        /// <returns>Product attribute combinations query.</returns>
        public static IOrderedQueryable<ProductVariantAttributeCombination> ApplyLowestPriceFilter(this IQueryable<ProductVariantAttributeCombination> query, int productId)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotZero(productId, nameof(productId));

            query = query.Where(x => x.ProductId == productId && x.Price != null && x.IsActive);

            return query.OrderBy(x => x.Price);
        }

        /// <summary>
        /// Applies a filter for SKU.
        /// </summary>
        /// <param name="query">Product attribute combinations query.</param>
        /// <param name="sku">Stock keeping unit (SKU).</param>
        /// <returns>Product attribute combinations query.</returns>
        public static IQueryable<ProductVariantAttributeCombination> ApplySkuFilter(this IQueryable<ProductVariantAttributeCombination> query, string sku)
        {
            Guard.NotNull(query, nameof(query));

            sku = sku.TrimSafe();

            query = query.Where(x => x.Sku == sku && !x.Product.Deleted);

            return query;
        }

        /// <summary>
        /// Applies a filter for GTIN.
        /// </summary>
        /// <param name="query">Product attribute combinations query.</param>
        /// <param name="gtin">Global Trade Item Number (GTIN).</param>
        /// <returns>Product attribute combinations query.</returns>
        public static IQueryable<ProductVariantAttributeCombination> ApplyGtinFilter(this IQueryable<ProductVariantAttributeCombination> query, string gtin)
        {
            Guard.NotNull(query, nameof(query));

            gtin = gtin.TrimSafe();

            query = query.Where(x => x.Gtin == gtin && !x.Product.Deleted);

            return query;
        }

        /// <summary>
        /// Applies a filter for MPN.
        /// </summary>
        /// <param name="query">Product attribute combinations query.</param>
        /// <param name="manufacturerPartNumber">Manufacturer Part Number (MPN).</param>
        /// <returns>Product attribute combinations query.</returns>
        public static IQueryable<ProductVariantAttributeCombination> ApplyMpnFilter(this IQueryable<ProductVariantAttributeCombination> query, string manufacturerPartNumber)
        {
            Guard.NotNull(query, nameof(query));

            manufacturerPartNumber = manufacturerPartNumber.TrimSafe();

            query = query.Where(x => x.ManufacturerPartNumber == manufacturerPartNumber && !x.Product.Deleted);

            return query;
        }
    }
}
