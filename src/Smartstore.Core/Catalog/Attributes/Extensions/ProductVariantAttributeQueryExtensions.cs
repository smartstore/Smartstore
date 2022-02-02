namespace Smartstore.Core.Catalog.Attributes
{
    public static partial class ProductVariantAttributeQueryExtensions
    {
        /// <summary>
        /// Applies a filter for products and sorts by <see cref="ProductVariantAttribute.DisplayOrder"/>.
        /// </summary>
        /// <param name="query">Product variant attribute query.</param>
        /// <param name="productIds">Product identifiers to be filtered.</param>
        /// <returns>Product variant attribute query.</returns>
        public static IOrderedQueryable<ProductVariantAttribute> ApplyProductFilter(this IQueryable<ProductVariantAttribute> query, int[] productIds)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(productIds, nameof(productIds));

            query = query.Where(x => productIds.Contains(x.ProductId));

            return query.OrderBy(x => x.DisplayOrder);
        }

        /// <summary>
        /// Applies a filter for list control types.
        /// </summary>
        /// <param name="query"><see cref="ProductVariantAttribute"/> query.</param>
        /// <returns><see cref="ProductVariantAttribute"/> query.</returns>
        public static IQueryable<ProductVariantAttribute> ApplyListTypeFilter(this IQueryable<ProductVariantAttribute> query)
        {
            Guard.NotNull(query, nameof(query));

            return query.Where(x => ProductAttributeMaterializer.AttributeListControlTypeIds.Contains(x.AttributeControlTypeId));
        }
    }
}
