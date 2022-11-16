namespace Smartstore.Core.Catalog.Attributes
{
    public static partial class ProductVariantAttributeValueQueryExtensions
    {
        /// <summary>
        /// Applies a filter for product variant attribute and sorts by <see cref="ProductVariantAttributeValue.DisplayOrder"/>.
        /// </summary>
        /// <param name="query">Product variant attribute value query.</param>
        /// <param name="productVariantAttributeId">Filter by <c>ProductVariantAttribute.Id</c>.</param>
        /// <returns>Product variant attribute value query.</returns>
        public static IOrderedQueryable<ProductVariantAttributeValue> ApplyProductAttributeFilter(this IQueryable<ProductVariantAttributeValue> query, int productVariantAttributeId)
        {
            Guard.NotNull(query, nameof(query));

            if (productVariantAttributeId != 0)
            {
                query = query.Where(x => x.ProductVariantAttributeId == productVariantAttributeId);
            }

            return query.OrderBy(x => x.DisplayOrder);
        }

        /// <summary>
        /// Applies a filter for list control types and sorts by <see cref="ProductVariantAttribute.DisplayOrder"/>, 
        /// then by <see cref="ProductVariantAttributeValue.DisplayOrder"/>.
        /// Only product attributes of list types (e.g. dropdown list) can have assigned <see cref="ProductVariantAttributeValue"/> entities.
        /// </summary>
        /// <param name="query">Product variant attribute value query.</param>
        /// <returns>Product variant attribute value query.</returns>
        public static IOrderedQueryable<ProductVariantAttributeValue> ApplyListTypeFilter(this IQueryable<ProductVariantAttributeValue> query)
        {
            Guard.NotNull(query, nameof(query));

            query = query.Where(x => ProductAttributeMaterializer.AttributeListControlTypeIds.Contains(x.ProductVariantAttribute.AttributeControlTypeId));

            return query
                .OrderBy(x => x.ProductVariantAttribute.DisplayOrder)
                .ThenBy(x => x.DisplayOrder);
        }
    }
}
