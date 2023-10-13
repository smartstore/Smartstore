using System.Linq;
using Amazon.Pay.API.WebStore.Charge;
using Amazon.Pay.API.WebStore.ChargePermission;
using Amazon.Pay.API.WebStore.CheckoutSession;
using Amazon.Pay.API.WebStore.Interfaces;
using Amazon.Pay.API.WebStore.Refund;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.AmazonPay.Components;
using Smartstore.AmazonPay.Services;
using Smartstore.Core;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.AmazonPay.Providers
{
    [SystemName("Payments.AmazonPay")]
    [FriendlyName("Amazon Pay")]
    [Order(-1)] // AmazonPay review.
    public class AmazonPayProvider : PaymentMethodBase, IConfigurable
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IAmazonPayService _amazonPayService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IRoundingHelper _roundingHelper;

        public AmazonPayProvider(
            SmartDbContext db,
            ICommonServices services,
            IAmazonPayService amazonPayService,
            IHttpContextAccessor httpContextAccessor,
            ICheckoutStateAccessor checkoutStateAccessor,
            IRoundingHelper roundingHelper)
        {
            _db = db;
            _services = services;
            _amazonPayService = amazonPayService;
            _httpContextAccessor = httpContextAccessor;
            _checkoutStateAccessor = checkoutStateAccessor;
            _roundingHelper = roundingHelper;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        /// <summary>Also named "spId".</summary>
        internal static string PlatformId => "A3OJ83WFYM72IY";
        internal static string LeadCode => "SPEXDEAPA-SmartStore.Net-CP-DP";

        public static string SystemName => "Payments.AmazonPay";

        public override bool SupportCapture => true;

        public override bool SupportPartiallyRefund => true;

        public override bool SupportRefund => true;

        public override bool SupportVoid => true;

        public override PaymentMethodType PaymentMethodType => PaymentMethodType.Button;

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "AmazonPayAdmin", new { area = "Admin" });

        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(typeof(PayButtonViewComponent));

        public override async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            var state = _checkoutStateAccessor.CheckoutState.GetCustomState<AmazonPayCheckoutState>();
            var httpContext = _httpContextAccessor.HttpContext;
            httpContext.Session.TryRemove("AmazonPayResponseStatus");

            try
            {
                if (state.SessionId.IsEmpty())
                {
                    throw new AmazonPayException(T("Payment.MissingCheckoutState", "AmazonPayCheckoutState." + nameof(state.SessionId)));
                }

                var client = GetClient(processPaymentRequest.StoreId);
                var orderTotal = _roundingHelper.Round(processPaymentRequest.OrderTotal, _services.CurrencyService.PrimaryCurrency);
                var request = new CompleteCheckoutSessionRequest(orderTotal, _amazonPayService.GetAmazonPayCurrency());
                var response = client.CompleteCheckoutSession(state.SessionId, request);

                if (response.Success)
                {
                    // A Charge represents a single payment transaction.
                    // Can either be created using a valid Charge Permission, or as a result of a successful Checkout Session.
                    result.AuthorizationTransactionId = response.ChargeId;

                    // A Charge Permission represents buyer consent to be charged.
                    // Can either be requested for a one-time or recurring payment scenario.
                    result.AuthorizationTransactionCode = response.ChargePermissionId;

                    result.AuthorizationTransactionResult = response.StatusDetails.State.Grow(response.StatusDetails.ReasonCode);

                    if (response.Status == 200)
                    {
                        // 200 (OK): authorization succeeded.
                        var settings = await _services.SettingFactory.LoadSettingsAsync<AmazonPaySettings>(processPaymentRequest.StoreId);

                        result.NewPaymentStatus = settings.TransactionType == AmazonPayTransactionType.AuthorizeAndCapture
                            ? PaymentStatus.Paid
                            : PaymentStatus.Authorized;
                    }

                    httpContext.Session.SetString("AmazonPayResponseStatus", response.Status.ToString());
                }
                else
                {
                    // 4xx/5xx: authorization failed.
                    // Canceled by buyer or by AmazonPay, declined or expired.
                    var reason = response?.StatusDetails?.ReasonCode;
                    var message = string.Empty;

                    if (reason.EqualsNoCase("AmazonRejected"))
                    {
                        message = T("Plugins.Payments.AmazonPay.AuthorizationSoftDeclineMessage");
                    }
                    else if (reason.EqualsNoCase("HardDeclined"))
                    {
                        message = T("Plugins.Payments.AmazonPay.AuthorizationHardDeclineMessage");
                    }
                    else
                    {
                        message = T("Plugins.Payments.AmazonPay.AuthenticationStatusFailureMessage");
                    }

                    throw new AmazonPayException(message, response);
                }
            }
            finally
            {
                if (state.SubmitForm)
                {
                    // Avoid infinite loop where the confirm-form is automatically submitted over and over again.
                    state.SubmitForm = false;
                }
            }

            return result;
        }

        public override Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var order = postProcessPaymentRequest.Order;

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                var client = GetClient(order.StoreId);
                var request = new CloseChargePermissionRequest(T("Plugins.Payments.AmazonPay.CloseChargeReason").Value.Truncate(255))
                {
                    CancelPendingCharges = false
                };

                var response = client.CloseChargePermission(order.AuthorizationTransactionCode, request);
                if (!response.Success)
                {
                    Logger.Log(response);
                }
            }

            return Task.CompletedTask;
        }

        public override Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            var order = capturePaymentRequest.Order;
            var result = new CapturePaymentResult
            {
                NewPaymentStatus = order.PaymentStatus
            };

            var client = GetClient(order.StoreId);
            var request = new CaptureChargeRequest(order.OrderTotal, _amazonPayService.GetAmazonPayCurrency());
            var response = client.CaptureCharge(order.AuthorizationTransactionId, request);

            if (response.Success)
            {
                var state = response.StatusDetails.State;

                result.CaptureTransactionResult = state.Grow(response.StatusDetails.ReasonCode);

                if (state.EqualsNoCase("Captured"))
                {
                    result.NewPaymentStatus = PaymentStatus.Paid;
                }
            }
            else
            {
                throw new AmazonPayException(response);
            }

            return Task.FromResult(result);
        }

        public override async Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            var order = refundPaymentRequest.Order;
            var result = new RefundPaymentResult
            {
                NewPaymentStatus = order.PaymentStatus
            };

            var client = GetClient(order.StoreId);
            var request = new CreateRefundRequest(order.AuthorizationTransactionId, refundPaymentRequest.AmountToRefund.Amount, _amazonPayService.GetAmazonPayCurrency());
            var response = client.CreateRefund(request);

            if (response.Success)
            {
                result.NewPaymentStatus = refundPaymentRequest.IsPartialRefund ? PaymentStatus.PartiallyRefunded : PaymentStatus.Refunded;

                if (response.RefundId.HasValue() && refundPaymentRequest.Order.Id != 0)
                {
                    var refundIds = order.GenericAttributes.Get<List<string>>(SystemName + ".RefundIds") ?? new List<string>();
                    if (!refundIds.Contains(response.RefundId, StringComparer.OrdinalIgnoreCase))
                    {
                        refundIds.Add(response.RefundId);
                        order.GenericAttributes.Set(SystemName + ".RefundIds", refundIds);
                        await _db.SaveChangesAsync();
                    }
                }
            }
            else
            {
                throw new AmazonPayException(response);
            }

            return result;
        }

        public override Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            var order = voidPaymentRequest.Order;
            var result = new VoidPaymentResult
            {
                NewPaymentStatus = order.PaymentStatus
            };

            if (order.PaymentStatus == PaymentStatus.Pending || order.PaymentStatus == PaymentStatus.Authorized)
            {
                var client = GetClient(order.StoreId);
                var request = new CloseChargePermissionRequest(T("Plugins.Payments.AmazonPay.CloseChargeReason").Value.Truncate(255))
                {
                    CancelPendingCharges = true
                };

                var response = client.CloseChargePermission(order.AuthorizationTransactionCode, request);

                if (response.Success)
                {
                    result.NewPaymentStatus = PaymentStatus.Voided;
                }
                else
                {
                    throw new AmazonPayException(response);
                }
            }

            return Task.FromResult(result);
        }

        public override async Task<(decimal FixedFeeOrPercentage, bool UsePercentage)> GetPaymentFeeInfoAsync(ShoppingCart cart)
        {
            var settings = await _services.SettingFactory.LoadSettingsAsync<AmazonPaySettings>(_services.StoreContext.CurrentStore.Id);

            return (settings.AdditionalFee, settings.AdditionalFeePercentage);
        }

        protected IWebStoreClient GetClient(int storeId)
        {
            return _httpContextAccessor.HttpContext.GetAmazonPayApiClient(storeId);
        }
    }
}
