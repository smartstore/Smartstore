using System.Collections.Generic;
using System.Linq;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore
{
    /// <summary>
    /// Checkout attribute extensions.
    /// </summary>
    public static class CheckoutAttributeExtensions
    {
        /// <summary>
        /// Gets invalid shippable attribute ids.
        /// Invalid shippable attributes are attributes that require a shippable product, when the shopping cart does not require shipping at all.
        /// </summary>
        /// <returns><see cref="IEnumerable{int}"/> with invalid shippable attribute identifiers.</returns>
        public static IEnumerable<int> GetInvalidShippableAttributesIds(this IEnumerable<CheckoutAttribute> attributes, IList<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(attributes, nameof(attributes));

            if (cart.IsShippingRequired())
                return Enumerable.Empty<int>();

            return attributes
                .Where(x => x.ShippableProductRequired)
                .Select(x => x.Id);
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