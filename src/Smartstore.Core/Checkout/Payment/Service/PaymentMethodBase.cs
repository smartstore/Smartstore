using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Localization;
using Smartstore.Core.Widgets;

namespace Smartstore.Core.Checkout.Payment
{
    /// <summary>
    /// Base class to implement payment methods.
    /// </summary>
    public abstract class PaymentMethodBase : IPaymentMethod
    {
        public Localizer T { get; set; } = NullLocalizer.Instance;

        #region Provider capabilities

        public virtual bool SupportCapture => false;

        public virtual bool SupportPartiallyRefund => false;

        public virtual bool SupportRefund => false;

        public virtual bool SupportVoid => false;

        public virtual RecurringPaymentType RecurringPaymentType
            => RecurringPaymentType.NotSupported;

        public virtual Task<(decimal FixedFeeOrPercentage, bool UsePercentage)> GetPaymentFeeInfoAsync(ShoppingCart cart)
            => Task.FromResult((decimal.Zero, false));

        #endregion


        #region Checkout integration

        public virtual bool RequiresInteraction => false;

        public virtual bool RequiresPaymentSelection => true;

        public virtual PaymentMethodType PaymentMethodType
            => PaymentMethodType.Unknown;

        public abstract Widget GetPaymentInfoWidget();

        public virtual Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
            => Task.FromResult(new ProcessPaymentRequest());

        public virtual Task<PaymentValidationResult> ValidatePaymentDataAsync(IFormCollection form)
            => Task.FromResult(new PaymentValidationResult());

        public virtual Task<string> GetPaymentSummaryAsync()
            => Task.FromResult<string>(null);

        public virtual Task<ProcessPaymentRequest> CreateProcessPaymentRequestAsync(ShoppingCart cart)
            => Task.FromResult(new ProcessPaymentRequest { OrderGuid = Guid.NewGuid() });

        #endregion


        #region Payment confirmation (called after "buy now" button click, before order placement)

        public virtual bool RequiresConfirmation => false;

        public virtual Task<string> GetConfirmationUrlAsync(ProcessPaymentRequest request, CheckoutContext context)
            => Task.FromResult<string>(null);

        public virtual Task CompletePaymentAsync(ProcessPaymentRequest request, CheckoutContext context)
            => Task.CompletedTask;

        #endregion


        #region Payment processing (called during order placement)

        public virtual Task<PreProcessPaymentResult> PreProcessPaymentAsync(ProcessPaymentRequest request)
            => Task.FromResult(new PreProcessPaymentResult());

        public abstract Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest);

        public virtual Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
            => Task.CompletedTask;

        #endregion


        #region After-sales operations (called after order has been placed)

        public virtual Task<bool> CanRePostProcessPaymentAsync(Order order)
            => Task.FromResult(false);

        public virtual Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
            => throw new PaymentException(T("Common.Payment.NoCaptureSupport"));

        public virtual Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
            => throw new PaymentException(T("Common.Payment.NoRefundSupport"));

        public virtual Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
            => throw new PaymentException(T("Common.Payment.NoVoidSupport"));

        public virtual Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
            => throw new PaymentException(T("Common.Payment.NoRecurringPaymentSupport"));

        public virtual Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
            => throw new PaymentException(T("Common.Payment.NoRecurringPaymentSupport"));

        #endregion
    }
}