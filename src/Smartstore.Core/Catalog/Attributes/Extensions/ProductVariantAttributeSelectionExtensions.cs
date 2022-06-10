using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Attributes
{
    public static partial class ProductVariantAttributeSelectionExtensions
    {
        /// <summary>
        /// Gets a list of product variant attribute values from an attribute selection. 
        /// Loads attribute values from <paramref name="attributes"/> and not from database.
        /// Typically used in conjunction with <see cref="ProductBatchContext"/>.
        /// Only returns values of list type attributes (<see cref="ProductVariantAttribute.IsListTypeAttribute()"/>).
        /// </summary>
        /// <param name="selection">Attributes selection.</param>
        /// <param name="attributes">Attributes from which the values are loaded.</param>
        /// <returns>List of product variant attribute values.</returns>
        public static IList<ProductVariantAttributeValue> MaterializeProductVariantAttributeValues(
            this ProductVariantAttributeSelection selection,
            IEnumerable<ProductVariantAttribute> attributes)
        {
            Guard.NotNull(attributes, nameof(attributes));

            var result = new List<ProductVariantAttributeValue>();

            if (selection?.AttributesMap?.Any() ?? false)
            {
                var listTypeAttributeIds = attributes
                    .Where(x => x.IsListTypeAttribute())
                    .OrderBy(x => x.DisplayOrder)
                    .Select(x => x.Id)
                    .Distinct()
                    .ToArray();

                var valueIds = selection.AttributesMap
                    .Where(x => listTypeAttributeIds.Contains(x.Key))
                    .SelectMany(x => x.Value)
                    .Select(x => x.ToString())
                    .Where(x => x.HasValue())   // Avoid exception when string is empty.
                    .Select(x => x.ToInt())
                    .Where(x => x != 0)
                    .Distinct()
                    .ToArray();

                foreach (int valueId in valueIds)
                {
                    foreach (var attribute in attributes)
                    {
                        var attributeValue = attribute.ProductVariantAttributeValues.FirstOrDefault(x => x.Id == valueId);
                        if (attributeValue != null)
                        {
                            result.Add(attributeValue);
                            break;
                        }
                    }
                }
            }

            return result;
        }
    }
}
