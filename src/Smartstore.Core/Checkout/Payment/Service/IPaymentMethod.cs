using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
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
        /// Gets a value indicating whether the payment method requires user input in checkout
        /// before proceeding, e.g. credit card or direct debit payment.
        /// </summary>
        bool RequiresInteraction { get; }

        /// <summary>
        /// Gets a value indicating whether the payment method requires the payment selection page in checkout
        /// before proceeding. For example, to create a payment transaction at this stage.
        /// If <c>false</c>, then the payment method is qualified for Quick Checkout and 
        /// <see cref="CreateProcessPaymentRequestAsync"/> must be implemented.
        /// </summary>
        bool RequiresPaymentSelection { get; }

        /// <summary>
        /// Gets a value indicating whether (later) capturing of the payment amount is supported,
        /// for instance when the goods are shipped.
        /// </summary>
        /// <remarks>If <c>true</c>, then you must overwrite the method <see cref="CaptureAsync(CapturePaymentRequest)"/>.</remarks>
        bool SupportCapture { get; }

        /// <summary>
        /// Gets a value indicating whether a partial refund is supported.
        /// </summary>
        /// <remarks>If <c>true</c>, then you must overwrite the method <see cref="RefundAsync(RefundPaymentRequest)"/>.</remarks>
        bool SupportPartiallyRefund { get; }

        /// <summary>
        /// Gets a value indicating whether a full refund is supported.
        /// </summary>
        /// <remarks>If <c>true</c>, then you must overwrite the method <see cref="RefundAsync(RefundPaymentRequest)"/>.</remarks>
        bool SupportRefund { get; }

        /// <summary>
        /// Gets a value indicating whether cancellation of the payment (transaction) is supported.
        /// </summary>
        /// <remarks>If <c>true</c>, then you must overwrite the method <see cref="VoidAsync(VoidPaymentRequest)"/>.</remarks>
        bool SupportVoid { get; }

        /// <summary>
        /// Gets the type of recurring payment.
        /// </summary>
        RecurringPaymentType RecurringPaymentType { get; }

        /// <summary>
        /// Gets the payment method type.
        /// </summary>
        /// <remarks>Choose a type that best suits your payment method.</remarks>
        PaymentMethodType PaymentMethodType { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the widget invoker for payment info. The payment info is displayed on checkout's payment page.
        /// Return <c>null</c> when there is nothing to render.
        /// </summary>
        Widget GetPaymentInfoWidget();

        /// <summary>
        /// Gets the additional handling fee for a payment.
        /// </summary>
        /// <returns>The fixed fee or a percentage value. If <c>UsePercentage</c> is <c>true</c>, the fee is calculated as a percentage of the order total.</returns>
        Task<(decimal FixedFeeOrPercentage, bool UsePercentage)> GetPaymentFeeInfoAsync(ShoppingCart cart);

        /// <summary>
        /// Gets a <see cref="ProcessPaymentRequest"/>. Called after the customer selected a payment method on checkout's payment page.
        /// </summary>
        /// <param name="form">Form with payment data.</param>
        /// <remarks>
        /// Typically used to specify an <see cref="ProcessPaymentRequest.OrderGuid"/> that can be sent to the payment provider before the order is placed.
        /// It will be saved later when the order is created.
        /// </remarks>
        Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form);

        /// <summary>
        /// Validates the payment data entered by customer on checkout's payment page.
        /// </summary>
        /// <param name="form">Form with payment data.</param>
        /// <returns><see cref="PaymentValidationResult"/> instance</returns>
        Task<PaymentValidationResult> ValidatePaymentDataAsync(IFormCollection form);

        /// <summary>
        /// Gets a short summary of payment data entered by customer in checkout that is displayed on the checkout's confirm page.
        /// </summary>
        /// <returns>Payment summary. <c>null</c> if there is no summary.</returns>
        /// <remarks>Typically used to display the brand name and a masked credit card number.</remarks>
        Task<string> GetPaymentSummaryAsync();

        /// <summary>
        /// Creates a <see cref="ProcessPaymentRequest"/> for automatic fulfillment of a payment request (Quick Checkout).
        /// Only required if <see cref="RequiresPaymentSelection"/> is <c>false</c>.
        /// </summary>
        /// <param name="cart">Current shopping cart.</param>
        /// <param name="lastOrder">The last order of the current customer.</param>
        /// <returns>
        /// <see cref="ProcessPaymentRequest"/> or <c>null</c> if the payment request cannot be fulfilled automatically.
        /// In this case, the customer will be directed to the payment selection page.
        /// </returns>
        Task<ProcessPaymentRequest> CreateProcessPaymentRequestAsync(ShoppingCart cart, Order lastOrder);

        /// <summary>
        /// Pre-process a payment. Called immediately before <see cref="ProcessPaymentAsync(ProcessPaymentRequest)"/>.
        /// </summary>
        /// <param name="request">Payment info required for order processing.</param>
        /// <returns>Pre-process payment result.</returns>
        /// <remarks>
        /// Can be used, for example, to complete required data such as the billing address.
        /// Throw <see cref="PaymentException"/> to abort payment and order placement.
        /// </remarks>
        Task<PreProcessPaymentResult> PreProcessPaymentAsync(ProcessPaymentRequest request);

        /// <summary>
        /// The main method to make a payment. Called immediately before placing the order.
        /// </summary>
        /// <param name="request">Payment info required for order processing.</param>
        /// <returns>Process payment result.</returns>
        /// <remarks>
        /// Intended for main payment processing like payment authorization.
        /// Throw <see cref="PaymentException"/> to abort payment and order placement.
        /// </remarks>
        Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request);

        /// <summary>
		/// Post-process payment. Called after (!) an order has been placed or when the user starts the payment again and is redirected
        /// to the payment page of a third-party provider for this purpose (only required for older payment methods).
        /// </summary>
        /// <param name="request">Payment info required for order processing.</param>
        /// <remarks>
        /// Used, for example, to redirect to a payment page to complete the payment after the order has been placed.
        /// Throw <see cref="PaymentException"/> if a payment error occurs.
        /// </remarks>
        Task PostProcessPaymentAsync(PostProcessPaymentRequest request);

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order
        /// has been placed but not yet completed (only for redirection payment methods).
        /// </summary>
        /// <returns>A value indicating whether re-starting the payment process is supported.</returns>
        Task<bool> CanRePostProcessPaymentAsync(Order order);

        /// <summary>
        /// Captures a payment amount.
        /// </summary>
        /// <param name="request">Capture payment request.</param>
        /// <returns>Capture payment result.</returns>
        /// <remarks>
        /// Throw <see cref="PaymentException"/> if a payment error occurs.
        /// </remarks>
        Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest request);

        /// <summary>
        /// Fully or partially refunds a payment amount.
        /// </summary>
        /// <param name="request">Refund payment request.</param>
        /// <returns>Refund payment result.</returns>
        /// <remarks>
        /// Throw <see cref="PaymentException"/> if a payment error occurs.
        /// </remarks>
        Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest request);

        /// <summary>
        /// Cancels a payment (transaction).
        /// </summary>
        /// <param name="request">Void payment request.</param>
        /// <returns>Void payment result.</returns>
        /// <remarks>
        /// Throw <see cref="PaymentException"/> if a payment error occurs.
        /// </remarks>
        Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest request);

        /// <summary>
        /// Processes a recurring payment.
        /// </summary>
        /// <param name="request">Payment info required for order processing.</param>
        /// <returns>Process payment result.</returns>
        /// <remarks>
        /// Throw <see cref="PaymentException"/> if a payment error occurs.
        /// </remarks>
        Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest request);

        /// <summary>
        /// Cancels a recurring payment.
        /// </summary>
        /// <param name="request">Cancel recurring payment request.</param>
        /// <returns>Cancel recurring payment result.</returns>
        /// <remarks>
        /// Throw <see cref="PaymentException"/> if a payment error occurs.
        /// </remarks>
        Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest request);

        #endregion
    }
}