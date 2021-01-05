using System.Collections.Generic;
using System.Threading.Tasks;

namespace Smartstore.Core.Checkout.Attributes
{
    /// <summary>
    /// Checkout attribute materialization helper
    /// </summary>
    public partial interface ICheckoutAttributeMaterializer
    {
        /// <summary>
        /// Gets a list of all checkout attributes from <see cref="CheckoutAttributeSelection"/>.
        /// </summary>
        /// <param name="selection">Attribute selection object</param>
        Task<List<CheckoutAttribute>> MaterializeCheckoutAttributesAsync(CheckoutAttributeSelection selection);

        /// <summary>
        /// Gets a list of all checkout attribute values from <see cref="CheckoutAttributeSelection"/>.
        /// </summary>
        /// <param name="selection">Attribute selection object</param>
        Task<List<CheckoutAttributeValue>> MaterializeCheckoutAttributeValuesAsync(CheckoutAttributeSelection selection);
    }
}