using System.Linq;
using Smartstore.Core.Data;

namespace Smartstore.Core.Catalog.Attributes
{
    public static partial class ProductVariantAttributeValueQueryExtensions
    {
        /// <summary>
        /// Applies a filter product variant attribute values and sorts by <see cref="ProductVariantAttribute.DisplayOrder"/>, then by <see cref="ProductVariantAttributeValue.DisplayOrder"/>.
        /// </summary>
        /// <param name="query">Product variant attribute value query.</param>
        /// <param name="valueIds">Identifiers of <see cref="ProductVariantAttributeValue"/> to be filtered.</param>
        /// <param name="controlTypes">Attribute control types to be filtered.</param>
        /// <returns>Product variant attribute value query.</returns>
        public static IOrderedQueryable<ProductVariantAttributeValue> ApplyValueFilter(
            this IQueryable<ProductVariantAttributeValue> query, 
            int[] valueIds,
            AttributeControlType[] controlTypes = null)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(valueIds, nameof(valueIds));

            var db = query.GetDbContext<SmartDbContext>();

            query = query.Where(x => valueIds.Contains(x.Id));

            if (controlTypes?.Any() ?? false)
            {
                var controlTypeIds = controlTypes.Select(x => (int)x).ToArray();

                query = query.Where(x => controlTypeIds.Contains(x.ProductVariantAttribute.AttributeControlTypeId));
            }

            return query
                .OrderBy(x => x.ProductVariantAttribute.DisplayOrder)
                .ThenBy(x => x.DisplayOrder);
        }
    }
}
