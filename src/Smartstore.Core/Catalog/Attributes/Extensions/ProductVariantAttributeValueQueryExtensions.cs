using System.Linq;
using Autofac.Features.ResolveAnything;
using Smartstore.Core.Data;

namespace Smartstore.Core.Catalog.Attributes
{
    public static partial class ProductVariantAttributeValueQueryExtensions
    {
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
                var listTypes = new[]
                {
                    (int)AttributeControlType.DropdownList,
                    (int)AttributeControlType.RadioList,
                    (int)AttributeControlType.Checkboxes,
                    (int)AttributeControlType.Boxes
                };

                query = query.Where(x => listTypes.Contains(x.ProductVariantAttribute.AttributeControlTypeId));
            }

            return query
                .OrderBy(x => x.ProductVariantAttribute.DisplayOrder)
                .ThenBy(x => x.DisplayOrder);
        }
    }
}
