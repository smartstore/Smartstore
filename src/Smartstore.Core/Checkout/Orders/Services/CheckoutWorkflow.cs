using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Cart.Events;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;
using Smartstore.Events;
using Smartstore.Utilities.Html;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class CheckoutWorkflow : ICheckoutWorkflow
    {
        const int _maxWarnings = 3;

        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;
        private readonly INotifier _notifier;
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly IEventPublisher _eventPublisher;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IPaymentService _paymentService;
        private readonly ICheckoutFactory _checkoutFactory;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly OrderSettings _orderSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public CheckoutWorkflow(
            SmartDbContext db,
            IStoreContext storeContext,
            INotifier notifier,
            ILogger logger,
            IWebHelper webHelper,
            IEventPublisher eventPublisher,
            IShoppingCartValidator shoppingCartValidator,
            IOrderProcessingService orderProcessingService,
            IPaymentService paymentService,
            ICheckoutFactory checkoutFactory,
            ICheckoutStateAccessor checkoutStateAccessor,
            OrderSettings orderSettings,
            ShoppingCartSettings shoppingCartSettings)
        {
            _db = db;
            _storeContext = storeContext;
            _notifier = notifier;
            _logger = logger;
            _webHelper = webHelper;
            _eventPublisher = eventPublisher;
            _shoppingCartValidator = shoppingCartValidator;
            _orderProcessingService = orderProcessingService;
            _paymentService = paymentService;
            _checkoutFactory = checkoutFactory;
            _checkoutStateAccessor = checkoutStateAccessor;
            _orderSettings = orderSettings;
            _shoppingCartSettings = shoppingCartSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public virtual async Task<CheckoutResult> StartAsync(CheckoutContext context)
        {
            Guard.NotNull(context);

            var warnings = new List<string>();
            var cart = context.Cart;

            var preliminaryResult = Preliminary(context);
            if (preliminaryResult != null)
            {
                return new(preliminaryResult);
            }

            cart.Customer.ResetCheckoutData(cart.StoreId);
            _checkoutStateAccessor.Abandon();

            if (!cart.Requirements.HasFlag(CheckoutRequirements.BillingAddress))
            {
                cart.Customer.BillingAddress = null;
            }
            if (!cart.IsShippingRequired)
            {
                cart.Customer.ShippingAddress = null;
            }

            if (await _shoppingCartValidator.ValidateCartAsync(cart, warnings, true))
            {
                var validatingCartEvent = new ValidatingCartEvent(cart, warnings);
                await _eventPublisher.PublishAsync(validatingCartEvent);

                if (validatingCartEvent.Result != null)
                {
                    return new(validatingCartEvent.Result);
                }

                // Validate each shopping cart item.
                foreach (var item in cart.Items)
                {
                    if (warnings.Count > 0)
                    {
                        break;
                    }

                    var addToCartContext = new AddToCartContext
                    {
                        StoreId = cart.StoreId,
                        Product = item.Item.Product,
                        BundleItem = item.Item.BundleItem,
                        ChildItems = item.ChildItems.Select(x => x.Item).ToList()
                    };

                    if (!await _shoppingCartValidator.ValidateAddToCartItemAsync(addToCartContext, item.Item, cart.Items))
                    {
                        warnings.AddRange(addToCartContext.Warnings);
                    }
                }
            }

            await _db.SaveChangesAsync();

            if (warnings.Count > 0)
            {
                warnings.Take(_maxWarnings).Each(x => _notifier.Warning(x));
                return new(RedirectToCart());
            }

            return await AdvanceAsync(context);
        }

        public virtual async Task<CheckoutResult> ProcessAsync(CheckoutContext context)
        {
            Guard.NotNull(context);

            var preliminaryResult = Preliminary(context);
            if (preliminaryResult != null)
            {
                return new(preliminaryResult);
            }

            // Get and process the current handler (based on the request's route values).
            var step = _checkoutFactory.GetCheckoutStep(context);
            if (step == null)
            {
                return new(false);
            }

            var result = await ProcessInternal(step, context);
            if (result.SkipPage)
            {
                // Current checkout page should be skipped. For example there is only one shipping method
                // and the customer has nothing to select on the associated page.
                if (result.ActionResult != null)
                {
                    return result;
                }

                var adjacentStep = Adjacent(step, context);
                if (adjacentStep != null)
                {
                    return new(adjacentStep.GetActionResult(context), adjacentStep.ViewPath);
                }

                return new(RedirectToCart());
            }

            // No redirect (default). Opening the current checkout page is fine.
            return new(result.Errors, result.ViewPath, true);
        }

        public virtual async Task<CheckoutResult> AdvanceAsync(CheckoutContext context)
        {
            Guard.NotNull(context);

            var preliminaryResult = Preliminary(context);
            if (preliminaryResult != null)
            {
                return new(preliminaryResult);
            }

            var steps = _checkoutFactory.GetCheckoutSteps();
            if (steps.Length == 0)
            {
                throw new InvalidOperationException($"No checkout handlers of type {nameof(ICheckoutHandler)} found.");
            }

            if (_shoppingCartSettings.QuickCheckoutEnabled)
            {
                // Process all steps in sequence.
                foreach (var step in steps)
                {
                    var result = await ProcessInternal(step, context);
                    if (!result.Success)
                    {
                        // Redirect to the checkout page associated with the "unsuccessful" step.
                        return result;
                    }
                }
            }
            else
            {
                if (context.IsCurrentRoute(null, "Index"))
                {
                    return new(steps[0].GetActionResult(context), steps[0].ViewPath);
                }

                // Get and process current handler (based on the request's route values).
                var step = _checkoutFactory.GetCheckoutStep(context);
                if (step != null)
                {
                    var result = await ProcessInternal(step, context);
                    if (!result.Success)
                    {
                        // Redirect to the checkout page associated with the "unsuccessful" step.
                        return result;
                    }

                    // Redirect to the checkout page associated with the next step.
                    var nextStep = _checkoutFactory.GetNextCheckoutStep(step, true);
                    if (nextStep != null)
                    {
                        return new(nextStep.GetActionResult(context), nextStep.ViewPath);
                    }
                }
            }

            // A redirect target cannot be determined.
            return new(false);
        }

        public virtual async Task<CheckoutResult> CompleteAsync(CheckoutContext context)
        {
            Guard.NotNull(context);

            var confirmStep = Guard.NotNull(_checkoutFactory.GetCheckoutStep(CheckoutActionNames.Confirm));

            var warnings = new List<string>();
            var store = _storeContext.CurrentStore;
            var cart = context.Cart;
            OrderPlacementResult placeOrderResult = null;

            var validatingCartEvent = new ValidatingCartEvent(cart, warnings);
            await _eventPublisher.PublishAsync(validatingCartEvent);

            if (validatingCartEvent.Result != null)
            {
                return new(validatingCartEvent.Result);
            }

            if (warnings.Count > 0)
            {
                warnings.Take(_maxWarnings).Each(x => _notifier.Warning(x));
                return new(RedirectToCart());
            }

            // Prevent two orders from being placed within a time span of x seconds.
            if (!await _orderProcessingService.IsMinimumOrderPlacementIntervalValidAsync(cart.Customer, store))
            {
                _notifier.Warning(T("Checkout.MinOrderPlacementInterval"));
                return new(confirmStep.GetActionResult(context), confirmStep.ViewPath);
            }

            try
            {
                context.HttpContext.Session.TryGetObject<ProcessPaymentRequest>(CheckoutState.OrderPaymentInfoName, out var paymentRequest);
                paymentRequest ??= new();
                paymentRequest.StoreId = store.Id;
                paymentRequest.CustomerId = cart.Customer.Id;
                paymentRequest.PaymentMethodSystemName = cart.Customer.GenericAttributes.SelectedPaymentMethod;

                var placeOrderExtraData = new Dictionary<string, string>
                {
                    ["CustomerComment"] = context.HttpContext.Request.Form["customercommenthidden"].ToString(),
                    ["SubscribeToNewsletter"] = context.HttpContext.Request.Form["SubscribeToNewsletter"].ToString(),
                    ["AcceptThirdPartyEmailHandOver"] = context.HttpContext.Request.Form["AcceptThirdPartyEmailHandOver"].ToString()
                };

                placeOrderResult = await _orderProcessingService.PlaceOrderAsync(paymentRequest, placeOrderExtraData);
            }
            catch (PaymentException ex)
            {
                return PaymentFailure(ex);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);

                return new([new(string.Empty, ex.Message)], confirmStep.ViewPath);
            }

            if (placeOrderResult == null || !placeOrderResult.Success)
            {
                var errors = placeOrderResult?.Errors
                    ?.Take(_maxWarnings)
                    ?.Select(x => new CheckoutError(string.Empty, HtmlUtility.ConvertPlainTextToHtml(x)))
                    ?.ToArray();

                return new(errors, confirmStep.ViewPath);
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
                return PaymentFailure(ex);
            }
            catch (Exception ex)
            {
                _notifier.Error(ex.Message);
            }
            finally
            {
                context.HttpContext.Session.TrySetObject<ProcessPaymentRequest>(CheckoutState.OrderPaymentInfoName, null);
                _checkoutStateAccessor.Abandon();
            }

            if (postPaymentRequest.RedirectUrl.HasValue())
            {
                return new(new RedirectResult(postPaymentRequest.RedirectUrl));
            }

            return new(RedirectToCheckout(CheckoutActionNames.Completed));

            CheckoutResult PaymentFailure(PaymentException ex)
            {
                _logger.Error(ex);
                _notifier.Error(ex.Message);

                if (ex.RedirectRoute != null)
                {
                    return new(new RedirectToActionResult(ex.RedirectRoute.Action, ex.RedirectRoute.Controller, ex.RedirectRoute.RouteValues));
                }

                var paymentStep = _checkoutFactory.GetCheckoutStep(CheckoutActionNames.PaymentMethod);

                return new(paymentStep.GetActionResult(context), paymentStep.ViewPath);
            }
        }

        /// <summary>
        /// Executes the handler associated with <paramref name="step"/> and fully prepares <see cref="CheckoutResult"/>.
        /// </summary>
        private static async Task<CheckoutResult> ProcessInternal(CheckoutStep step, CheckoutContext context)
        {
            var result = await step.Handler.Value.ProcessAsync(context);
            result.ViewPath = step.ViewPath;

            if (!result.Success)
            {
                // Redirect to the page associated with this step.
                result.ActionResult ??= step.GetActionResult(context);
            }

            return result;
        }

        /// <summary>
        /// Checks whether the checkout can be executed, e.g. whether the shopping cart has items.
        /// </summary>
        private IActionResult Preliminary(CheckoutContext context)
        {
            if (context.HttpContext?.Request == null)
            {
                throw new InvalidOperationException("The checkout workflow is only applicable in the context of a HTTP request.");
            }

            if (!_orderSettings.AnonymousCheckoutAllowed && !context.Cart.Customer.IsRegistered())
            {
                return new ChallengeResult();
            }

            if (!context.Cart.HasItems)
            {
                return RedirectToCart();
            }

            return null;
        }

        /// <summary>
        /// Special case when the checkout page associated with <paramref name="step"/> must always be skipped
        /// (e.g. if the store only offers a single shipping method).
        /// In this case, based on the referrer, the customer must be redirected to the next or previous page,
        /// depending on the direction from which the customer accessed the current page.
        /// </summary>
        private CheckoutStep Adjacent(CheckoutStep step, CheckoutContext context)
        {
            // Get route values of the URL referrer.
            var referrer = _webHelper.GetUrlReferrer();
            var path = referrer?.PathAndQuery;
            var routeValues = new RouteValueDictionary();

            if (path.HasValue())
            {
                var values = new RouteValueDictionary();
                var template = TemplateParser.Parse("{controller}/{action}/{id?}");
                var matcher = new TemplateMatcher(template, []);
                matcher.TryMatch(path, routeValues);
            }

            var next = true;
            var action = routeValues.GetActionName();
            var controller = routeValues.GetControllerName();

            if (action.HasValue() && controller.HasValue())
            {
                if (action.EqualsNoCase("Index") && controller.EqualsNoCase("Checkout"))
                {
                    // Referrer is the checkout index page -> return the next handler (billing address).
                    next = true;
                }
                else if (action.EqualsNoCase(CheckoutActionNames.Confirm) && controller.EqualsNoCase("Checkout"))
                {
                    // Referrer is the confirm page -> return the previous handler (payment selection).
                    next = false;
                }
                else
                {
                    // Referrer is any step in checkout -> return the next handler if the referrer's order number
                    // is less than that of the current handler. Otherwise return previous handler.
                    var referrerStep = _checkoutFactory.GetCheckoutStep(action, controller, routeValues.GetAreaName());

                    next = (referrerStep?.Handler?.Metadata?.Order ?? 0) < step.Handler.Metadata.Order;
                }
            }

            var result = _checkoutFactory.GetNextCheckoutStep(step, next);
            result ??= next ? _checkoutFactory.GetCheckoutStep(CheckoutActionNames.Confirm) : null;

            return result;
        }

        private static RedirectToActionResult RedirectToCheckout(string action)
            => new(action, "Checkout", null);

        // INFO: do not use RedirectToRouteResult here. It would create an infinite redirection loop.
        // In CheckoutWorkflow always use RedirectToActionResult with controller and action name.
        private static RedirectToActionResult RedirectToCart()
            => new("Cart", "ShoppingCart", null);
    }
}
