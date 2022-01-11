using System.Net;
using Amazon.Pay.API.Types;
using Amazon.Pay.API.WebStore.Charge;
using Amazon.Pay.API.WebStore.ChargePermission;
using Amazon.Pay.API.WebStore.CheckoutSession;
using Amazon.Pay.API.WebStore.Interfaces;
using Amazon.Pay.API.WebStore.Refund;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.AmazonPay.Components;
using Smartstore.AmazonPay.Services;
using Smartstore.Core;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.AmazonPay.Providers
{
    public class AmazonPayException : PaymentException
    {
        const string ProviderName = "AmazonPay";

        public AmazonPayException(string message, Exception innerException)
            : base(message, innerException, ProviderName)
        {
        }

        public AmazonPayException(string message)
            : base(message, ProviderName)
        {
        }

        public AmazonPayException(string message, AmazonPayResponse response)
            : base(message, new PaymentResponse((HttpStatusCode)response.Status, response.Headers), ProviderName)
        {
        }
    }

    // TODO: (mg) (core) check error handling of payment infrastructure after all have GIT-committed.
    // Check whether all errors are also logged, not only notified. Example: OrderController.RePostPayment has no logging yet.
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
        private readonly IUrlHelper _urlHelper;
        private readonly OrderSettings _orderSettings;

        public AmazonPayProvider(
            SmartDbContext db,
            ICommonServices services,
            IAmazonPayService amazonPayService,
            IHttpContextAccessor httpContextAccessor,
            ICheckoutStateAccessor checkoutStateAccessor,
            IUrlHelper urlHelper,
            OrderSettings orderSettings)
        {
            _db = db;
            _services = services;
            _amazonPayService = amazonPayService;
            _httpContextAccessor = httpContextAccessor;
            _checkoutStateAccessor = checkoutStateAccessor;
            _urlHelper = urlHelper;
            _orderSettings = orderSettings;
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

        public override WidgetInvoker GetPaymentInfoWidget()
            => new ComponentWidgetInvoker(typeof(PayButtonViewComponent), new { providerName = nameof(AmazonPayProvider) });

        public override async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult
            {
                NewPaymentStatus = PaymentStatus.Pending
            };

            var state = _checkoutStateAccessor.GetAmazonPayCheckoutState();
            var httpContext = _httpContextAccessor.HttpContext;
            httpContext.Session.TryRemove(AmazonPayCompletedInfo.Key);

            try
            {
                if (state.SessionId.IsEmpty())
                {
                    throw new AmazonPayException(T("Plugins.Payments.AmazonPay.MissingCheckoutSessionState"));
                }

                var orderTotal = new Money(processPaymentRequest.OrderTotal, _services.CurrencyService.PrimaryCurrency);
                var client = GetClient(processPaymentRequest.StoreId);
                var request = new CompleteCheckoutSessionRequest(orderTotal.RoundedAmount, _amazonPayService.GetAmazonPayCurrency());
                var response = client.CompleteCheckoutSession(state.SessionId, request);

                if (response.Success)
                {
                    // A Charge represents a single payment transaction.
                    // Can either be created using a valid Charge Permission, or as a result of a successful Checkout Session.
                    result.AuthorizationTransactionId = response.ChargeId;

                    // A Charge Permission represents buyer consent to be charged.
                    // Can either be requested for a one-time or recurring payment scenario.
                    result.AuthorizationTransactionCode = response.ChargePermissionId;

                    result.AuthorizationTransactionResult = response.StatusDetails.State.Grow(response.StatusDetails.ReasonCode, " ");

                    if (response.Status == 200)
                    {
                        // 200 (OK): authorization succeeded.
                        var settings = await _services.SettingFactory.LoadSettingsAsync<AmazonPaySettings>(processPaymentRequest.StoreId);

                        result.NewPaymentStatus = settings.TransactionType == AmazonPayTransactionType.AuthorizeAndCapture
                            ? PaymentStatus.Paid
                            : PaymentStatus.Authorized;
                    }
                    else
                    {
                        // 202 (Accepted): authorization is pending.
                        httpContext.Session.TrySetObject(AmazonPayCompletedInfo.Key, new AmazonPayCompletedInfo
                        {
                            Note = T("Plugins.Payments.AmazonPay.AsyncPaymentAuthorizationNote"),
                            UseWidget = !_orderSettings.DisableOrderCompletedPage
                        });
                    }
                }
                else
                {
                    // 4xx/5xx: authorization failed.
                    // Canceled by buyer or by AmazonPay, declined or expired.
                    var reason = response?.StatusDetails?.ReasonCode;

                    if (reason.EqualsNoCase("AmazonRejected"))
                    {
                        result.Errors.Add(T("Plugins.Payments.AmazonPay.AuthorizationSoftDeclineMessage"));
                    }
                    else if (reason.EqualsNoCase("HardDeclined"))
                    {
                        result.Errors.Add(T("Plugins.Payments.AmazonPay.AuthorizationHardDeclineMessage"));
                    }
                    else
                    {
                        result.Errors.Add(T("Plugins.Payments.AmazonPay.AuthenticationStatusFailureMessage"));
                    }

                    // Redirect the buyer to the start of checkout.
                    result.RedirectUrl = _urlHelper.Action("Cart", "ShoppingCart", new { area = string.Empty });

                    Logger.LogAmazonPayFailure(request, response);
                }
            }
            catch (Exception ex)
            {
                // Avoid infinite loop where the confirm-form is automatically submitted over and over again.
                state.SubmitForm = false;
                _checkoutStateAccessor.SetAmazonPayCheckoutState(state);
                throw new AmazonPayException(ex.Message, ex);
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
                    Logger.LogAmazonPayFailure(request, response);
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

                result.CaptureTransactionResult = state.Grow(response.StatusDetails.ReasonCode, " ");

                if (state.EqualsNoCase("Captured"))
                {
                    result.NewPaymentStatus = PaymentStatus.Paid;
                }
            }
            else
            {
                var message = Logger.LogAmazonPayFailure(request, response);
                throw new AmazonPayException(message, response);
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
                    order.GenericAttributes.Set(SystemName + ".RefundId", response.RefundId, order.StoreId);
                    await _db.SaveChangesAsync();
                }
            }
            else
            {
                var message = Logger.LogAmazonPayFailure(request, response);
                throw new AmazonPayException(message, response);
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
                    var message = Logger.LogAmazonPayFailure(request, response);
                    throw new AmazonPayException(message, response);
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

    public class AmazonPayActionState
    {
        public Guid OrderGuid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
