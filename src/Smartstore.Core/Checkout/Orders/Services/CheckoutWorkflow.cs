using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Cart.Events;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class CheckoutWorkflow : ICheckoutWorkflow
    {
        const int _maxWarnings = 3;

        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;
        private readonly INotifier _notifier;
        private readonly IWebHelper _webHelper;
        private readonly IEventPublisher _eventPublisher;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly ICheckoutHandler[] _handlers;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly OrderSettings _orderSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public CheckoutWorkflow(
            SmartDbContext db,
            IStoreContext storeContext,
            INotifier notifier,
            IWebHelper webHelper,
            IEventPublisher eventPublisher,
            IShoppingCartService shoppingCartService,
            IShoppingCartValidator shoppingCartValidator,
            IEnumerable<ICheckoutHandler> handlers,
            ICheckoutStateAccessor checkoutStateAccessor,
            IHttpContextAccessor httpContextAccessor,
            OrderSettings orderSettings,
            ShoppingCartSettings shoppingCartSettings)
        {
            _db = db;
            _storeContext = storeContext;
            _notifier = notifier;
            _webHelper = webHelper;
            _eventPublisher = eventPublisher;
            _shoppingCartService = shoppingCartService;
            _shoppingCartValidator = shoppingCartValidator;
            _checkoutStateAccessor = checkoutStateAccessor;
            _httpContextAccessor = httpContextAccessor;
            _orderSettings = orderSettings;
            _shoppingCartSettings = shoppingCartSettings;

            _handlers = handlers.OrderBy(x => x.Order).ToArray();
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public virtual async Task<CheckoutWorkflowResult> StartAsync()
        {
            var warnings = new List<string>();
            var store = _storeContext.CurrentStore;
            var cart = await _shoppingCartService.GetCartAsync(storeId: store.Id);
            var customer = cart.Customer;

            var preliminaryResult = Preliminary(cart);
            if (preliminaryResult != null)
            {
                return new(preliminaryResult);
            }

            customer.ResetCheckoutData(store.Id);
            _checkoutStateAccessor.Abandon();

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

                    var ctx = new AddToCartContext
                    {
                        StoreId = store.Id,
                        Product = item.Item.Product,
                        BundleItem = item.Item.BundleItem,
                        ChildItems = item.ChildItems.Select(x => x.Item).ToList()
                    };

                    if (!await _shoppingCartValidator.ValidateAddToCartItemAsync(ctx, item.Item, cart.Items))
                    {
                        warnings.AddRange(ctx.Warnings);
                    }
                }
            }

            await _db.SaveChangesAsync();

            if (warnings.Count > 0)
            {
                warnings.Take(_maxWarnings).Each(x => _notifier.Warning(x));
                return new(RedirectToCart());
            }

            return await AdvanceAsync();
        }

        public virtual async Task<CheckoutWorkflowResult> ProcessAsync()
        {
            var cart = await _shoppingCartService.GetCartAsync(storeId: _storeContext.CurrentStore.Id);
            var preliminaryResult = Preliminary(cart);
            if (preliminaryResult != null)
            {
                return new(preliminaryResult);
            }

            var (action, controller) = GetActionAndController();
            var handler = _handlers.FirstOrDefault(x => x.IsHandlerFor(action, controller));
            if (handler != null)
            {
                var result = await handler.ProcessAsync(cart);
                if (result.SkipPage)
                {
                    return new(result.ActionResult ?? Adjacent(handler));
                }

                return new(null, result.Errors);
            }

            return new(null);
        }

        public virtual async Task<CheckoutWorkflowResult> AdvanceAsync(object model = null)
        {
            var cart = await _shoppingCartService.GetCartAsync(storeId: _storeContext.CurrentStore.Id);
            var preliminaryResult = Preliminary(cart);
            if (preliminaryResult != null)
            {
                return new(preliminaryResult);
            }

            if (_shoppingCartSettings.QuickCheckoutEnabled)
            {
                foreach (var handler in _handlers)
                {
                    var result = await handler.ProcessAsync(cart, model);
                    if (!result.Success)
                    {
                        return new(result.ActionResult ?? handler.GetActionResult(), result.Errors);
                    }
                }

                return new(RedirectToCheckout("Completed"));
            }
            else
            {
                var (action, controller) = GetActionAndController();
                if (action.EqualsNoCase("Index") && controller.EqualsNoCase("Checkout"))
                {
                    return new(_handlers[0].GetActionResult());
                }

                var handler = _handlers.FirstOrDefault(x => x.IsHandlerFor(action, controller));
                if (handler != null)
                {
                    var result = await handler.ProcessAsync(cart, model);
                    if (!result.Success)
                    {
                        return new(result.ActionResult ?? handler.GetActionResult(), result.Errors);
                    }

                    if (handler.Equals(_handlers[^1]))
                    {
                        return new(RedirectToCheckout("Completed"));
                    }
                    
                    var nextHandler = GetNextHandler(handler, true);
                    if (nextHandler != null)
                    {
                        return new(nextHandler.GetActionResult());
                    }
                }

                return new(null);
            }
        }

        private IActionResult Preliminary(ShoppingCart cart)
        {
            if (_httpContextAccessor.HttpContext?.Request == null)
            {
                throw new InvalidOperationException("The checkout workflow is only applicable in the context of a HTTP request.");
            }

            if (_handlers.Length == 0)
            {
                throw new InvalidOperationException("No checkout handlers found.");
            }

            if (!_orderSettings.AnonymousCheckoutAllowed && !cart.Customer.IsRegistered())
            {
                return new ChallengeResult();
            }

            if (!cart.HasItems)
            {
                return RedirectToCart();
            }

            return null;
        }

        /// <summary>
        /// Special case when a checkout page must always be skipped (e.g. if the store only offers a single shipping method).
        /// In this case, based on the referrer, the user must be redirected to the next or previous page,
        /// depending on the direction from which the user accessed the current page.
        /// </summary>
        private IActionResult Adjacent(ICheckoutHandler handler)
        {
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
                    next = true;
                }
                else if (action.EqualsNoCase("Confirm") && controller.EqualsNoCase("Checkout"))
                {
                    next = false;
                }
                else
                {
                    var referrerHandler = _handlers.FirstOrDefault(x => x.IsHandlerFor(action, controller));
                    next = (referrerHandler?.Order ?? 0) < handler.Order;
                }
            }

            var result = GetNextHandler(handler, next)?.GetActionResult();
            result ??= next ? RedirectToCheckout("Confirm") : RedirectToCart();

            return result;
        }

        private ICheckoutHandler GetNextHandler(ICheckoutHandler handler, bool next)
        {
            if (next)
            {
                return _handlers
                    .Where(x => x.Order > handler.Order)
                    .OrderBy(x => x.Order)
                    .FirstOrDefault();
            }
            else
            {
                return _handlers
                    .Where(x => x.Order < handler.Order)
                    .OrderByDescending(x => x.Order)
                    .FirstOrDefault();
            }
        }

        private (string Action, string Controller) GetActionAndController()
        {
            var routeValues = _httpContextAccessor.HttpContext.Request.RouteValues;
            return (routeValues.GetActionName(), routeValues.GetControllerName());
        }

        internal static RedirectToActionResult RedirectToCheckout(string action)
            => new(action, "Checkout", null);

        // INFO: do not use RedirectToRouteResult here. It would create an infinite redirection loop.
        // In CheckoutWorkflow always use RedirectToActionResult with controller and action name.
        internal static RedirectToActionResult RedirectToCart()
            => new("Cart", "ShoppingCart", null);
    }
}
