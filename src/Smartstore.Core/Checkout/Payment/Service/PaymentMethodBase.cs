using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Localization;
using Smartstore.Core.Widgets;

namespace Smartstore.Core.Checkout.Payment
{
    /// <summary>
    /// Base class for payment methods.
    /// </summary>
    public abstract class PaymentMethodBase : IPaymentMethod
    {
        public Localizer T { get; set; } = NullLocalizer.Instance;

        #region Properties

        /// <inheritdoc/>
        public virtual bool IsActive => true;

        /// <inheritdoc/>
        public virtual bool RequiresInteraction => false;

        /// <inheritdoc/>
        public virtual bool SupportCapture => false;

        /// <inheritdoc/>
        public virtual bool SupportPartiallyRefund => false;

        /// <inheritdoc/>
        public virtual bool SupportRefund => false;

        /// <inheritdoc/>
        public virtual bool SupportVoid => false;

        /// <inheritdoc/>
        public virtual RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        /// <inheritdoc/>
        public virtual PaymentMethodType PaymentMethodType => PaymentMethodType.Unknown;

        #endregion

        #region Methods

        /// <inheritdoc/>
        public virtual Task<PreProcessPaymentResult> PreProcessPaymentAsync(ProcessPaymentRequest request)
            => Task.FromResult(new PreProcessPaymentResult());

        /// <inheritdoc/>
        public abstract Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest);

        /// <inheritdoc/>
        public virtual Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
            => Task.CompletedTask;

        /// <inheritdoc/>
        public virtual Task<(decimal FixedFeeOrPercentage, bool UsePercentage)> GetPaymentFeeInfoAsync(ShoppingCart cart)
            => Task.FromResult((decimal.Zero, false));

        /// <inheritdoc/>
        public virtual Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
            => Task.FromResult(new ProcessPaymentRequest());

        /// <inheritdoc/>
        public virtual Task<List<string>> GetPaymentDataWarningsAsync()
            => Task.FromResult<List<string>>(null);

        /// <inheritdoc/>
        public virtual Task<string> GetPaymentSummaryAsync()
            => Task.FromResult((string)null);

        /// <inheritdoc/>
        public virtual Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.Errors.Add(T("Common.Payment.NoCaptureSupport"));
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public virtual Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.Errors.Add(T("Common.Payment.NoRefundSupport"));
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public virtual Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.Errors.Add(T("Common.Payment.NoVoidSupport"));
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public virtual Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.Errors.Add(T("Common.Payment.NoRecurringPaymentSupport"));
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public virtual Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.Errors.Add(T("Common.Payment.NoRecurringPaymentSupport"));
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public virtual Task<bool> CanRePostProcessPaymentAsync(Order order) 
            => Task.FromResult(false);

        /// <inheritdoc/>
        public abstract WidgetInvoker GetPaymentInfoWidget();

        #endregion
    }
}