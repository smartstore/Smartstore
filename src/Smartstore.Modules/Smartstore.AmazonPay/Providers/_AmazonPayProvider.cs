using System.Linq;
using AmazonPay;
using AmazonPay.Responses;
using AmazonPay.StandardPaymentRequests;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.AmazonPay.Services;
using Smartstore.Core;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Data;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.Threading;

namespace Smartstore.AmazonPay.Providers
{
    [SystemName("Smartstore.AmazonPay")]
    [FriendlyName("Amazon Pay")]
    [DependentWidgets("Widgets.AmazonPay")]
    [Order(-1)]
    public class AmazonPayProvider : PaymentMethodBase, IConfigurable
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IAmazonPayService _amazonPayService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUrlHelper _urlHelper;
        private readonly AsyncRunner _asyncRunner;

        public AmazonPayProvider(
            SmartDbContext db,
            ICommonServices services,
            IAmazonPayService amazonPayService,
            IHttpContextAccessor httpContextAccessor,
            IUrlHelper urlHelper,
            AsyncRunner asyncRunner)
        {
            _db = db;
            _services = services;
            _amazonPayService = amazonPayService;
            _httpContextAccessor = httpContextAccessor;
            _urlHelper = urlHelper;
            _asyncRunner = asyncRunner;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        // INFO: provider and module system name are equal because the payment method
        // was developed at a time when there were no payment providers yet.
        public static string SystemName => "Smartstore.AmazonPay";

        public override bool SupportCapture => true;

        public override bool SupportPartiallyRefund => true;

        public override bool SupportRefund => true;

        public override bool SupportVoid => true;

        public override PaymentMethodType PaymentMethodType => PaymentMethodType.StandardAndButton;

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "AmazonPayAdmin", new { area = "Admin" });

        public override WidgetInvoker GetPaymentInfoWidget()
        {
            throw new NotImplementedException();
        }

        public override async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult
            {
                NewPaymentStatus = PaymentStatus.Pending
            };

