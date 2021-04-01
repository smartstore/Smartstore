using System.Collections.Generic;
using System.Linq;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Attributes;

namespace Smartstore
{
    /// <summary>
    /// Checkout attribute extensions.
    /// </summary>
    public static class CheckoutAttributeExtensions
    {
        /// <summary>
        /// Gets invalid shippable attribute ids from <paramref name="attributes"/>.
        /// </summary>
        /// <returns><see cref="IEnumerable{int}"/> with invalid shippable attribute identifiers.</returns>
        public static IEnumerable<int> GetInvalidShippableAttributesIds(this IEnumerable<CheckoutAttribute> attributes)
        {
            Guard.NotNull(attributes, nameof(attributes));

            return attributes
                .Where(x => x.ShippableProductRequired)
                .Select(x => x.Id);
        }
        /// <summary>
        /// Removes shippable product attributes from <paramref name="attributes"/>.        
        /// </summary>
        /// <returns><see cref="IEnumerable{CheckoutAttribute}"/> with invalid shippable attributes.</returns>
        public static IEnumerable<CheckoutAttribute> RemoveShippableAttributes(this IEnumerable<CheckoutAttribute> attributes)
        {
            Guard.NotNull(attributes, nameof(attributes));

            return attributes.Where(x => !x.ShippableProductRequired);
        }

        /// <summary>
        /// Gets checkout attribute values by id. 
        /// </summary>
        /// <returns>
        /// <see cref="List{string}"/> of attribute values as strings.
        /// </returns>
        public static List<string> GetAttributeValuesById(this IEnumerable<CheckoutAttribute> attributes, int attributeId)
        {
            Guard.NotNull(attributes, nameof(attributes));

            return attributes
                .Where(x => x.Id == attributeId)
                .SelectMany(x => x.CheckoutAttributeValues.Select(x => x.Id.ToString()))
                .ToList();
        }
    }
}