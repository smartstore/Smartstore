using System.Linq;

namespace Smartstore.Core.Catalog.Attributes
{
    public static partial class ProductVariantAttributeValueQueryExtensions
    {
        private readonly static int[] _attributeListControlTypeIds = new[]
        {
            (int)AttributeControlType.DropdownList,
            (int)AttributeControlType.RadioList,
            (int)AttributeControlType.Checkboxes,
            (int)AttributeControlType.Boxes
        };

        /// <summary>
        /// Applies a filter for product variant attribute and sorts by <see cref="ProductVariantAttributeValue.DisplayOrder"/>.
        /// </summary>
        /// <param name="query">Product variant attribute value query.</param>
        /// <param name="productVariantAttributeId">Filter by <see cref="ProductVariantAttribute.Id"/>.</param>
        /// <returns>Product variant attribute value query.</returns>
        public static IOrderedQueryable<ProductVariantAttributeValue> ApplyProductAttributeFilter(this IQueryable<ProductVariantAttributeValue> query, int productVariantAttributeId)
        {
            Guard.NotNull(query, nameof(query));

            if (productVariantAttributeId == 0)
            {
                query = query.Where(x => x.ProductVariantAttributeId == productVariantAttributeId);
            }

            return query.OrderBy(x => x.DisplayOrder);
        }

        // TODO: (mg) (core) remove this method again. It tempts to wrong usage.
        /// <summary>
        /// Applies a filter for product variant attribute values and sorts by <see cref="ProductVariantAttribute.DisplayOrder"/>, then by <see cref="ProductVariantAttributeValue.DisplayOrder"/>.
        /// </summary>
        /// <param name="query">Product variant attribute value query.</param>
        /// <param name="ids">Filter by multiple <see cref="ProductVariantAttributeValue.Id"/>.</param>
        /// <param name="listTypesOnly">A Value indicating whether to only load list type values.</param>
        /// <returns>Product variant attribute value query.</returns>
        public static IOrderedQueryable<ProductVariantAttributeValue> ApplyValueFilter(
            this IQueryable<ProductVariantAttributeValue> query,
            int[] ids,
            bool listTypesOnly = true)
        {
            Guard.NotNull(query, nameof(query));

            if (ids?.Any() ?? false)
            {
                query = query.Where(x => ids.Contains(x.Id));
            }

            if (listTypesOnly)
            {
                query = query.Where(x => _attributeListControlTypeIds.Contains(x.ProductVariantAttribute.AttributeControlTypeId));
            }

            return query
                .OrderBy(x => x.ProductVariantAttribute.DisplayOrder)
                .ThenBy(x => x.DisplayOrder);
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

            query = query.Where(x => _attributeListControlTypeIds.Contains(x.ProductVariantAttribute.AttributeControlTypeId));

            return query
                .OrderBy(x => x.ProductVariantAttribute.DisplayOrder)
                .ThenBy(x => x.DisplayOrder);
        }
    }
}
