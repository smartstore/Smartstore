using System.Linq;
using Amazon.Pay.API.WebStore.Charge;
using Amazon.Pay.API.WebStore.ChargePermission;
using Amazon.Pay.API.WebStore.CheckoutSession;
using Amazon.Pay.API.WebStore.Interfaces;
using Amazon.Pay.API.WebStore.Refund;
using Amazon.Pay.API.WebStore.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.AmazonPay.Components;
using Smartstore.AmazonPay.Services;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;
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
        private readonly IStoreContext _storeContext;
        private readonly IWebHelper _webHelper;
        private readonly ICurrencyService _currencyService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ISettingFactory _settingFactory;
        private readonly IAmazonPayService _amazonPayService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IRoundingHelper _roundingHelper;

        public AmazonPayProvider(
            SmartDbContext db,
            IStoreContext storeContext,
            IWebHelper webHelper,
            ICurrencyService currencyService,
            IOrderCalculationService orderCalculationService,
            ISettingFactory settingFactory,
            IAmazonPayService amazonPayService,
            IHttpContextAccessor httpContextAccessor,
            ICheckoutStateAccessor checkoutStateAccessor,
            IRoundingHelper roundingHelper)
        {
            _db = db;
            _storeContext = storeContext;
            _webHelper = webHelper;
            _currencyService = currencyService;
            _orderCalculationService = orderCalculationService;
            _settingFactory = settingFactory;
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

        #region Provider capabilities

        public override bool SupportCapture => true;

        public override bool SupportPartiallyRefund => true;

        public override bool SupportRefund => true;

        public override bool SupportVoid => true;

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "AmazonPayAdmin", new { area = "Admin" });

        public override async Task<(decimal FixedFeeOrPercentage, bool UsePercentage)> GetPaymentFeeInfoAsync(ShoppingCart cart)
        {
            var settings = await _settingFactory.LoadSettingsAsync<AmazonPaySettings>(_storeContext.CurrentStore.Id);

            return (settings.AdditionalFee, settings.AdditionalFeePercentage);
        }

        #endregion


        #region Checkout integration

        public override PaymentMethodType PaymentMethodType => PaymentMethodType.Button;

        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(typeof(PayButtonViewComponent));

        #endregion


        #region Payment confirmation (called after "buy now" button click, before order placement)

        public override bool RequiresConfirmation => true;

        public override async Task<string> GetConfirmationUrlAsync(ProcessPaymentRequest request, CheckoutContext context)
        {
            var cart = context.Cart;
            var store = _storeContext.GetStoreById(cart.StoreId);
            var protocol = _webHelper.IsCurrentConnectionSecured() ? "https" : "http";
            var settings = await _settingFactory.LoadSettingsAsync<AmazonPaySettings>(cart.StoreId);
            var amazonState = _checkoutStateAccessor.CheckoutState.GetCustomState<AmazonPayCheckoutState>();
            var cartTotal = (Money?)await _orderCalculationService.GetShoppingCartTotalAsync(cart);
            var updateRequest = new UpdateCheckoutSessionRequest();

            updateRequest.PaymentDetails.ChargeAmount.Amount = _roundingHelper.Round(cartTotal.Value);
            updateRequest.PaymentDetails.ChargeAmount.CurrencyCode = _amazonPayService.GetAmazonPayCurrency();
            // INFO: cannot be 'true' if transaction type is 'AuthorizeWithCapture'.
            updateRequest.PaymentDetails.CanHandlePendingAuthorization = settings.TransactionType == AmazonPayTransactionType.Authorize;
            updateRequest.PaymentDetails.PaymentIntent = settings.TransactionType == AmazonPayTransactionType.AuthorizeAndCapture
                ? PaymentIntent.AuthorizeWithCapture
                : PaymentIntent.Authorize;

            // INFO: amazonCheckoutSessionId query parameter is provided at return URL too but it is more secure to use the state object.
            updateRequest.WebCheckoutDetails.CheckoutResultReturnUrl = context.Action(CheckoutActionNames.PaymentCompleted, protocol: protocol);
            updateRequest.MerchantMetadata.MerchantStoreName = store.Name.Truncate(50);

            if (request.OrderGuid != Guid.Empty)
            {
                updateRequest.MerchantMetadata.MerchantReferenceId = request.OrderGuid.ToString();
            }

            // INFO: Unlike in v1, the constraints can be ignored. They are only returned if mandatory parameters are missing.
            var client = GetClient(store.Id);
            var response = client.UpdateCheckoutSession(amazonState.SessionId, updateRequest);
            return response?.WebCheckoutDetails?.AmazonPayRedirectUrl;
        }

        #endregion


        #region Payment processing (called during order placement)

        public override async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            var state = _checkoutStateAccessor.CheckoutState.GetCustomState<AmazonPayCheckoutState>();
            var httpContext = _httpContextAccessor.HttpContext;
            httpContext.Session.TryRemove("AmazonPayResponseStatus");

            if (state.SessionId.IsEmpty())
            {
                throw new AmazonPayException(T("Payment.MissingCheckoutState", "AmazonPayCheckoutState." + nameof(state.SessionId)));
            }

            var client = GetClient(processPaymentRequest.StoreId);
            var session = client.GetCheckoutSession(state.SessionId);
            if (!session.Success || session.StatusDetails.State.EqualsNoCase("Canceled"))
            {
                throw new AmazonPayException(T("Plugins.Payments.AmazonPay.AuthenticationStatusFailureMessage"), session);
            }

            var orderTotal = _roundingHelper.Round(processPaymentRequest.OrderTotal, _currencyService.PrimaryCurrency);
            var completeRequest = new CompleteCheckoutSessionRequest(orderTotal, _amazonPayService.GetAmazonPayCurrency());
            var completeResponse = client.CompleteCheckoutSession(state.SessionId, completeRequest);
            if (completeResponse.Success)
            {
                // A Charge represents a single payment transaction.
                // Can either be created using a valid Charge Permission, or as a result of a successful Checkout Session.
                result.AuthorizationTransactionId = completeResponse.ChargeId;

                // A Charge Permission represents buyer consent to be charged.
                // Can either be requested for a one-time or recurring payment scenario.
                result.AuthorizationTransactionCode = completeResponse.ChargePermissionId;
                result.AuthorizationTransactionResult = completeResponse.StatusDetails.State.Grow(completeResponse.StatusDetails.ReasonCode);

                if (completeResponse.Status == 200)
                {
                    // 200 (OK): authorization succeeded.
                    var settings = await _settingFactory.LoadSettingsAsync<AmazonPaySettings>(processPaymentRequest.StoreId);

                    result.NewPaymentStatus = settings.TransactionType == AmazonPayTransactionType.AuthorizeAndCapture
                        ? PaymentStatus.Paid
                        : PaymentStatus.Authorized;
                }

                httpContext.Session.SetString("AmazonPayResponseStatus", completeResponse.Status.ToString());
            }
            else
            {
                // 4xx/5xx: authorization failed.
                // Canceled by buyer or by AmazonPay, declined or expired.
                var reason = completeResponse?.StatusDetails?.ReasonCode;
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

                throw new AmazonPayException(message, completeResponse);
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

        #endregion


        #region After-sales operations (called after order has been placed)

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

        #endregion

        protected IWebStoreClient GetClient(int storeId)
        {
            return _httpContextAccessor.HttpContext.GetAmazonPayApiClient(storeId);
        }
    }
}
