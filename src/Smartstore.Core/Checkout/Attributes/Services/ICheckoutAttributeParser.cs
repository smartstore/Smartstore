using Smartstore.Core.Checkout.Cart;
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
        IEnumerable<int> ParseCheckoutAttributeIds(string attributes);

        /// <summary>
        /// Gets selected checkout attributes
        /// </summary>
        Task<List<CheckoutAttribute>> ParseCheckoutAttributesAsync(string attributes);

        /// <summary>
        /// Gets checkout attribute values
        /// </summary>
        Task<List<CheckoutAttributeValue>> ParseCheckoutAttributeValuesAsync(string attributes);

        /// <summary>
        /// Gets selected checkout attribute value
        /// </summary>
        IList<string> ParseValues(string attributes, int attributeId);

        /// <summary>
        /// Removes checkout attributes which cannot be applied to the current cart and returns an updated attributes XML string
        /// </summary>
        Task<string> RemoveNotApplicableAttributesAsync(string attributes, IList<OrganizedShoppingCartItem> cart);
    }
}