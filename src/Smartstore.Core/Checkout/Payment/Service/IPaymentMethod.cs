using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Payment
{
    /// <summary>
    /// Provides an interface for creating payment gateways and methods.
    /// </summary>
    public partial interface IPaymentMethod : IProvider, IUserEditable
    {
        #region Properties

        /// <summary>
        /// Gets a value indicating whether the payment method is active and should be offered to customers
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets a value indicating whether the payment method requires user input
        /// before proceeding (e.g. CreditCard, DirectDebit etc.)
        /// </summary>
        bool RequiresInteraction { get; }

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        bool SupportCapture { get; }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        bool SupportPartiallyRefund { get; }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        bool SupportRefund { get; }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        bool SupportVoid { get; }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        RecurringPaymentType RecurringPaymentType { get; }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        PaymentMethodType PaymentMethodType { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Pre process a payment.
        /// </summary>
        /// <param name="request">Payment info required for order processing.</param>
        /// <returns>Pre process payment result.</returns>
        Task<PreProcessPaymentResult> PreProcessPaymentAsync(ProcessPaymentRequest request);

        /// <summary>
        /// Process a payment.
        /// </summary>
        /// <param name="request">Payment info required for order processing.</param>
        /// <returns>Process payment result.</returns>
        Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request);

        /// <summary>
		/// Post process payment (e.g. used by payment gateways to redirect to a third-party URL).
		/// Called after an order has been placed or when customer re-posted the payment.
        /// </summary>
        /// <param name="request">Payment info required for order processing.</param>
        Task PostProcessPaymentAsync(PostProcessPaymentRequest request);

        /// <summary>
        /// Gets additional handling fee.
        /// </summary>
        /// <param name="cart">Shoping cart.</param>
        /// <returns>Additional handling fee.</returns>
		Task<Money> GetAdditionalHandlingFeeAsync(IList<OrganizedShoppingCartItem> cart);

        /// <summary>
        /// Captures payment.
        /// </summary>
        /// <param name="request">Capture payment request.</param>
        /// <returns>Capture payment result.</returns>
        Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest request);

        /// <summary>
        /// Refunds a payment.
        /// </summary>
        /// <param name="request">Refund payment request.</param>
        /// <returns>Refund payment result.</returns>
        Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest request);

        /// <summary>
        /// Voids a payment.
        /// </summary>
        /// <param name="request">Void payment request.</param>
        /// <returns>Void payment result.</returns>
        Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest request);

        /// <summary>
        /// Process recurring payment.
        /// </summary>
        /// <param name="request">Payment info required for order processing.</param>
        /// <returns>Process payment result.</returns>
        Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest request);

        /// <summary>
        /// Cancels a recurring payment.
        /// </summary>
        /// <param name="request">Cancel recurring payment request.</param>
        /// <returns>Cancel recurring payment result.</returns>
        Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest request);

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods).
        /// </summary>
        /// <param name="order">Order placed</param>
        /// <returns>Value indicating wheter it is possible to repost process payment.</returns>
        Task<bool> CanRePostProcessPaymentAsync(Order order);

        /// <summary>
        /// Gets the widget invoker for provider configuration. Returns <c>null</c> when there is nothing to render.
        /// </summary>
        WidgetInvoker GetConfigurationWidget();

        /// <summary>
        /// Gets the widget invoker for payment info. Return <c>null</c> when there's nothing to render.
        /// </summary>
        WidgetInvoker GetPaymentInfoWidget();

        /// <summary>
        /// Gets the controller type. It is required by checkout to validate payment forms and to get payment summary and info.
        /// </summary>
        /// <returns>Type of controller.</returns>
        Type GetControllerType();

        #endregion
    }
}