using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common;
using Smartstore.Core.Localization;
using Smartstore.Core.Web;
using Smartstore.Core.Widgets;

namespace Smartstore.Core.Checkout.Payment
{
    /// <summary>
    /// Base class for payment methods.
    /// </summary>
    public abstract class PaymentMethodBase : IPaymentMethod
    {
        protected PaymentMethodBase()
        {
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }


        #region Properties

        /// <summary>
        /// Gets a value indicating whether the payment method is active and should be offered to customers.
        /// </summary>
        public virtual bool IsActive => true;

        /// <summary>
        /// Gets a value indicating whether the payment method requires user input
        /// before proceeding (e.g. CreditCard, DirectDebit etc.).
        /// </summary>
        public virtual bool RequiresInteraction => false;

        /// <summary>
        /// Gets a value indicating whether capture is supported.
        /// </summary>
        public virtual bool SupportCapture => false;

        /// <summary>
        /// Gets a value indicating whether partial refund is supported.
        /// </summary>
        public virtual bool SupportPartiallyRefund => false;

        /// <summary>
        /// Gets a value indicating whether refund is supported.
        /// </summary>
        public virtual bool SupportRefund => false;

        /// <summary>
        /// Gets a value indicating whether void is supported.
        /// </summary>
        public virtual bool SupportVoid => false;

        /// <summary>
        /// Gets a recurring payment type of payment method.
        /// </summary>
        public virtual RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        /// <summary>
        /// Gets a payment method type.
        /// </summary>
        public virtual PaymentMethodType PaymentMethodType => PaymentMethodType.Unknown;

        #endregion

        #region Methods

        /// <summary>
        /// Pre process payment.
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing.</param>
        /// <returns>Pre process payment result.</returns>
        public virtual Task<PreProcessPaymentResult> PreProcessPaymentAsync(ProcessPaymentRequest request)
            => Task.FromResult(new PreProcessPaymentResult());

        /// <summary>
        /// Process payment.
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing.</param>
        /// <returns>Process payment result.</returns>
        public abstract Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest);

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL).
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing.</param>
        public virtual Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            // No Impl
            return null;
        }

        /// <summary>
        /// Gets additional handling fee.
        /// </summary>
        /// <returns>Additional handling fee.</returns>
        public virtual Task<Money> GetAdditionalHandlingFeeAsync(IList<OrganizedShoppingCartItem> cart)
            => Task.FromResult(new Money());

        /// <summary>
        /// Captures payment.
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request.</param>
        /// <returns>Capture payment result.</returns>
        public virtual Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.Errors.Add(T("Common.Payment.NoCaptureSupport"));
            return Task.FromResult(result);
        }

        /// <summary>
        /// Refunds a payment.
        /// </summary>
        /// <param name="refundPaymentRequest">Refund payment request.</param>
        /// <returns>Refund payment result.</returns>
        public virtual Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.Errors.Add(T("Common.Payment.NoRefundSupport"));
            return Task.FromResult(result);
        }

        /// <summary>
        /// Voids a payment.
        /// </summary>
        /// <param name="voidPaymentRequest">Void payment request.</param>
        /// <returns>Void payment result.</returns>
        public virtual Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.Errors.Add(T("Common.Payment.NoVoidSupport"));
            return Task.FromResult(result);
        }

        /// <summary>
        /// Process recurring payment.
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing.</param>
        /// <returns>Process payment result.</returns>
        public virtual Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.Errors.Add(T("Common.Payment.NoRecurringPaymentSupport"));
            return Task.FromResult(result);
        }

        /// <summary>
        /// Cancels a recurring payment.
        /// </summary>
        /// <param name="cancelPaymentRequest">Cancel recurring payment request.</param>
        /// <returns>Cancel recurring payment result.</returns>
        public virtual Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.Errors.Add(T("Common.Payment.NoRecurringPaymentSupport"));
            return Task.FromResult(result);
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods).
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns><c>True</c> if process payment can re post, otherwise <c>false</c></returns>
        public virtual Task<bool> CanRePostProcessPaymentAsync(Order order) 
            => Task.FromResult(false);

        /// <summary>
        /// Gets the widget invoker for provider configuration. Returns <c>null</c> when there is nothing to render.
        /// </summary>
        public abstract WidgetInvoker GetConfigurationWidget();

        /// <summary>
        /// Gets the widget invoker for payment info. Returns <c>null</c> when there's nothing to render.
        /// </summary>
        public abstract WidgetInvoker GetPaymentInfoWidget();

        /// <summary>
        /// Gets a route for the payment info handler controller action
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        /// <remarks>
        /// The defined route is being redirected to during the checkout process > PaymentInfo page.
        /// Implementors should return <c>null</c> if no redirection occurs.
        /// </remarks>
        public virtual RouteInfo GetPaymentInfoHandlerRoute() 
            => null;

        /// <summary>
        /// Gets the controller type.
        /// </summary>
        /// <returns>Type of controller.</returns>
        public abstract Type GetControllerType();

        #endregion
    }
}