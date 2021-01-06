using System.Collections.Generic;
using System.Linq;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Checkout.Attributes
{
    /// <summary>
    /// Checkout attribute extensions
    /// </summary>
    public static class CheckoutAttributeExtensions
    {
        /// <summary>
        /// Checks whether this checkout attribute should have values
        /// </summary>
        public static bool ShouldHaveValues(this CheckoutAttribute attribute)
        {
            return attribute is not null
                && attribute.AttributeControlType is not AttributeControlType.TextBox
                or AttributeControlType.MultilineTextbox
                or AttributeControlType.Datepicker
                or AttributeControlType.FileUpload;
        }

        /// <summary>
        /// Removes attributes from list which require shippable products, if there are no shippable products in the cart
        /// </summary>
        public static void RemoveShippableAttributes(this IEnumerable<CheckoutAttribute> attributes, IList<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(attributes, nameof(attributes));

            if (cart.IsNullOrEmpty() || !cart.IsShippingRequired())
                return;

            attributes = attributes.Where(x => !x.ShippableProductRequired);
        }

        /// <summary>
        /// Gets checkout attribute values by id. 
        /// </summary>
        /// <returns>
        /// <see cref="List{string}"/> of attribute values as strings
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