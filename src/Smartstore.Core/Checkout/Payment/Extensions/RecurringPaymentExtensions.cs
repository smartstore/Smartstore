using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Payment
{
    public static partial class RecurringPaymentExtensions
    {
        /// <summary>
        /// Gets a value indicating whether the recurring payment can be canceled (by the customer).
        /// </summary>
        /// <param name="recurringPayment">The <see cref="RecurringPayment"/> to be canceled.</param>
        /// <param name="customerToValidate">The <see cref="Customer"/> who wants to cancel.</param>
        /// <returns><c>True</c> when the <paramref name="recurringPayment"/> can be canceled by <paramref name="customerToValidate"/>, otherwise <c>false</c>.</returns>
        public static bool IsCancelable(this RecurringPayment recurringPayment, Customer customerToValidate)
        {
            Guard.NotNull(recurringPayment, nameof(recurringPayment));
            Guard.NotNull(customerToValidate, nameof(customerToValidate));

            var initialOrder = recurringPayment.InitialOrder;
            if (initialOrder == null)
                return false;

            var customer = recurringPayment.InitialOrder.Customer;
            if (customer == null)
                return false;

            if (initialOrder.OrderStatus == OrderStatus.Cancelled
                || !customerToValidate.IsAdmin() && customer.Id != customerToValidate.Id
                || !recurringPayment.NextPaymentDate.HasValue)
                return false;

            return true;
        }
    }
}
