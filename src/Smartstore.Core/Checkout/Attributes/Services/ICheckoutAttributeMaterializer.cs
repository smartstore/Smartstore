using System.Collections.Generic;
using System.Threading.Tasks;

namespace Smartstore.Core.Checkout.Attributes
{
    /// <summary>
    /// Checkout attribute materialization helper
    /// Can retrieve <see cref="CheckoutAttribute"/> and <see cref="CheckoutAttributeValue"/> from <see cref="CheckoutAttributeSelection"/>.
    /// </summary>
    public partial interface ICheckoutAttributeMaterializer
    {
        /// <summary>
        /// Retrieves a list of all <see cref="CheckoutAttribute"/> from <see cref="CheckoutAttributeSelection"/>.
        /// </summary>
        /// <param name="selection">Attribute selection</param>
        /// <example>
        /// MaterializeCheckoutAttributesAsync(new(rawAttributes));
        /// </example>
        /// <returns>
        /// <see cref="List{T}"/> of <see cref="CheckoutAttribute"/>.
        /// </returns>
        Task<List<CheckoutAttribute>> MaterializeCheckoutAttributesAsync(CheckoutAttributeSelection selection);

        /// <summary>
        /// Retrieves a list of all <see cref="CheckoutAttributeValue"/> from <see cref="CheckoutAttributeSelection"/>.
        /// </summary>
        /// <param name="selection">Attribute selection</param>
        /// <returns>
        /// <see cref="List{T}"/> of <see cref="CheckoutAttributeValue"/>.
        /// </returns>
        Task<List<CheckoutAttributeValue>> MaterializeCheckoutAttributeValuesAsync(CheckoutAttributeSelection selection);
    }
}