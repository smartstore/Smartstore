using System.Threading.Tasks;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.PayPal.Client;
using Smartstore.PayPal.Components;
using Smartstore.PayPal.Settings;

namespace Smartstore.PayPal.Providers
{
    [SystemName("Payments.PayPalStandard")]
    [FriendlyName("PayPal Standard")]
    [Order(1)]
    public class PayPalStandardProvider : PaymentMethodBase, IConfigurable
    {
        private readonly PayPalHttpClient _client;
        private readonly PayPalSettings _settings;

        public PayPalStandardProvider(PayPalHttpClient client, PayPalSettings settings)
        {
            _client = client;
            _settings = settings;
        }

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "PayPal", new { area = "Admin" });

        public override bool SupportCapture => true;

        public override bool SupportPartiallyRefund => true;

        public override bool SupportRefund => true;

        public override bool SupportVoid => true;

        public override RecurringPaymentType RecurringPaymentType => RecurringPaymentType.Automatic;

        public override PaymentMethodType PaymentMethodType => PaymentMethodType.StandardAndButton;

        public override Task<(decimal FixedFeeOrPercentage, bool UsePercentage)> GetPaymentFeeInfoAsync(ShoppingCart cart) 
            => Task.FromResult<(decimal FixedFeeOrPercentage, bool UsePercentage)>(new(_settings.AdditionalFee, _settings.AdditionalFeePercentage));

        public override WidgetInvoker GetPaymentInfoWidget()
            => new ComponentWidgetInvoker(typeof(PayPalViewComponent), true);

        public override async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request)
        {
            var result = new ProcessPaymentResult
            {
                NewPaymentStatus = PaymentStatus.Pending,
            };

            
            //TODO: (mh) (core) OBSOLETE > Remove
            //await _client.GetOrder(request);

            _ = await _client.UpdateOrderAsync(request, result);

            //await _client.GetOrder(request);

            if (_settings.Intent == "authorize")
            {
                var response = await _client.AuthorizeOrderAsync(request, result);
            }
            else
            {
                var response = await _client.CaptureOrderAsync(request, result);
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

            var response = await _client.RefundPaymentAsync(request, result);

            return result;
        }

        // TODO: (mh) (core)
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
