using System.Collections.Generic;
using System.Threading.Tasks;

namespace Smartstore.Core.Checkout.Attributes
{
    /// <summary>
    /// Checkout attribute parser helper
    /// </summary>
    public partial interface ICheckoutAttributeParser
    {
        /// <summary>
        /// Gets selected checkout attribute identifiers
        /// </summary>
        Task<IEnumerable<int>> ParseCheckoutAttributeIdsAsync(string attributes);

        /// <summary>
        /// Gets selected checkout attributes
        /// </summary>
        Task<IEnumerable<CheckoutAttribute>> ParseCheckoutAttributesAsync(string attributes);

        /// <summary>
        /// Gets checkout attribute values
        /// </summary>
        Task<IEnumerable<CheckoutAttributeValue>> ParseCheckoutAttributeValuesAsync(string attributes);

        /// <summary>
        /// Gets selected checkout attribute value
        /// </summary>
        Task<IEnumerable<string>> ParseValuesAsync(string attributes, int attributeId);

        /// <summary>
        /// Removes checkout attributes which cannot be applied to the current cart and returns an update attributes in XML format
        /// </summary>
        // TODO: (core) (ms) needs OrganizedShoppingCartItem here
        //Task<string> EnsureOnlyActiveAttributesAsync(string attributes, IList<OrganizedShoppingCartItem> cart);
    }
}