            var orderNoteErrors = new List<string>();
            var informCustomerAboutErrors = false;
            var informCustomerAddErrors = false;
            var isSynchronous = false;
            string error = null;

            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    httpContext.Session.TryRemove("AmazonPayFailedPaymentReason");
                    httpContext.Session.TryRemove("AmazonPayCheckoutCompletedNote");
                }

                var settings = await _services.SettingFactory.LoadSettingsAsync<AmazonPaySettings>(processPaymentRequest.StoreId);
                var captureNow = settings.TransactionType == AmazonPayTransactionType.AuthorizeAndCapture;
                var state = _amazonPayService.GetCheckoutState();
                var client = _amazonPayService.CreateApiClient(settings);
                AuthorizeRequest authRequest = null;
                AuthorizeResponse authResponse = null;

                informCustomerAboutErrors = settings.InformCustomerAboutErrors;
                informCustomerAddErrors = settings.InformCustomerAddErrors;

                // Authorize.
                if (settings.AuthorizeMethod == AmazonPayAuthorizeMethod.Omnichronous)
                {
                    // First try synchronously.
                    (authRequest, authResponse) = AuthorizePayment(settings, state, processPaymentRequest, client, true);

                    if (authResponse.GetAuthorizationState().EqualsNoCase("Declined") &&
                        authResponse.GetReasonCode().EqualsNoCase("TransactionTimedOut"))
                    {
                        // Second try asynchronously.
                        // Transaction is always in pending state after return.
                        (authRequest, authResponse) = AuthorizePayment(settings, state, processPaymentRequest, client, false);
                    }
                    else
                    {
                        isSynchronous = true;
                    }
                }
                else
                {
                    isSynchronous = settings.AuthorizeMethod == AmazonPayAuthorizeMethod.Synchronous;
                    (authRequest, authResponse) = AuthorizePayment(settings, state, processPaymentRequest, client, isSynchronous);
                }

                // Process authorization response.
                if (authResponse.GetSuccess())
                {
                    var reason = authResponse.GetReasonCode();

                    result.AuthorizationTransactionId = authResponse.GetAuthorizationId();
                    result.AuthorizationTransactionCode = authResponse.GetAuthorizationReferenceId();
                    result.AuthorizationTransactionResult = authResponse.GetAuthorizationState();

                    if (captureNow)
                    {
                        var idList = authResponse.GetCaptureIdList();
                        if (idList.Any())
                        {
                            result.CaptureTransactionId = idList.First();
                        }
                    }

                    if (isSynchronous)
                    {
                        if (result.AuthorizationTransactionResult.EqualsNoCase("Open"))
                        {
                            result.NewPaymentStatus = PaymentStatus.Authorized;
                        }
                        else if (result.AuthorizationTransactionResult.EqualsNoCase("Closed"))
                        {
                            if (captureNow && reason.EqualsNoCase("MaxCapturesProcessed"))
                            {
                                result.NewPaymentStatus = PaymentStatus.Paid;
                            }
                        }
                    }
                    else
                    {
                        httpContext.Session.SetString("AmazonPayCheckoutCompletedNote", T("Plugins.Payments.AmazonPay.AsyncPaymentAuthrizationNote"));
                    }

                    if (reason.EqualsNoCase("InvalidPaymentMethod") || reason.EqualsNoCase("AmazonRejected") ||
                        reason.EqualsNoCase("ProcessingFailure") || reason.EqualsNoCase("TransactionTimedOut") ||
                        reason.EqualsNoCase("TransactionTimeout"))
                    {
                        error = authResponse.GetReasonDescription();
                        error = error.HasValue() ? $"{reason}: {error}" : reason;

                        if (reason.EqualsNoCase("AmazonRejected"))
                        {
                            // Must be logged out and redirected to shopping cart.
                            httpContext.Session.SetString("AmazonPayFailedPaymentReason", reason);

                            result.RedirectUrl = _urlHelper.Action("Cart", "ShoppingCart", new { area = string.Empty });
                        }
                        else if (reason.EqualsNoCase("InvalidPaymentMethod"))
                        {
                            // Must be redirected to checkout payment page.
                            httpContext.Session.SetString("AmazonPayFailedPaymentReason", reason);

                            // Review: confirmation required to get order reference object from suspended to open state again.
                            state.IsConfirmed = false;
                            state.FormData = null;

                            result.RedirectUrl = _urlHelper.Action("PaymentMethod", "Checkout", new { area = string.Empty });
                        }
                    }
                }
                else
                {
                    error = Logger.LogAmazonResponse(authRequest, authResponse);
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
            if (informCustomerAboutErrors && orderNoteErrors.Any())
            {
                var ctx = new AmazonPayActionState { OrderGuid = processPaymentRequest.OrderGuid };
                if (informCustomerAddErrors)
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

        public override async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            // Early polling... note we do not have the amazon billing address yet.
            //try
            //{
            //	int orderId = order.Id;
            //	var settings = _services.Settings.LoadSetting<AmazonPaySettings>(order.StoreId);

            //	if (orderId != 0 && settings.StatusFetching == AmazonPayStatusFetchingType.Polling)
            //	{
            //		AsyncRunner.Run((container, obj) =>
            //		{
            //			var amazonService = container.Resolve<IAmazonPayService>();
            //			amazonService.EarlyPolling(orderId, obj as AmazonPaySettings);
            //		},
            //		settings, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            //	}
            //}
            //catch (Exception ex)
            //{
            //	Logger.Error(ex);
            //}

            try
            {
                var order = postProcessPaymentRequest.Order;
                var state = _amazonPayService.GetCheckoutState();

                var orderReference = new AmazonPayOrderReference
                {
                    OrderReferenceId = state.OrderReferenceId
                };

                if (order.PaymentStatus == PaymentStatus.Paid)
                {
                    var settings = await _services.SettingFactory.LoadSettingsAsync<AmazonPaySettings>(order.StoreId);
                    var client = _amazonPayService.CreateApiClient(settings);

                    var closeRequest = new CloseOrderReferenceRequest()
                        .WithMerchantId(settings.SellerId)
                        .WithAmazonOrderReferenceId(orderReference.OrderReferenceId);

                    var closeResponse = client.CloseOrderReference(closeRequest);
                    if (closeResponse.GetSuccess())
                    {
                        orderReference.OrderReferenceClosed = true;
                    }
                    else
                    {
                        Logger.LogAmazonResponse(closeRequest, closeResponse, LogLevel.Warning);
                    }
                }

                order.SetAmazonPayOrderReference(orderReference);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public override async Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            var order = capturePaymentRequest.Order;
            var result = new CapturePaymentResult
            {
                NewPaymentStatus = order.PaymentStatus
            };

            try
            {
                var settings = await _services.SettingFactory.LoadSettingsAsync<AmazonPaySettings>(order.StoreId);
                var client = _amazonPayService.CreateApiClient(settings);

                var captureRequest = new CaptureRequest()
                    .WithMerchantId(settings.SellerId)
                    .WithAmazonAuthorizationId(order.AuthorizationTransactionId)
                    .WithCaptureReferenceId(AmazonPayService.GenerateRandomId("Capture"))
                    .WithCurrencyCode(_amazonPayService.GetAmazonCurrencyCode())
                    .WithAmount(order.OrderTotal);

                var captureResponse = client.Capture(captureRequest);

                if (captureResponse.GetSuccess())
                {
                    var state = captureResponse.GetCaptureState();

                    result.CaptureTransactionId = captureResponse.GetCaptureId();
                    result.CaptureTransactionResult = state.Grow(captureResponse.GetReasonCode(), " ");

                    if (state.EqualsNoCase("completed"))
                    {
                        result.NewPaymentStatus = PaymentStatus.Paid;
                    }
                }
                else
                {
                    var message = Logger.LogAmazonResponse(captureRequest, captureResponse);
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

        public override async Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            var order = refundPaymentRequest.Order;
            var result = new RefundPaymentResult
            {
                NewPaymentStatus = order.PaymentStatus
            };

            try
            {
                var settings = await _services.SettingFactory.LoadSettingsAsync<AmazonPaySettings>(order.StoreId);
                var client = _amazonPayService.CreateApiClient(settings);

                var refundRequest = new RefundRequest()
                    .WithMerchantId(settings.SellerId)
                    .WithAmazonCaptureId(order.CaptureTransactionId)
                    .WithRefundReferenceId(AmazonPayService.GenerateRandomId("Refund"))
                    .WithCurrencyCode(_amazonPayService.GetAmazonCurrencyCode())
                    .WithAmount(refundPaymentRequest.AmountToRefund.Amount);

                var refundResponse = client.Refund(refundRequest);

                if (refundResponse.GetSuccess())
                {
                    result.NewPaymentStatus = refundPaymentRequest.IsPartialRefund ? PaymentStatus.PartiallyRefunded : PaymentStatus.Refunded;

                    var refundId = refundResponse.GetAmazonRefundId();
                    if (refundId.HasValue() && refundPaymentRequest.Order.Id != 0)
                    {
                        order.GenericAttributes.Set<string>(SystemName + ".RefundId", refundId, order.StoreId);

                        await _db.SaveChangesAsync();
                    }
                }
                else
                {
                    var message = Logger.LogAmazonResponse(refundRequest, refundResponse);
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

        public override async Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            var order = voidPaymentRequest.Order;
            var result = new VoidPaymentResult
            {
                NewPaymentStatus = order.PaymentStatus
            };

            // redundant... cause payment infrastructure hides "void" and displays "refund" instead.
            //if (order.PaymentStatus == PaymentStatus.Paid)
            //{
            //	var refundRequest = new RefundPaymentRequest()
            //	{
            //		Order = order,
            //		IsPartialRefund = false,
            //		AmountToRefund = order.OrderTotal
            //	};

            //	var refundResult = Refund(refundRequest);

            //	result.Errors.AddRange(refundResult.Errors);
            //	return result;
            //}

            try
            {
                if (order.PaymentStatus == PaymentStatus.Pending || order.PaymentStatus == PaymentStatus.Authorized)
                {
                    var settings = await _services.SettingFactory.LoadSettingsAsync<AmazonPaySettings>(order.StoreId);
                    var orderReference = order.GetAmazonPayOrderReference();
                    var client = _amazonPayService.CreateApiClient(settings);

                    var cancelRequest = new CancelOrderReferenceRequest()
                        .WithMerchantId(settings.SellerId)
                        .WithAmazonOrderReferenceId(orderReference.OrderReferenceId);

                    var cancelResponse = client.CancelOrderReference(cancelRequest);
                    if (cancelResponse.GetSuccess())
                    {
                        result.NewPaymentStatus = PaymentStatus.Voided;
                    }
                    else
                    {
                        var message = Logger.LogAmazonResponse(cancelRequest, cancelResponse);
                        result.Errors.Add(message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                result.Errors.Add(ex.Message);
            }

            return result;
        }

        public override async Task<(decimal FixedFeeOrPercentage, bool UsePercentage)> GetPaymentFeeInfoAsync(ShoppingCart cart)
        {
            var settings = await _services.SettingFactory.LoadSettingsAsync<AmazonPaySettings>(_services.StoreContext.CurrentStore.Id);

            return (settings.AdditionalFee, settings.AdditionalFeePercentage);
        }

        #region Utilities

        private (AuthorizeRequest Request, AuthorizeResponse Response) AuthorizePayment(
            AmazonPaySettings settings,
            AmazonPayCheckoutState state,
            ProcessPaymentRequest request,
            Client client,
            bool synchronously)
        {
            var authRequest = new AuthorizeRequest()
                .WithMerchantId(settings.SellerId)
                .WithAmazonOrderReferenceId(state.OrderReferenceId)
                .WithAuthorizationReferenceId(AmazonPayService.GenerateRandomId("Authorize"))
                .WithCaptureNow(settings.TransactionType == AmazonPayTransactionType.AuthorizeAndCapture)
                .WithCurrencyCode(_amazonPayService.GetAmazonCurrencyCode())
                .WithAmount(request.OrderTotal.Amount);

            if (synchronously)
            {
                authRequest = authRequest.WithTransactionTimeout(0);
            }

            // See https://pay.amazon.com/de/developer/documentation/lpwa/201956480
            //{"SandboxSimulation": {"State":"Declined", "ReasonCode":"InvalidPaymentMethod", "PaymentMethodUpdateTimeInMins":5}}
            //{"SandboxSimulation": {"State":"Declined", "ReasonCode":"AmazonRejected"}}
            //if (settings.UseSandbox)
            //{
            //	var authNote = _services.Settings.GetSettingByKey<string>("SmartStore.AmazonPay.SellerAuthorizationNote");
            //  authRequest = authRequest.WithSellerAuthorizationNote(authNote);
            //}

            var authResponse = client.Authorize(authRequest);

            return (authRequest, authResponse);
        }

        #endregion
    }

    public class AmazonPayActionState
    {
        public Guid OrderGuid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
