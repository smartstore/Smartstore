using System.Linq;
using Amazon.Pay.API.WebStore.Charge;
using Amazon.Pay.API.WebStore.ChargePermission;
using Amazon.Pay.API.WebStore.CheckoutSession;
using Amazon.Pay.API.WebStore.Interfaces;
using Amazon.Pay.API.WebStore.Refund;
using Autofac;
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
using Smartstore.Core.Data;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.Threading;

namespace Smartstore.AmazonPay.Providers
{
    [SystemName("Payments.AmazonPay")]
    [FriendlyName("Amazon Pay")]
    [Order(-1)] // AmazonPay review.
    public class AmazonPayProvider : PaymentMethodBase, IConfigurable
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IWebStoreClient _apiClient;
        private readonly IAmazonPayService _amazonPayService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IUrlHelper _urlHelper;
        private readonly AsyncRunner _asyncRunner;
        private readonly OrderSettings _orderSettings;

        public AmazonPayProvider(
            SmartDbContext db,
            ICommonServices services,
            IWebStoreClient apiClient,
            IAmazonPayService amazonPayService,
            IHttpContextAccessor httpContextAccessor,
            ICheckoutStateAccessor checkoutStateAccessor,
            IUrlHelper urlHelper,
            AsyncRunner asyncRunner,
            OrderSettings orderSettings)
        {
            _db = db;
            _services = services;
            _apiClient = apiClient;
            _amazonPayService = amazonPayService;
            _httpContextAccessor = httpContextAccessor;
            _checkoutStateAccessor = checkoutStateAccessor;
            _urlHelper = urlHelper;
            _asyncRunner = asyncRunner;
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
            => new ComponentWidgetInvoker(typeof(AmazonPayButtonViewComponent), new { providerName = nameof(AmazonPayProvider) });

        public override async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult
            {
                NewPaymentStatus = PaymentStatus.Pending
            };

            var settings = await _services.SettingFactory.LoadSettingsAsync<AmazonPaySettings>(processPaymentRequest.StoreId);
            var orderNoteErrors = new List<string>();
            var isSynchronous = true;
            string error = null;

            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    httpContext.Session.TryRemove("AmazonPayFailedPaymentReason");
                    httpContext.Session.TryRemove(AmazonPayCheckoutCompleteInfo.Key);
                }

                if (_checkoutStateAccessor.CheckoutState?.CustomProperties?.Get(AmazonPayCheckoutState.Key) is not AmazonPayCheckoutState state
                    || state.CheckoutSessionId.IsEmpty())
                {
                    throw new SmartException(T("Plugins.Payments.AmazonPay.MissingCheckoutSessionState"));
                }

                var captureNow = settings.TransactionType == AmazonPayTransactionType.AuthorizeAndCapture;

                var request = new CompleteCheckoutSessionRequest(processPaymentRequest.OrderTotal, _amazonPayService.GetAmazonPayCurrency());
                var response = _apiClient.CompleteCheckoutSession(state.CheckoutSessionId, request);

                // TODO: (mg) (core) test and debug. State\reason and further processing is unclear here. Do I have to call GetCharge?
                response.RawResponse.Dump();

                if (response.Success)
                {
                    // 202 (Accepted): authorization is pending.
                    // 200 (OK): authorization succeeded.
                    isSynchronous = response.Status != 202;

                    // A Charge represents a single payment transaction.
                    // Can either be created using a valid Charge Permission, or as a result of a successful Checkout Session.
                    result.AuthorizationTransactionId = response.ChargeId;

                    // A Charge Permission represents buyer consent to be charged.
                    // Can either be requested for a one-time or recurring payment scenario.
                    result.AuthorizationTransactionCode = response.ChargePermissionId;

                    result.AuthorizationTransactionResult = response.StatusDetails.State;

                    if (isSynchronous)
                    {
                        // TODO: AuthorizationTransactionResult = Open vs. Closed
                        result.NewPaymentStatus = captureNow ? PaymentStatus.Paid : PaymentStatus.Authorized;
                    }
                    else
                    {
                        httpContext.Session.TrySetObject(AmazonPayCheckoutCompleteInfo.Key, new AmazonPayCheckoutCompleteInfo
                        {
                            Note = T("Plugins.Payments.AmazonPay.AsyncPaymentAuthorizationNote"),
                            UseWidget = !_orderSettings.DisableOrderCompletedPage
                        });
                    }

                    // TODO: (mg) (core) state\reason is unclear here.
                    var reason = response.StatusDetails.ReasonCode;

                    //    if (reason.EqualsNoCase("InvalidPaymentMethod") || reason.EqualsNoCase("AmazonRejected") ||
                    //        reason.EqualsNoCase("ProcessingFailure") || reason.EqualsNoCase("TransactionTimedOut") ||
                    //        reason.EqualsNoCase("TransactionTimeout"))
                    //    {
                    //        error = authResponse.GetReasonDescription();
                    //        error = error.HasValue() ? $"{reason}: {error}" : reason;

                    //        if (reason.EqualsNoCase("AmazonRejected"))
                    //        {
                    //            // Must be logged out and redirected to shopping cart.
                    //            httpContext.Session.SetString("AmazonPayFailedPaymentReason", reason);

                    //            result.RedirectUrl = _urlHelper.Action("Cart", "ShoppingCart", new { area = string.Empty });
                    //        }
                    //        else if (reason.EqualsNoCase("InvalidPaymentMethod")) // PaymentMethodNotAllowed?
                    //        {
                    //            // Must be redirected to checkout payment page.
                    //            httpContext.Session.SetString("AmazonPayFailedPaymentReason", reason);

                    //            // Review: confirmation required to get order reference object from suspended to open state again.
                    //            state.IsConfirmed = false;
                    //            state.FormData = null;

                    //            result.RedirectUrl = _urlHelper.Action("PaymentMethod", "Checkout", new { area = string.Empty });
                    //        }
                    //    }
                }
                else
                {
                    // 4xx/5xx: authorization failed.
                    error = Logger.LogAmazonPayFailure(request, response);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                error = ex.Message;
            }

