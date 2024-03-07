using System.Collections.Frozen;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Cart.Events;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Stores;
using Smartstore.Events;
using Smartstore.Utilities.Html;

namespace Smartstore.Core.Checkout.Orders.Handlers
{
    public class ConfirmHandler : CheckoutHandlerBase
    {
        const int _maxWarnings = 3;

        private static readonly FrozenSet<string> _actionNames = new[]
        {
            "Confirm",
            "ConfirmOrder"
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        private readonly IStoreContext _storeContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly INotifier _notifier;
        private readonly ILogger _logger;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IPaymentService _paymentService;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;

        public ConfirmHandler(
            IStoreContext storeContext,
            IEventPublisher eventPublisher,
            INotifier notifier,
            ILogger logger,
            IOrderProcessingService orderProcessingService,
            IPaymentService paymentService,
            ICheckoutStateAccessor checkoutStateAccessor,
            IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
            _storeContext = storeContext;
            _eventPublisher = eventPublisher;
            _notifier = notifier;
            _logger = logger;
            _orderProcessingService = orderProcessingService;
            _paymentService = paymentService;
            _checkoutStateAccessor = checkoutStateAccessor;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        protected override string ActionName => "Confirm";

        public override int Order => int.MaxValue;

        public override bool IsHandlerFor(string action, string controller)
            => _actionNames.Contains(action) && controller.EqualsNoCase(ControllerName);

        public override async Task<CheckoutHandlerResult> ProcessAsync(ShoppingCart cart, object model = null)
        {            
            if (!IsSameRoute(HttpMethods.Post, ActionName))
            {
                // Never ever skip confirm page!
                return new(false);
            }

            var warnings = new List<string>();
            var store = _storeContext.CurrentStore;

            var validatingCartEvent = new ValidatingCartEvent(cart, warnings);
            await _eventPublisher.PublishAsync(validatingCartEvent);

            if (validatingCartEvent.Result != null)
            {
                return new(validatingCartEvent.Result);
            }

            if (warnings.Count > 0)
            {
                warnings.Take(_maxWarnings).Each(x => _notifier.Warning(x));

                return new(CheckoutWorkflow.RedirectToCart());
            }

            // Prevent two orders from being placed within a time span of x seconds.
            if (!await _orderProcessingService.IsMinimumOrderPlacementIntervalValidAsync(cart.Customer, store))
            {
                _notifier.Warning(T("Checkout.MinOrderPlacementInterval"));

                return new(GetActionResult());
            }

            OrderPlacementResult placeOrderResult = null;

            try
            {
                HttpContext.Session.TryGetObject<ProcessPaymentRequest>(CheckoutState.OrderPaymentInfoName, out var paymentRequest);
                paymentRequest ??= new();
                paymentRequest.StoreId = store.Id;
                paymentRequest.CustomerId = cart.Customer.Id;
                paymentRequest.PaymentMethodSystemName = cart.Customer.GenericAttributes.SelectedPaymentMethod;

                var placeOrderExtraData = new Dictionary<string, string>
                {
                    ["CustomerComment"] = HttpContext.Request.Form["customercommenthidden"].ToString(),
                    ["SubscribeToNewsletter"] = HttpContext.Request.Form["SubscribeToNewsletter"].ToString(),
                    ["AcceptThirdPartyEmailHandOver"] = HttpContext.Request.Form["AcceptThirdPartyEmailHandOver"].ToString()
                };

                placeOrderResult = await _orderProcessingService.PlaceOrderAsync(paymentRequest, placeOrderExtraData);
            }
            catch (PaymentException ex)
            {
                return new(PaymentFailure(ex));
            }
            catch (Exception ex)
            {
                _logger.Error(ex);

                return new(false, [new(string.Empty, ex.Message)]);
            }

            if (placeOrderResult == null || !placeOrderResult.Success)
            {
                var errors = placeOrderResult?.Errors
                    ?.Take(_maxWarnings)
                    ?.Select(x => new CheckoutWorkflowError(string.Empty, HtmlUtility.ConvertPlainTextToHtml(x)))
                    ?.ToArray();

                return new(false, errors);
            }

            var postPaymentRequest = new PostProcessPaymentRequest
            {
                Order = placeOrderResult.PlacedOrder
            };

            try
            {
                await _paymentService.PostProcessPaymentAsync(postPaymentRequest);
            }
            catch (PaymentException ex)
            {
                return new(PaymentFailure(ex));
            }
            catch (Exception ex)
            {
                _notifier.Error(ex.Message);
            }
            finally
            {
                HttpContext.Session.TrySetObject<ProcessPaymentRequest>(CheckoutState.OrderPaymentInfoName, null);
                _checkoutStateAccessor.Abandon();
            }

            if (postPaymentRequest.RedirectUrl.HasValue())
            {
                return new(new RedirectResult(postPaymentRequest.RedirectUrl), true);
            }

            return new(true);

            RedirectToActionResult PaymentFailure(PaymentException ex)
            {
                _logger.Error(ex);
                _notifier.Error(ex.Message);

                if (ex.RedirectRoute != null)
                {
                    return new RedirectToActionResult(ex.RedirectRoute.Action, ex.RedirectRoute.Controller, ex.RedirectRoute.RouteValues);
                }

                return CheckoutWorkflow.RedirectToCheckout("PaymentMethod");
            }
        }
    }
}
