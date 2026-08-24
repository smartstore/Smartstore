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
using Smartstore.Http;
using Smartstore.Utilities.Html;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Smartstore.Core.Checkout.Orders;

public partial class CheckoutWorkflow : ICheckoutWorkflow
{
    const int _maxWarnings = 3;
    internal const string CustomerCommentKey = "CustomerComment";
    internal const string SubscribeToNewsletterKey = "SubscribeToNewsletter";
    internal const string AcceptThirdPartyEmailHandOverKey = "AcceptThirdPartyEmailHandOver";

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

        if (await _shoppingCartValidator.ValidateCartAsync(cart, warnings, true, true))
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

            var adjacentStep = Adjacent(step);
            if (adjacentStep != null)
            {
                return new(adjacentStep.GetActionResult(context), adjacentStep.ViewPath);
            }

            return new(RedirectToCart());
        }

        // No redirect (default). Opening the current checkout page is fine.
        return new(result.Errors, result.ViewPath, true);
    }

    public virtual async Task<CheckoutResult> RefreshAsync(CheckoutContext context)
    {
        Guard.NotNull(context);
        Guard.NotNull(context.Partial);

        throw new NotImplementedException();
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

    public virtual async Task<CheckoutResult> ConfirmPaymentAsync(CheckoutContext context)
    {
        Guard.NotNull(context);

        var confirmStep = Guard.NotNull(_checkoutFactory.GetCheckoutStep(CheckoutActionNames.Confirm));

        try
        {
            var store = _storeContext.CurrentStore;
            var customer = context.Cart.Customer;
            var paymentMethod = customer.GenericAttributes.SelectedPaymentMethod;

            var validationResult = await PublishValidatingCartEvent(context);
            if (validationResult != null)
            {
                return validationResult;
            }

            context.HttpContext.Session.TryGetObject<ProcessPaymentRequest>(CheckoutState.OrderPaymentInfoName, out var paymentRequest);
            paymentRequest ??= new();
            paymentRequest.StoreId = store.Id;
            paymentRequest.CustomerId = customer.Id;
            paymentRequest.PaymentMethodSystemName = paymentMethod;

            var provider = await _paymentService.LoadPaymentProviderBySystemNameAsync(paymentMethod);
            if (provider == null || !provider.Value.RequiresConfirmation)
            {
                return new(T("Payment.CouldNotLoadMethod"), confirmStep.ViewPath);
            }

            var (warnings, _) = await _orderProcessingService.ValidateOrderPlacementAsync(paymentRequest);
            if (warnings.Count > 0)
            {
                return new(warnings
                    ?.Take(_maxWarnings)
                    ?.Select(x => new CheckoutError(string.Empty, HtmlUtility.ConvertPlainTextToHtml(x)))
                    ?.ToArray());
            }

            if (!await _orderProcessingService.IsMinimumOrderPlacementIntervalValidAsync(customer, store))
            {
                return new(T("Checkout.MinOrderPlacementInterval"), confirmStep.ViewPath);
            }

            var url = await provider.Value.GetConfirmationUrlAsync(paymentRequest, context);
            if (url.IsEmpty())
            {
                return new(true);
            }

            customer.GenericAttributes.CheckoutOrderData = GetCheckoutOrderData(context);

            return new(new RedirectResult(url), confirmStep.ViewPath, true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex);

            // We expect a payment error here.
            var paymentStep = _checkoutFactory.GetCheckoutStep(CheckoutActionNames.PaymentMethod);
            var protocol = _webHelper.IsCurrentConnectionSecured() ? "https" : "http";

            return new(ex.Message)
            {
                ActionResult = new RedirectResult(paymentStep.GetUrl(context, protocol))
            };
        }
    }

    public virtual async Task<CheckoutResult> CompletePaymentAsync(CheckoutContext context)
    {
        Guard.NotNull(context);

        CheckoutResult result = null;
        OrderPlacementResult placeOrderResult = null;
        var confirmStep = Guard.NotNull(_checkoutFactory.GetCheckoutStep(CheckoutActionNames.Confirm));
        var paymentType = PaymentMethodType.Standard;
        var store = _storeContext.CurrentStore;
        var customer = context.Cart.Customer;
        var paymentMethod = customer.GenericAttributes.SelectedPaymentMethod;

        context.HttpContext.Session.TryGetObject<ProcessPaymentRequest>(CheckoutState.OrderPaymentInfoName, out var paymentRequest);

        if (paymentRequest != null 
            && paymentRequest.OrderGuid != Guid.Empty
            && await _db.Orders.AnyAsync(x => x.OrderGuid == paymentRequest.OrderGuid && x.CustomerId == customer.Id && x.StoreId == store.Id))
        {
            // The order has already been recovered. The payment plugin was faster. Further processing would result
            // in the error message "Payment.CouldNotLoadMethod" (see below), because "ResetCheckoutData" has been executed.
            //
            // INFO: This order GUID check is unnecessary in "CompleteAsync" because order recoveries only occur when the
            // "IPaymentMethod.RequiresConfirmation" property is set to "true".
            ResetSession(context);
            return new(RedirectToCheckout(CheckoutActionNames.Completed), confirmStep.ViewPath, true);
        }

        try
        {
            paymentRequest ??= new();
            paymentRequest.StoreId = store.Id;
            paymentRequest.CustomerId = customer.Id;
            paymentRequest.PaymentMethodSystemName = paymentMethod;

            var provider = await _paymentService.LoadPaymentProviderBySystemNameAsync(paymentMethod);
            if (provider == null || !provider.Value.RequiresConfirmation)
            {
                throw new Exception(T("Payment.CouldNotLoadMethod"),
                    new Exception($"Cannot complete the payment. The Payment provider {paymentMethod} could not be loaded or does not support payment confirmation."));
            }

            paymentType = provider?.Metadata?.PaymentMethodType ?? PaymentMethodType.Standard;

            if (await provider.Value.CompletePaymentAsync(paymentRequest, context))
            {
                // Payment completed successfully. Place the order.
                placeOrderResult = await _orderProcessingService.PlaceOrderAsync(paymentRequest);
            }
        }
        catch (PaymentException ex)
        {
            // A payment-related error always takes precedence over a generic error.
            // The buyer should only see a simple, general payment message here.
            _logger.Error(ex);
            _notifier.Error(ex.Message);
            return GetDefaultResult();
        }
        catch (Exception ex)
        {
            if (await ProcessOrderPlacementException(ex, customer.Id, true))
            {
                return new(RedirectToCheckout(CheckoutActionNames.Completed), confirmStep.ViewPath, true);
            }
            else
            {
                _notifier.Error(ex.Message);
                return GetDefaultResult();
            }
        }

        if (placeOrderResult != null 
            && placeOrderResult.Success 
            && placeOrderResult.PlacedOrder != null)
        {
            try
            {
                result = await PostProcessPayment(placeOrderResult, confirmStep);
            }
            catch (Exception ex)
            {
                _notifier.Error(ex.Message);
            }
            finally
            {
                ResetSession(context);
            }
        }

        return result ?? GetDefaultResult();

        CheckoutResult GetDefaultResult()
            => new(paymentType == PaymentMethodType.Button ? RedirectToCart() : RedirectToCheckout(CheckoutActionNames.PaymentMethod), confirmStep.ViewPath);
    }

    public virtual async Task<CheckoutResult> CompleteAsync(CheckoutContext context)
    {
        Guard.NotNull(context);

        OrderPlacementResult placeOrderResult = null;
        var confirmStep = Guard.NotNull(_checkoutFactory.GetCheckoutStep(CheckoutActionNames.Confirm));
        var cart = context.Cart;
        var store = _storeContext.CurrentStore;
        var customer = cart.Customer;

        var validationResult = await PublishValidatingCartEvent(context);
        if (validationResult != null)
        {
            return validationResult;
        }

        // Prevent two orders from being placed within a time span of x seconds.
        if (!await _orderProcessingService.IsMinimumOrderPlacementIntervalValidAsync(customer, store))
        {
            _notifier.Warning(T("Checkout.MinOrderPlacementInterval"));
            return new(confirmStep.GetActionResult(context), confirmStep.ViewPath);
        }

        try
        {
            context.HttpContext.Session.TryGetObject<ProcessPaymentRequest>(CheckoutState.OrderPaymentInfoName, out var paymentRequest);
            paymentRequest ??= new();
            paymentRequest.StoreId = store.Id;
            paymentRequest.CustomerId = customer.Id;
            paymentRequest.PaymentMethodSystemName = customer.GenericAttributes.SelectedPaymentMethod;

            customer.GenericAttributes.CheckoutOrderData = GetCheckoutOrderData(context);

            placeOrderResult = await _orderProcessingService.PlaceOrderAsync(paymentRequest);
        }
        catch (PaymentException ex)
        {
            // A payment-related error always takes precedence over a generic error.
            // The buyer should only see a simple, general payment message here.
            return CreateResult(ex, context);
        }
        catch (Exception ex)
        {
            if (await ProcessOrderPlacementException(ex, customer.Id))
            {
                return new(RedirectToCheckout(CheckoutActionNames.Completed), confirmStep.ViewPath, true);
            }
            else
            {
                return new(ex.Message, confirmStep.ViewPath);
            }
        }

        if (placeOrderResult != null
            && placeOrderResult.Success
            && placeOrderResult.PlacedOrder != null)
        {
            CheckoutResult result = null;
            try
            {
                result = await PostProcessPayment(placeOrderResult, confirmStep);
            }
            catch (PaymentException ex)
            {
                result = CreateResult(ex, context);
            }
            catch (Exception ex)
            {
                _notifier.Error(ex.Message);
            }
            finally
            {
                ResetSession(context);
            }

            return result ?? new(RedirectToCheckout(CheckoutActionNames.Completed));
        }
        else
        {
            var errors = placeOrderResult?.Errors
                ?.Take(_maxWarnings)
                ?.Select(x => new CheckoutError(string.Empty, HtmlUtility.ConvertPlainTextToHtml(x)))
                ?.ToArray();

            return new(errors, confirmStep.ViewPath);
        }
    }

    private async Task<CheckoutResult> PostProcessPayment(OrderPlacementResult placeOrderResult, CheckoutStep step)
    {
        var postPaymentRequest = new PostProcessPaymentRequest
        {
            Order = placeOrderResult.PlacedOrder
        };

        await _paymentService.PostProcessPaymentAsync(postPaymentRequest);

        if (postPaymentRequest.RedirectUrl.HasValue())
        {
            return new(new RedirectResult(postPaymentRequest.RedirectUrl), step.ViewPath, true);
        }

        return new(RedirectToCheckout(CheckoutActionNames.Completed), step.ViewPath, true);
    }

    private static Dictionary<string, string> GetCheckoutOrderData(CheckoutContext context)
    {
        return new()
        {
            [CustomerCommentKey] = context.GetFormValue<string>("customercommenthidden"),
            [SubscribeToNewsletterKey] = context.GetFormValue<string>(SubscribeToNewsletterKey),
            [AcceptThirdPartyEmailHandOverKey] = context.GetFormValue<string>(AcceptThirdPartyEmailHandOverKey)
        };
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
    private CheckoutStep Adjacent(CheckoutStep step)
    {
        // Get route values of the URL referrer.
        var referrer = _webHelper.ClientInfo.UrlReferrer;
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

    private async Task<CheckoutResult> PublishValidatingCartEvent(CheckoutContext context)
    {
        var warnings = new List<string>();
        var result = new ValidatingCartEvent(context.Cart, warnings);

        await _eventPublisher.PublishAsync(result);

        if (result.Result != null)
        {
            return new(result.Result);
        }

        if (warnings.Count > 0)
        {
            warnings.Take(_maxWarnings).Each(x => _notifier.Warning(x));
            return new(RedirectToCart());
        }

        return null;
    }

    private CheckoutResult CreateResult(PaymentException ex, CheckoutContext context)
    {
        if (ex.RedirectRoute is not string)
        {
            _logger.ErrorsAll(ex);
            _notifier.Error(ex.Message);
        }

        if (ex.RedirectRoute is RouteInfo routeInfo)
        {
            return new(new RedirectToActionResult(routeInfo.Action, routeInfo.Controller, routeInfo.RouteValues));
        }
        else if (ex.RedirectRoute is RouteValueDictionary routeValues)
        {
            return new(new RedirectToRouteResult(routeValues));
        }
        else if (ex.RedirectRoute is string redirectUrl)
        {
            return new(new RedirectResult(redirectUrl));
        }

        var paymentStep = _checkoutFactory.GetCheckoutStep(CheckoutActionNames.PaymentMethod);

        return new(paymentStep.GetActionResult(context), paymentStep.ViewPath);
    }

    private async Task<bool> ProcessOrderPlacementException(Exception ex, int customerId, bool notify = false)
    {
        var isDuplicatePaymentReference = await _orderProcessingService.IsDuplicatePaymentReferenceAsync(ex);
        var msg = T(isDuplicatePaymentReference ? "Order.AlreadyExists" : "Order.PlaceOrderError", customerId);

        _logger.Log(
            isDuplicatePaymentReference ? MsLogLevel.Warning : MsLogLevel.Error, 
            ex, 
            msg);

        if (notify && !isDuplicatePaymentReference)
        {
            _notifier.Error(msg);
        }

        return isDuplicatePaymentReference;
    }

    private static RedirectToActionResult RedirectToCheckout(string action)
        => new(action, "Checkout", null);

    // INFO: do not use RedirectToRouteResult here. It would create an infinite redirection loop.
    // In CheckoutWorkflow always use RedirectToActionResult with controller and action name.
    private static RedirectToActionResult RedirectToCart()
        => new("Cart", "ShoppingCart", null);

    private void ResetSession(CheckoutContext context)
    {
        context.HttpContext.Session.TrySetObject<ProcessPaymentRequest>(CheckoutState.OrderPaymentInfoName, null);
        _checkoutStateAccessor.Abandon();
    }
}
