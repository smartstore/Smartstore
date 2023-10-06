using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Data;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.PayPal.Client;
using Smartstore.PayPal.Client.Messages;

namespace Smartstore.PayPal.Providers
{
    public abstract class PayPalProviderBase : PaymentMethodBase, IConfigurable
    {
        private readonly SmartDbContext _db;
        private readonly PayPalHttpClient _client;
        private readonly PayPalSettings _settings;
        private readonly IPaymentService _paymentService;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        
        public PayPalProviderBase(
            SmartDbContext db, 
            PayPalHttpClient client, 
            PayPalSettings settings, 
            IPaymentService paymentService,
            ICheckoutStateAccessor checkoutStateAccessor)
        {
            _db = db;
            _client = client;
            _settings = settings;
            _paymentService = paymentService;
            _checkoutStateAccessor = checkoutStateAccessor;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "PayPal", new { area = "Admin" });

        public override bool SupportCapture => true;

        public override bool SupportPartiallyRefund => true;

        public override bool SupportRefund => true;

        public override bool SupportVoid => true;

        public override RecurringPaymentType RecurringPaymentType => RecurringPaymentType.Automatic;

        public override PaymentMethodType PaymentMethodType => PaymentMethodType.StandardAndButton;

        public override async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request)
        {
            var checkoutState = _checkoutStateAccessor.CheckoutState;
            if (!request.PayPalOrderId.HasValue())
            {
                // INFO: In some cases the PayPalOrderId is lost in the ProcessPaymentRequest. Lets check the checkout state and log some infos.
                var paypalCheckoutState = checkoutState.GetCustomState<PayPalCheckoutState>();

                var orderId = paypalCheckoutState.PayPalOrderId.HasValue() ? paypalCheckoutState.PayPalOrderId : checkoutState.CustomProperties["PayPalOrderId"].ToString();
                if (!orderId.HasValue())
                {
                    throw new PayPalException(T("Payment.MissingCheckoutState", "PayPalCheckoutState." + nameof(request.PayPalOrderId)));
                }

                request.PayPalOrderId = orderId;
            }

            var result = new ProcessPaymentResult
            {
                NewPaymentStatus = PaymentStatus.Pending,
            };

            // INFO: Only update order when express button was used.
            // Shipping fee or discounts may have changed the total value of the order.
            var updateOrder = _checkoutStateAccessor.CheckoutState.CustomProperties.ContainsKey("UpdatePayPalOrder");
            if (updateOrder)
            {
                _ = await _client.UpdateOrderAsync(request, result);
            }
            
            try
            {
                var paymentMethod = await _paymentService.LoadPaymentProviderBySystemNameAsync(request.PaymentMethodSystemName);

                if (_settings.Intent == PayPalTransactionType.Authorize && paymentMethod.Value.SupportCapture)
                {
                    var response = await _client.AuthorizeOrderAsync(request, result);
                }
                else
                {
                    var response = await _client.CaptureOrderAsync(request, result);
                }
            }
            catch (Exception ex) 
            {
                Logger.LogError(ex, "Authorization or capturing failed. User was redirected to payment selection.");

                // Delete properties for backward navigation.
                checkoutState.CustomProperties.Remove("PayPalButtonUsed");
                checkoutState.CustomProperties.Remove("UpdatePayPalOrder");

                throw new PayPalException(T("Plugins.Smartstore.PayPal.OrderUpdateFailed"));
            }

            return result;
        }

        public override async Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest request)
        {
            var result = new CapturePaymentResult
            {
                NewPaymentStatus = request.Order.PaymentStatus
            };

            var response = await _client.CapturePaymentAsync(request, result);

            return result;
        }

        public override async Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest request)
        {
            var result = new VoidPaymentResult
            {
                NewPaymentStatus = request.Order.PaymentStatus
            };

            var response = await _client.VoidPaymentAsync(request, result);

            return result;
        }

        public override async Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest request)
        {
            var result = new RefundPaymentResult
            {
                NewPaymentStatus = request.Order.PaymentStatus
            };

            if (!request.Order.CaptureTransactionId.HasValue())
            {
                throw new PayPalException(T("Plugins.Smartstore.PayPal.Refund.NoTransactionId"));
            }

            var response = await _client.RefundPaymentAsync(request, result);
            var refund = response.Body<RefundMessage>();

            if (refund.Id.HasValue() && request.Order.Id != 0)
            {
                var refundIds = request.Order.GenericAttributes.Get<List<string>>("Payments.PayPalStandard.RefundId") ?? new List<string>();
                if (!refundIds.Contains(refund.Id))
                {
                    refundIds.Add(refund.Id);
                }

                request.Order.GenericAttributes.Set("Payments.PayPalStandard.RefundId", refundIds);
                await _db.SaveChangesAsync();

                result.NewPaymentStatus = request.IsPartialRefund ? PaymentStatus.PartiallyRefunded : PaymentStatus.Refunded;
            }

            return result;
        }

        // TODO: (mh) (core) Implement in future
        //public override async Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest request)
        //{
        //    var result = new ProcessPaymentResult
        //    {
        //        NewPaymentStatus = request.Order.PaymentStatus
        //    };

        //    return result;
        //}

        //public override Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest request)
        //{
        //    throw new System.NotImplementedException();
        //}
    }
}
