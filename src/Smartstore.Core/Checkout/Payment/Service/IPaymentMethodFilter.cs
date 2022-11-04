using Smartstore.Core.Checkout.Cart;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Payment
{
    /// <summary>
    /// Allows to filter out payment methods.
    /// </summary>
	public partial interface IPaymentMethodFilter
    {
        /// <summary>
        /// Gets a value indicating whether a payment method should be filtered out.
        /// </summary>
        /// <param name="request">Payment filter request.</param>
        /// <returns><c>True</c> filter out method, <c>False</c> do not filter out method.</returns>
        Task<bool> IsExcludedAsync(PaymentFilterRequest request);
    }

    public partial class PaymentFilterRequest
    {
        /// <summary>
        /// The payment method to be checked.
        /// </summary>
        public Provider<IPaymentMethod> PaymentMethod { get; set; }

        /// <summary>
        /// The context shopping cart. Can be <c>null</c>.
        /// </summary>
        public ShoppingCart Cart { get; set; }

        /// <summary>
        /// The context store identifier.
        /// </summary>
        public int StoreId { get; set; }
    }
}