            if (error.HasValue())
            {
                if (isSynchronous)
                {
                    result.Errors.Add(error);
                }
                else
                {
                    orderNoteErrors.Add(error);
                }
            }

            // Customer needs to be informed of an Amazon error here. Hooking OrderPlaced.CustomerNotification won't work
            // cause of asynchronous processing. Solution: we add a customer order note that is also send as an email.
            if (settings.InformCustomerAboutErrors && orderNoteErrors.Any())
            {
                var ctx = new AmazonPayActionState { OrderGuid = processPaymentRequest.OrderGuid };
                if (settings.InformCustomerAddErrors)
                {
                    ctx.Errors.AddRange(orderNoteErrors);
                }

                _ = _asyncRunner.Run(async (scope, ct, state) =>
                {
                    var aps = scope.Resolve<IAmazonPayService>();
                    await aps.AddCustomerOrderNoteLoopAsync(state as AmazonPayActionState, ct);
                },
                ctx);
            }

            return result;
        }

        public override Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var order = postProcessPaymentRequest.Order;

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                try
                {
                    var request = new CloseChargePermissionRequest(T("Plugins.Payments.AmazonPay.CloseChargeReason").Value.Truncate(255))
                    {
                        CancelPendingCharges = false
                    };

                    var response = _apiClient.CloseChargePermission(order.AuthorizationTransactionCode, request);

                    if (!response.Success)
                    {
                        Logger.LogAmazonPayFailure(request, response);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
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

            try
            {
                var request = new CaptureChargeRequest(order.OrderTotal, _amazonPayService.GetAmazonPayCurrency());
                var response = _apiClient.CaptureCharge(order.AuthorizationTransactionId, request);

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
                    result.Errors.Add(message);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                result.Errors.Add(ex.Message);
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

            try
            {
                var request = new CreateRefundRequest(order.AuthorizationTransactionId, refundPaymentRequest.AmountToRefund.Amount, _amazonPayService.GetAmazonPayCurrency());
                var response = _apiClient.CreateRefund(request);

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
                    result.Errors.Add(message);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                result.Errors.Add(ex.Message);
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
                try
                {
                    var request = new CloseChargePermissionRequest(T("Plugins.Payments.AmazonPay.CloseChargeReason").Value.Truncate(255))
                    {
                        CancelPendingCharges = true
                    };

                    var response = _apiClient.CloseChargePermission(order.AuthorizationTransactionCode, request);

                    if (response.Success)
                    {
                        result.NewPaymentStatus = PaymentStatus.Voided;
                    }
                    else
                    {
                        var message = Logger.LogAmazonPayFailure(request, response);
                        result.Errors.Add(message);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    result.Errors.Add(ex.Message);
                }
            }

            return Task.FromResult(result);
        }

        public override async Task<(decimal FixedFeeOrPercentage, bool UsePercentage)> GetPaymentFeeInfoAsync(ShoppingCart cart)
        {
            var settings = await _services.SettingFactory.LoadSettingsAsync<AmazonPaySettings>(_services.StoreContext.CurrentStore.Id);

            return (settings.AdditionalFee, settings.AdditionalFeePercentage);
        }
    }

    public class AmazonPayActionState
    {
        public Guid OrderGuid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
