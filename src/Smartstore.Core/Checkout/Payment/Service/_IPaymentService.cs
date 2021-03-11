using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common;

namespace Smartstore.Core.Checkout.Payment
{
    public partial interface IPaymentService
    {
        /// <summary>
        /// Gets an additional handling fee of a payment method.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="paymentMethodSystemName">Payment method system name.</param>
        /// <returns>Additional handling fee of a payment method.</returns>
        Task<Money> GetPaymentFeeAsync(IList<OrganizedShoppingCartItem> cart, string paymentMethodSystemName);
    }
}
