using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Attributes.Domain;

namespace Smartstore.Core.Checkout.Attributes
{
    /// <summary>
    /// Checkout attribute parser helper
    /// </summary>
    public partial interface ICheckoutAttributeParser
    {
        /// <summary>
        /// Gets list of checkout attributes of attributes XML/Json from <see cref="CheckoutAttributeSelection"/>
        /// </summary>
        Task<List<CheckoutAttribute>> ParseCheckoutAttributesAsync(CheckoutAttributeSelection selection);

        /// <summary>
        /// Gets list of checkout attribute values of attributes XML/Json from <see cref="CheckoutAttributeSelection"/>
        /// </summary>
        Task<List<CheckoutAttributeValue>> ParseCheckoutAttributeValuesAsync(CheckoutAttributeSelection selection);
    }
}