using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Cart.Events;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Stores;
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class CheckoutWorkflow : ICheckoutWorkflow
    {
        const int _maxWarnings = 3;

        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;
        private readonly INotifier _notifier;
        private readonly ILogger _logger;
        private readonly IEventPublisher _eventPublisher;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IPaymentService _paymentService;
        private readonly IEnumerable<ICheckoutRequirement> _checkoutRequirements;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CheckoutWorkflow(
            SmartDbContext db,
            IStoreContext storeContext,
            INotifier notifier,
            ILogger logger,
            IEventPublisher eventPublisher,
            IShoppingCartService shoppingCartService,
            IShoppingCartValidator shoppingCartValidator,
            IOrderCalculationService orderCalculationService,
            IOrderProcessingService orderProcessingService,
            IPaymentService paymentService,
            IEnumerable<ICheckoutRequirement> checkoutRequirements,
            ICheckoutStateAccessor checkoutStateAccessor,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _storeContext = storeContext;
            _notifier = notifier;
            _logger = logger;
            _eventPublisher = eventPublisher;
            _shoppingCartService = shoppingCartService;
            _shoppingCartValidator = shoppingCartValidator;
            _orderCalculationService = orderCalculationService;
            _orderProcessingService = orderProcessingService;
            _paymentService = paymentService;
            _checkoutRequirements = checkoutRequirements;
            _checkoutStateAccessor = checkoutStateAccessor;
            _httpContextAccessor = httpContextAccessor;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public virtual async Task<IActionResult> StartAsync()
        {
            var warnings = new List<string>();
            var store = _storeContext.CurrentStore;
            var cart = await _shoppingCartService.GetCartAsync(storeId: store.Id);

            var preRequirementResult = await CheckRequirements(cart, true);
            if (preRequirementResult != null)
            {
                return preRequirementResult;
            }

            cart.Customer.ResetCheckoutData(store.Id);
            _checkoutStateAccessor.Abandon();

            if (await _shoppingCartValidator.ValidateCartAsync(cart, warnings, true))
            {
                var validatingCartEvent = new ValidatingCartEvent(cart, warnings);
                await _eventPublisher.PublishAsync(validatingCartEvent);

                if (validatingCartEvent.Result != null)
                {
                    return validatingCartEvent.Result;
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

            if (warnings.Count > 0)
            {
                warnings.Take(_maxWarnings).Each(x => _notifier.Warning(x));
                return RedirectToCart();
            }

            await _db.SaveChangesAsync();

            return RedirectToCheckout("BillingAddress");
        }

        public virtual async Task<IActionResult> AdvanceAsync()
        {
            var cart = await _shoppingCartService.GetCartAsync(storeId: _storeContext.CurrentStore.Id);

            return await CheckRequirements(cart, false) ?? RedirectToCheckout("Confirm");
        }

        public virtual async Task<IActionResult> CompleteAsync()
        {
            var warnings = new List<string>();
            var store = _storeContext.CurrentStore;
            var cart = await _shoppingCartService.GetCartAsync(storeId: store.Id);

            var preRequirementResult = await CheckRequirements(cart, true);
            if (preRequirementResult != null)
            {
                return preRequirementResult;
            }

            var validatingCartEvent = new ValidatingCartEvent(cart, warnings);
            await _eventPublisher.PublishAsync(validatingCartEvent);

            if (validatingCartEvent.Result != null)
            {
                return validatingCartEvent.Result;
            }

            if (warnings.Count > 0)
            {
                warnings.Take(_maxWarnings).Each(x => _notifier.Warning(x));
                return RedirectToCart();
            }

            // Prevent two orders from being placed within a time span of x seconds.
            if (!await _orderProcessingService.IsMinimumOrderPlacementIntervalValidAsync(cart.Customer, store))
            {
                _notifier.Warning(T("Checkout.MinOrderPlacementInterval"));
                return RedirectToCheckout("Confirm");
            }

            OrderPlacementResult placeOrderResult = null;
            var ctx = _httpContextAccessor.HttpContext;

            try
            {
                if (!ctx.Session.TryGetObject<ProcessPaymentRequest>(CheckoutState.OrderPaymentInfoName, out var paymentRequest))
                {
                    // Check whether payment workflow is required.
                    var cartTotalBase = await _orderCalculationService.GetShoppingCartTotalAsync(cart, false);

                    if (!cartTotalBase.Total.HasValue && cartTotalBase.Total.Value != decimal.Zero
                        || !_checkoutStateAccessor.CheckoutState.IsPaymentSelectionSkipped)
                    {
                        return RedirectToCheckout("PaymentMethod");
                    }

                    paymentRequest = new();
                }

                paymentRequest.StoreId = store.Id;
                paymentRequest.CustomerId = cart.Customer.Id;
                paymentRequest.PaymentMethodSystemName = cart.Customer.GenericAttributes.SelectedPaymentMethod;

                var placeOrderExtraData = new Dictionary<string, string>
                {
                    ["CustomerComment"] = ctx.Request.Form["customercommenthidden"].ToString(),
                    ["SubscribeToNewsletter"] = ctx.Request.Form["SubscribeToNewsletter"].ToString(),
                    ["AcceptThirdPartyEmailHandOver"] = ctx.Request.Form["AcceptThirdPartyEmailHandOver"].ToString()
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
                _notifier.Error(ex.Message);

                return RedirectToCheckout("Confirm");
            }

            if (placeOrderResult == null || !placeOrderResult.Success)
            {
                placeOrderResult?.Errors?.Take(_maxWarnings)?.Each(x => _notifier.Warning(x));
                return RedirectToCheckout("Confirm");
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
                return RedirectToCheckout("Confirm");
            }
            finally
            {
                ctx.Session.TrySetObject<ProcessPaymentRequest>(CheckoutState.OrderPaymentInfoName, null);
                _checkoutStateAccessor.Abandon();
            }

            if (postPaymentRequest.RedirectUrl.HasValue())
            {
                return new RedirectResult(postPaymentRequest.RedirectUrl);
            }

            return RedirectToCheckout("Completed");

            RedirectToActionResult PaymentFailure(PaymentException ex)
            {
                _logger.Error(ex);
                _notifier.Error(ex.Message);

                if (ex.RedirectRoute != null)
                {
                    return new RedirectToActionResult(ex.RedirectRoute.Action, ex.RedirectRoute.Controller, ex.RedirectRoute.RouteValues);
                }

                return RedirectToCheckout("PaymentMethod");
            }
        }

        private async Task<IActionResult> CheckRequirements(ShoppingCart cart, bool preRequirementsOnly)
        {
            var requirements = preRequirementsOnly
                ? _checkoutRequirements.Where(x => x.Order < 0)
                : _checkoutRequirements;

            foreach (var requirement in requirements.OrderBy(x => x.Order))
            {
                if (!await requirement.IsFulfilledAsync(cart))
                {
                    return requirement.Fulfill();
                }
            }

            return null;
        }

        //protected virtual bool Challenge(Customer customer)
        //    => !customer.IsRegistered() && !_orderSettings.AnonymousCheckoutAllowed;

        internal static RedirectToActionResult RedirectToCheckout(string action)
            => new(action, "Checkout", null);

        internal static RedirectToRouteResult RedirectToCart()
            => new("ShoppingCart");
    }
}
