using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Caching;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.StripeElements.Components;
using Smartstore.StripeElements.Controllers;
using Smartstore.StripeElements.Models;
using Smartstore.StripeElements.Settings;

namespace Smartstore.StripeElements.Providers
{
    [SystemName("Payments.StripeElements")]
    [FriendlyName("Stripe Elements")]
    [Order(1)]
    public class StripeElementsProvider : PaymentMethodBase, IConfigurable
    {
        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;
        private readonly ISettingFactory _settingFactory;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IRoundingHelper _roundingHelper;
        private readonly ICacheManager _cache;
        private readonly StripeSettings _settings;

        public StripeElementsProvider(
            SmartDbContext db,
            IStoreContext storeContext,
            ISettingFactory settingFactory,
            ICheckoutStateAccessor checkoutStateAccessor,
            IRoundingHelper roundingHelper,
            ICacheManager cache,
            StripeSettings settings)
        {
            _db = db;
            _storeContext = storeContext;
            _settingFactory = settingFactory;
            _checkoutStateAccessor = checkoutStateAccessor;
            _roundingHelper = roundingHelper;
            _cache = cache;
            _settings = settings;

            // Ensure API is set with current module settings. 
            if (StripeConfiguration.ApiKey != _settings.SecrectApiKey)
            {
                StripeConfiguration.ApiKey = _settings.SecrectApiKey;
            }
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public static string SystemName => "Payments.StripeElements";

        public override bool SupportCapture => true;

        public override bool SupportVoid => true;

        public override bool SupportPartiallyRefund => true;

        public override bool SupportRefund => true;

        public override bool RequiresInteraction => true;

        public override PaymentMethodType PaymentMethodType => PaymentMethodType.StandardAndButton;

        public RouteInfo GetConfigurationRoute()
            => new(nameof(StripeAdminController.Configure), "StripeAdmin", new { area = "Admin" });

        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(typeof(StripeElementsViewComponent));

        public override Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            var request = new ProcessPaymentRequest
            {
                OrderGuid = Guid.NewGuid()
            };

            return Task.FromResult(request);
        }

        public override async Task<(decimal FixedFeeOrPercentage, bool UsePercentage)> GetPaymentFeeInfoAsync(ShoppingCart cart)
        {
            var settings = await _settingFactory.LoadSettingsAsync<StripeSettings>(_storeContext.CurrentStore.Id);

            return (settings.AdditionalFee, settings.AdditionalFeePercentage);
        }

        public override async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            // INFO: Real process payment happens in StripeController > ConfirmOrder

            if (processPaymentRequest.OrderGuid == Guid.Empty)
            {
                throw new Exception($"{nameof(processPaymentRequest.OrderGuid)} is missing.");
            }

            var result = new ProcessPaymentResult();
            var state = _checkoutStateAccessor.CheckoutState.GetCustomState<StripeCheckoutState>();

            if (state.PaymentIntent.Id.IsEmpty())
            {
                throw new Exception(T("Payment.MissingCheckoutState", "StripeCheckoutState." + nameof(state.PaymentIntent.Id)));
            }

            // Store PaymentIntent.Id in AuthorizationTransactionId.
            result.AuthorizationTransactionId = state.PaymentIntent.Id;

            var settings = await _settingFactory.LoadSettingsAsync<StripeSettings>(processPaymentRequest.StoreId);

            result.NewPaymentStatus = settings.CaptureMethod == "automatic"
                ? PaymentStatus.Paid
                : PaymentStatus.Authorized;

            return result;
        }

        public override async Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest request)
        {
            var result = new RefundPaymentResult
            {
                NewPaymentStatus = request.Order.PaymentStatus
            };

            var options = new RefundCreateOptions { PaymentIntent = request.Order.AuthorizationTransactionId };

            if (request.IsPartialRefund)
            {
                options.Amount = _roundingHelper.ToSmallestCurrencyUnit(request.AmountToRefund);
            }

            var service = new RefundService();
            var refund = await service.CreateAsync(options);

            if (refund.Id.HasValue() && request.Order.Id != 0)
            {
                var refundIds = request.Order.GenericAttributes.Get<List<string>>("Payments.StripeElements.RefundId") ?? new List<string>();
                if (!refundIds.Contains(refund.Id))
                {
                    refundIds.Add(refund.Id);
                }

                request.Order.GenericAttributes.Set("Payments.StripeElements.RefundId", refundIds);
                await _db.SaveChangesAsync();

                result.NewPaymentStatus = request.IsPartialRefund ? PaymentStatus.PartiallyRefunded : PaymentStatus.Refunded;
            }

            return result;
        }

        public override async Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest request)
        {
            var result = new CapturePaymentResult
            {
                NewPaymentStatus = request.Order.PaymentStatus
            };

            try
            {
                // INFO: PaymentIntent is stored in AuthorizationTransactionId
                var service = new PaymentIntentService();
                var response = await service.CaptureAsync(request.Order.AuthorizationTransactionId);

                result.CaptureTransactionResult = response.Status;

                result.NewPaymentStatus = PaymentStatus.Paid;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
            }

            return result;
        }

        public override async Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest request)
        {
            var order = request.Order;
            var result = new VoidPaymentResult
            {
                NewPaymentStatus = request.Order.PaymentStatus
            };

            // INFO: payment intent must have one of the following statuses, otherwise it will throw
            // requires_payment_method, requires_capture, requires_confirmation, requires_action

            if (order.PaymentStatus == PaymentStatus.Pending || order.PaymentStatus == PaymentStatus.Authorized)
            {
                // INFO: PaymentIntent is stored in AuthorizationTransactionId
                var service = new PaymentIntentService();
                await service.CancelAsync(request.Order.AuthorizationTransactionId);

                result.NewPaymentStatus = PaymentStatus.Voided;
            }

            return result;
        }
    }
}