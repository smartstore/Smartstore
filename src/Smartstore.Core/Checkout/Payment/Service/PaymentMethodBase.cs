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

        #region Properties

        public virtual bool RequiresInteraction => false;

        public virtual bool RequiresPaymentSelection => true;

        public virtual bool SupportCapture => false;

        public virtual bool SupportPartiallyRefund => false;

        public virtual bool SupportRefund => false;

        public virtual bool SupportVoid => false;

        public virtual RecurringPaymentType RecurringPaymentType
            => RecurringPaymentType.NotSupported;

        public virtual PaymentMethodType PaymentMethodType
            => PaymentMethodType.Unknown;

        #endregion

        #region Methods

        public abstract Widget GetPaymentInfoWidget();

        public virtual Task<(decimal FixedFeeOrPercentage, bool UsePercentage)> GetPaymentFeeInfoAsync(ShoppingCart cart)
            => Task.FromResult((decimal.Zero, false));

        public virtual Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
            => Task.FromResult(new ProcessPaymentRequest());

        public virtual Task<PaymentValidationResult> ValidatePaymentDataAsync(IFormCollection form)
            => Task.FromResult(new PaymentValidationResult());

        public virtual Task<string> GetPaymentSummaryAsync()
            => Task.FromResult<string>(null);

        public virtual Task<ProcessPaymentRequest> CreateProcessPaymentRequestAsync(ShoppingCart cart, Order lastOrder)
            => Task.FromResult(new ProcessPaymentRequest { OrderGuid = Guid.NewGuid() });

        public virtual Task<PreProcessPaymentResult> PreProcessPaymentAsync(ProcessPaymentRequest request)
            => Task.FromResult(new PreProcessPaymentResult());

        public abstract Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest);

        public virtual Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
            => Task.CompletedTask;

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

        /// <inheritdoc/>
        public virtual Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
            => throw new PaymentException(T("Common.Payment.NoRecurringPaymentSupport"));

        #endregion
    }
}