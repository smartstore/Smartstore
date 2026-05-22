#nullable enable

using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common;

namespace Smartstore.Core.Checkout.Orders;

/// <summary>
/// Order service interface.
/// </summary>
public partial interface IOrderService
{
    /// <summary>
    /// Gets the order total in the currency of the customer.
    /// </summary>
    /// <param name="order">Order.</param>
    /// <param name="targetCurrency">The target currency (which is assumed to be the currency specified by <see cref="Order.CustomerCurrencyCode"/>).</param>
    /// <param name="paymentMethod">The payment method used to pay for the order. If it is <c>null</c>, it is retrieved from the database.</param>
    /// <returns>Order total and the rounding amount (if any).</returns>
    Task<(Money OrderTotal, Money RoundingAmount)> GetOrderTotalInCustomerCurrencyAsync(Order order, Currency? targetCurrency, PaymentMethod? paymentMethod = null);
}