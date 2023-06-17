using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Rules;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Payment
{
    /// <summary>
    /// Payment service interface.
    /// </summary>
    public partial interface IPaymentService
    {
        #region Provider management

        /// <summary>
        /// Checks whether a payment provider is globally enabled, and also enabled for the given store.
        /// This method does NOT match rules or filters.
        /// </summary>
        /// <param name="systemName">System name of the payment provider.</param>
        /// <param name="storeId">Optional store id to check for.</param>
        /// <returns><c>True</c> payment method is enabled, otherwise <c>false</c>.</returns>
        Task<bool> IsPaymentProviderEnabledAsync(string systemName, int storeId = 0);

        /// <summary>
        /// Checks that a payment provider is enabled and active, not filtered out, and matches the applied rule sets.
        /// A payment method that meets these requirements will appear in the checkout.
        /// </summary>
        /// <param name="systemName">System name of the payment provider.</param>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="storeId">Optional store id to check for.</param>
        /// <returns><c>True</c> payment method is active, otherwise <c>false</c>.</returns>
        Task<bool> IsPaymentProviderActiveAsync(string systemName, ShoppingCart cart = null, int storeId = 0);

        /// <summary>
        /// Loads payment methods that are active, not filtered out, and match the applied rule sets.
        /// Payment methods that meet these requirements will appear in the checkout.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="storeId">Filter payment provider by store identifier. 0 to load all.</param>
        /// <param name="types">Filter payment methods by payment method types.</param>
        /// <param name="provideFallbackMethod">Provide a fallback payment method if there is no match.</param>
        /// <returns>Filtered payment methods.</returns>
        Task<IEnumerable<Provider<IPaymentMethod>>> LoadActivePaymentProvidersAsync(
            ShoppingCart cart = null,
            int storeId = 0,
            PaymentMethodType[] types = null,
            bool provideFallbackMethod = true);

        /// <summary>
        /// Loads all payment providers.
        /// </summary>
        /// <param name="onlyEnabled"><c>true</c> to only load enabled provider.</param>
        /// <param name="storeId">Filter payment provider by store identifier. 0 to load all.</param>
        /// <returns>Payment provider.</returns>
        Task<IEnumerable<Provider<IPaymentMethod>>> LoadAllPaymentProvidersAsync(bool onlyEnabled = false, int storeId = 0);

        /// <summary>
        /// Loads a payment provider by system name.
        /// </summary>
        /// <param name="systemName">System name of the payment provider.</param>
        /// <param name="onlyWhenEnabled"><c>true</c> to only load an enabled provider.</param>
        /// <param name="storeId">Optional store id to check for.</param>
        /// <returns>Payment provider.</returns>
        Task<Provider<IPaymentMethod>> LoadPaymentProviderBySystemNameAsync(string systemName, bool onlyWhenEnabled = false, int storeId = 0);

        /// <summary>
        /// Reads all configured payment methods from the database.
        /// </summary>
        /// <param name="withRules">
        /// <c>true</c> will eage load all assigned <see cref="PaymentMethod.RuleSets"/>, 
        /// then <see cref="RuleSetEntity.Rules"/> from database.
        /// </param>
        /// <returns>All payment methods.</returns>
        Task<Dictionary<string, PaymentMethod>> GetAllPaymentMethodsAsync(bool withRules = false);

        #endregion

        #region Payment processing

        /// <summary>
        /// Pre process a payment before the order is placed.
        /// </summary>
        /// <param name="processPaymentRequest">Process payment request.</param>
        /// <returns>Pre process payment result. Payment and order are cancelled if errors are returned.</returns>
        Task<PreProcessPaymentResult> PreProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest);

        /// <summary>
        /// Process a payment.
        /// </summary>
        /// <param name="processPaymentRequest">Process payment request.</param>
        /// <returns>Process payment result.</returns>
        Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest);

        /// <summary>
        /// Post process the payment directly after the order has been placed.
        /// Use <see cref="PostProcessPaymentRequest.RedirectUrl"/> if redirecting to a payment page of the payment provider is required.
        /// </summary>
        /// <param name="postProcessPaymentRequest">Post process the payment request.</param>
        Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest);

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed 
        /// (for redirection payment methods, <see cref="PostProcessPaymentRequest.RedirectUrl"/>).
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns><c>True</c> if the payment can be re-started, otherwise <c>false</c>.</returns>
        Task<bool> CanRePostProcessPaymentAsync(Order order);

        /// <summary>
        /// Captures payment.
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request.</param>
        /// <returns>Capture payment result.</returns>
        Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest);

        /// <summary>
        /// Refunds a payment.
        /// </summary>
        /// <param name="refundPaymentRequest">Refund payment request.</param>
        /// <returns>Refund payment result.</returns>
        Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest);

        /// <summary>
        /// Voids a payment.
        /// </summary>
        /// <param name="voidPaymentRequest">Void payment request.</param>
        /// <returns>Void payment result.</returns>
        Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest);

        /// <summary>
        /// Process recurring payment.
        /// </summary>
        /// <param name="processPaymentRequest">Proces payment request.</param>
        /// <returns>Process payment result.</returns>
        Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest);

        /// <summary>
        /// Gets masked credit card number.
        /// </summary>
        /// <param name="creditCardNumber">Credit card number.</param>
        /// <returns>Masked credit card number.</returns>
        string GetMaskedCreditCardNumber(string creditCardNumber);

        #endregion

        #region Recurring payment

        /// <summary>
        /// Gets the next recurring payment date.
        /// </summary>
        /// <param name="recurringPayment">Recurring payment.</param>
        /// <returns>Next recurring payment date. <c>null</c> if there is no next payment date.</returns>
        Task<DateTime?> GetNextRecurringPaymentDateAsync(RecurringPayment recurringPayment);

        /// <summary>
        /// Gets the remaining cycles of a recurring payment.
        /// </summary>
        /// <param name="recurringPayment">Recurring payment.</param>
        /// <returns>Remaining cycles of a recurring payment.</returns>
        Task<int> GetRecurringPaymentRemainingCyclesAsync(RecurringPayment recurringPayment);

        /// <summary>
        /// Cancels a recurring payment.
        /// </summary>
        /// <param name="cancelPaymentRequest">Cancel recurring payment request.</param>
        /// <returns>Cancel recurring payment result.</returns>
        Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest);

        #endregion
    }
}
