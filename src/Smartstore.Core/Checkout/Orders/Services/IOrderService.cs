using System.Threading.Tasks;
using Smartstore.Core.Common;

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Order service interface.
    /// </summary>
    public partial interface IOrderService
    {
        /// <summary>
        /// Gets the order total in the currency of the customer.
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns>Order total and the rounding amount (if any).</returns>
        Task<(Money OrderTotal, Money RoundingAmount)> GetOrderTotalInCustomerCurrencyAsync(Order order);
    }
}
