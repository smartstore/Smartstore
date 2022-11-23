using Microsoft.AspNetCore.Http;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Cart.Events;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Localization.Routing;
using Smartstore.Engine.Modularity;
using Smartstore.Utilities.Html;
using Smartstore.Web.Models.Cart;
using Smartstore.Web.Models.Checkout;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Controllers
{
    public class CheckoutController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly IPaymentService _paymentService;
        private readonly IShippingService _shippingService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly ModuleManager _moduleManager;
        private readonly ShippingSettings _shippingSettings;
        private readonly PaymentSettings _paymentSettings;
        private readonly OrderSettings _orderSettings;

        public CheckoutController(
            SmartDbContext db,
            IPaymentService paymentService,
            IShippingService shippingService,
            IShoppingCartService shoppingCartService,
            IShoppingCartValidator shoppingCartValidator,
            IOrderProcessingService orderProcessingService,
            IOrderCalculationService orderCalculationService,
            ICheckoutStateAccessor checkoutStateAccessor,
            ModuleManager moduleManager,
            ShippingSettings shippingSettings,
            PaymentSettings paymentSettings,
            OrderSettings orderSettings)
        {
            _db = db;
            _paymentService = paymentService;
            _shippingService = shippingService;
            _shoppingCartService = shoppingCartService;
            _orderProcessingService = orderProcessingService;
            _orderCalculationService = orderCalculationService;
            _checkoutStateAccessor = checkoutStateAccessor;
            _shoppingCartValidator = shoppingCartValidator;
            _moduleManager = moduleManager;
            _shippingSettings = shippingSettings;
            _paymentSettings = paymentSettings;
            _orderSettings = orderSettings;
        }

        private async Task<bool> ValidatePaymentDataAsync(IPaymentMethod paymentMethod, IFormCollection form)
        {
            var warnings = await paymentMethod.GetPaymentDataWarningsAsync();

            if (warnings != null)
            {
                warnings.Each(x => ModelState.AddModelError(string.Empty, x));
            }

            if (!ModelState.IsValid)
            {
                return false;
            }

            var paymentInfo = await paymentMethod.GetPaymentInfoAsync(form);
            HttpContext.Session.TrySetObject("OrderPaymentInfo", paymentInfo);

            var state = _checkoutStateAccessor.CheckoutState;
            state.PaymentSummary = await paymentMethod.GetPaymentSummaryAsync();

            return true;
        }

        private async Task<CheckoutAddressModel> PrepareCheckoutAddressModelAsync(bool shipping = false)
        {
            // Get existing addresses.
            var customer = Services.WorkContext.CurrentCustomer;
            var addresses = new List<Address>(customer.Addresses).AsEnumerable();

            // And map billing / shipping addresses.
            var model = new CheckoutAddressModel();
            await addresses.MapAsync(model, shipping, null);

            return model;
        }

        [LocalizedRoute("/checkout", Name = "Checkout")]
        public async Task<IActionResult> Index()
        {
            var storeId = Services.StoreContext.CurrentStore.Id;
            var customer = Services.WorkContext.CurrentCustomer;
            var cart = await _shoppingCartService.GetCartAsync(customer, storeId: storeId);

            if (!cart.Items.Any())
            {
                return RedirectToRoute("ShoppingCart");
            }

            if (customer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
            {
                return new UnauthorizedResult();
            }

            customer.ResetCheckoutData(storeId);
            _checkoutStateAccessor.Abandon();

            // Validate checkout attributes.
            var warnings = new List<string>();
            if (!await _shoppingCartValidator.ValidateCartAsync(cart, warnings, true))
            {
                warnings.Take(3).Each(x => NotifyWarning(x));

                return RedirectToRoute("ShoppingCart");
            }

            var validatingCartEvent = new ValidatingCartEvent(cart, warnings);
            await Services.EventPublisher.PublishAsync(validatingCartEvent);

            if (validatingCartEvent.Result != null)
            {
                return validatingCartEvent.Result;
            }

            if (warnings.Any())
            {
                warnings.Take(3).Each(x => NotifyWarning(x));

                return RedirectToRoute("ShoppingCart");
            }

            // Validate each shopping cart item.
            foreach (var cartItem in cart.Items)
            {
                var ctx = new AddToCartContext
                {
                    StoreId = storeId,
                    Product = cartItem.Item.Product,
                    BundleItem = cartItem.Item.BundleItem,
                    ChildItems = cartItem.ChildItems.Select(x => x.Item).ToList()
                };

                if (!await _shoppingCartValidator.ValidateAddToCartItemAsync(ctx, cartItem.Item, cart.Items))
                {
                    warnings.AddRange(ctx.Warnings);
                    warnings.Take(3).Each(x => NotifyWarning(x));

                    return RedirectToRoute("ShoppingCart");
                }
            }

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(BillingAddress));
        }

        public async Task<IActionResult> BillingAddress()
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var cart = await _shoppingCartService.GetCartAsync(customer, storeId: Services.StoreContext.CurrentStore.Id);

            if (!cart.Items.Any())
            {
                return RedirectToRoute("ShoppingCart");
            }

            if (customer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
            {
                return new UnauthorizedResult();
            }

            var model = await PrepareCheckoutAddressModelAsync(false);
            return View(model);
        }

        public async Task<IActionResult> SelectBillingAddress(int addressId)
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var address = customer.Addresses.FirstOrDefault(x => x.Id == addressId);
            if (address == null)
            {
                return RedirectToAction(nameof(BillingAddress));
            }

            customer.BillingAddress = address;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(ShippingAddress));
        }

        [HttpPost, ActionName("BillingAddress")]
        [FormValueRequired("nextstep")]
        public async Task<IActionResult> NewBillingAddress(CheckoutAddressModel model)
        {
            var result = await HandleNewAddress(model, true);
            if (result != null)
            {
                return result;
            }

            model = await PrepareCheckoutAddressModelAsync(false);
            return View(model);
        }

        [HttpPost, ActionName("ShippingAddress")]
        [FormValueRequired("nextstep")]
        public async Task<IActionResult> NewShippingAddress(CheckoutAddressModel model)
        {
            var result = await HandleNewAddress(model, true);
            if (result != null)
            {
                return result;
            }

            model = await PrepareCheckoutAddressModelAsync(false);
            return View(model);
        }

        private async Task<IActionResult> HandleNewAddress(CheckoutAddressModel model, bool isBillingAddress)
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var cart = await _shoppingCartService.GetCartAsync(customer, storeId: Services.StoreContext.CurrentStore.Id);

            if (!cart.Items.Any())
            {
                return RedirectToRoute("ShoppingCart");
            }

            if (customer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
            {
                return new UnauthorizedResult();
            }

            if (ModelState.IsValid)
            {
                var address = await MapperFactory.MapAsync<AddressModel, Address>(model.NewAddress);
                address.CreatedOnUtc = DateTime.UtcNow;

                if (address.CountryId == 0)
                {
                    address.CountryId = null;
                }
                if (address.StateProvinceId == 0)
                {
                    address.StateProvinceId = null;
                }

                customer.Addresses.Add(address);
                if (isBillingAddress)
                {
                    customer.BillingAddress = address;
                }
                else
                {
                    customer.ShippingAddress = address;
                }

                await _db.SaveChangesAsync();

                return RedirectToAction(isBillingAddress ? nameof(ShippingAddress) : nameof(ShippingMethod));
            }

            return null;
        }

        public async Task<IActionResult> ShippingAddress()
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var cart = await _shoppingCartService.GetCartAsync(customer, storeId: Services.StoreContext.CurrentStore.Id);

            if (!cart.Items.Any())
            {
                return RedirectToRoute("ShoppingCart");
            }

            if (customer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
            {
                return new UnauthorizedResult();
            }

            if (!cart.IncludesMatchingItems(x => x.IsShippingEnabled))
            {
                customer.ShippingAddress = null;

                await _db.SaveChangesAsync();

                return RedirectToAction(nameof(ShippingMethod));
            }

            var model = await PrepareCheckoutAddressModelAsync(true);
            return View(model);
        }

        public async Task<IActionResult> SelectShippingAddress(int addressId)
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var address = customer.Addresses.FirstOrDefault(x => x.Id == addressId);
            if (address == null)
            {
                return RedirectToAction(nameof(ShippingAddress));
            }

            customer.ShippingAddress = address;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(ShippingMethod));
        }

        public async Task<IActionResult> ShippingMethod()
        {
            var storeId = Services.StoreContext.CurrentStore.Id;
            var customer = Services.WorkContext.CurrentCustomer;
            var cart = await _shoppingCartService.GetCartAsync(customer, storeId: storeId);

            if (!cart.Items.Any())
            {
                return RedirectToRoute("ShoppingCart");
            }

            if (customer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
            {
                return new UnauthorizedResult();
            }

            if (!cart.IsShippingRequired())
            {
                customer.GenericAttributes.SelectedShippingOption = null;
                await customer.GenericAttributes.SaveChangesAsync();

                return RedirectToAction(nameof(PaymentMethod));
            }

            var response = await _shippingService.GetShippingOptionsAsync(cart, customer.ShippingAddress, storeId: storeId);
            var options = response.ShippingOptions;
            var state = _checkoutStateAccessor.CheckoutState;

            state.CustomProperties["HasOnlyOneActiveShippingMethod"] = options.Count == 1;

            if (options.Count <= 1 && _shippingSettings.SkipShippingIfSingleOption && response.Success)
            {
                customer.GenericAttributes.SelectedShippingOption = options.FirstOrDefault();
                await customer.GenericAttributes.SaveChangesAsync();

                var referrer = Services.WebHelper.GetUrlReferrer().AbsolutePath;
                if (referrer.EndsWith("/PaymentMethod") || referrer.EndsWith("/Confirm"))
                {
                    return RedirectToAction(nameof(ShippingAddress));
                }

                return RedirectToAction(nameof(PaymentMethod));
            }

            var model = new CheckoutShippingMethodModel();
            await cart.MapAsync(model, new { ShippingOptionResponse = response });

            return View(model);
        }

        [HttpPost, ActionName("ShippingMethod")]
        [FormValueRequired("nextstep")]
        public async Task<IActionResult> SelectShippingMethod(string shippingOption)
        {
            var storeId = Services.StoreContext.CurrentStore.Id;
            var customer = Services.WorkContext.CurrentCustomer;

            var cart = await _shoppingCartService.GetCartAsync(customer, storeId: storeId);
            if (!cart.Items.Any())
            {
                return RedirectToRoute("ShoppingCart");
            }

            if (customer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
            {
                return new UnauthorizedResult();
            }

            if (!cart.IsShippingRequired())
            {
                customer.GenericAttributes.SelectedShippingOption = null;
                await customer.GenericAttributes.SaveChangesAsync();

                return RedirectToAction(nameof(PaymentMethod));
            }

            // Parse selected method 
            if (!shippingOption.HasValue())
            {
                return RedirectToAction(nameof(ShippingMethod));
            }

            var splittedOption = shippingOption.Split(new[] { "___" }, StringSplitOptions.RemoveEmptyEntries);
            if (splittedOption.Length != 2)
            {
                return RedirectToAction(nameof(ShippingMethod));
            }

            var selectedName = splittedOption[0];
            var shippingRateComputationMethodSystemName = splittedOption[1];

            // Find shipping option. For performance reasons, try cached first.
            var shippingOptions = customer.GenericAttributes.OfferedShippingOptions;
            if (shippingOptions == null || !shippingOptions.Any())
            {
                // Shipping option was not found in customer attributes. Load via shipping service.
                shippingOptions = (await _shippingService
                    .GetShippingOptionsAsync(cart, customer.ShippingAddress, shippingRateComputationMethodSystemName, storeId))
                    .ShippingOptions;
            }
            else
            {
                // Loaded cached results. Filter result by a chosen shipping rate computation method.
                shippingOptions = shippingOptions
                    .Where(x => x.ShippingRateComputationMethodSystemName.EqualsNoCase(shippingRateComputationMethodSystemName))
                    .ToList();
            }

            var selectedShippingOption = shippingOptions.Find(x => x.Name.EqualsNoCase(selectedName));
            if (selectedShippingOption == null)
            {
                return RedirectToAction(nameof(ShippingMethod));
            }

            // Save selected shipping option in customer attributes.
            customer.GenericAttributes.SelectedShippingOption = selectedShippingOption;
            await customer.GenericAttributes.SaveChangesAsync();

            return RedirectToAction(nameof(PaymentMethod));
        }

        public async Task<IActionResult> PaymentMethod()
        {
            var storeId = Services.StoreContext.CurrentStore.Id;
            var customer = Services.WorkContext.CurrentCustomer;
            var language = Services.WorkContext.WorkingLanguage;

            var cart = await _shoppingCartService.GetCartAsync(customer, storeId: storeId);
            if (!cart.Items.Any())
            {
                return RedirectToRoute("ShoppingCart");
            }

            if (!_orderSettings.AnonymousCheckoutAllowed && customer.IsGuest())
            {
                return new UnauthorizedResult();
            }

            // Check whether payment workflow is required. We ignore reward points during cart total calculation.
            Money? shoppingCartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(cart, false);

            var model = new CheckoutPaymentMethodModel();
            await cart.MapAsync(model);

            var onlyOnePassiveMethod = model.PaymentMethods.Count == 1 && !model.PaymentMethods[0].RequiresInteraction;

            var checkoutState = _checkoutStateAccessor.CheckoutState;
            checkoutState.CustomProperties["HasOnlyOneActivePaymentMethod"] = model.PaymentMethods.Count == 1;
            checkoutState.IsPaymentRequired = shoppingCartTotal.GetValueOrDefault() != decimal.Zero;
            checkoutState.IsPaymentSelectionSkipped = !checkoutState.IsPaymentRequired || _paymentSettings.BypassPaymentMethodSelectionIfOnlyOne && onlyOnePassiveMethod;

            if (checkoutState.IsPaymentSelectionSkipped)
            {
                // If there is nothing to pay for OR if only one passive payment method is active and Reward Points are disabled
                // OR the current customer has no Reward Points, then the customer does not need to select a payment method.

                customer.GenericAttributes.SelectedPaymentMethod = model.PaymentMethods?.FirstOrDefault()?.PaymentMethodSystemName;
                await _db.SaveChangesAsync();

                var referrer = Services.WebHelper.GetUrlReferrer();
                if (referrer != null && referrer.OriginalString.EndsWith('/' + nameof(Confirm)))
                {
                    return RedirectToAction(nameof(ShippingMethod));
                }

                return RedirectToAction(nameof(Confirm));
            }

            return View(model);
        }

        [HttpPost, ActionName("PaymentMethod")]
        [FormValueRequired("nextstep")]
        public async Task<IActionResult> SelectPaymentMethod(string paymentMethod, CheckoutPaymentMethodModel model, IFormCollection form)
        {
            var storeId = Services.StoreContext.CurrentStore.Id;
            var customer = Services.WorkContext.CurrentCustomer;

            var cart = await _shoppingCartService.GetCartAsync(customer, storeId: storeId);
            if (!cart.Items.Any())
            {
                return RedirectToRoute("ShoppingCart");
            }

            if (customer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
            {
                return new UnauthorizedResult();
            }

            // Payment method 
            if (!paymentMethod.HasValue())
            {
                return RedirectToAction(nameof(PaymentMethod));
            }

            var paymentMethodProvider = await _paymentService.LoadPaymentMethodBySystemNameAsync(paymentMethod, true, storeId);
            if (paymentMethodProvider == null)
            {
                return RedirectToAction(nameof(PaymentMethod));
            }

            // Save payment method for customer.
            customer.GenericAttributes.SelectedPaymentMethod = paymentMethod;
            await customer.GenericAttributes.SaveChangesAsync();

            // Validate info
            if (!await ValidatePaymentDataAsync(paymentMethodProvider.Value, form))
            {
                return RedirectToAction(nameof(PaymentMethod));
            }

            // Save payment data so that the user must not re-enter it.
            var state = _checkoutStateAccessor.CheckoutState;
            foreach (var kvp in form)
            {
                state.PaymentData[kvp.Key] = kvp.Value.ToString();
            }

            return RedirectToAction(nameof(Confirm));
        }

        [HttpPost]
        public async Task<IActionResult> PaymentInfoAjax(string paymentMethodSystemName)
        {
            if (Services.WorkContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
            {
                return new EmptyResult();
            }

            if (paymentMethodSystemName.IsEmpty())
            {
                return new NotFoundResult();
            }

            var paymentMethod = await _paymentService.LoadPaymentMethodBySystemNameAsync(paymentMethodSystemName);
            if (paymentMethod == null)
            {
                return new NotFoundResult();
            }

            var infoWidget = paymentMethod.Value.GetPaymentInfoWidget();
            if (infoWidget == null)
            {
                return new EmptyResult();
            }

            var widgetContent = await infoWidget.InvokeAsync(new WidgetContext(ControllerContext));
            return Content(widgetContent.ToHtmlString().ToString());
        }

        public async Task<IActionResult> Confirm()
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var cart = await _shoppingCartService.GetCartAsync(customer, storeId: Services.StoreContext.CurrentStore.Id);
            if (!cart.Items.Any())
            {
                return RedirectToRoute("ShoppingCart");
            }

            if (customer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
            {
                return new UnauthorizedResult();
            }

            var model = new CheckoutConfirmModel();
            await cart.MapAsync(model);

            return View(model);
        }

        [HttpPost, ActionName("Confirm")]
        public async Task<IActionResult> ConfirmOrder()
        {
            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var cart = await _shoppingCartService.GetCartAsync(customer, storeId: store.Id);

            if (!cart.Items.Any())
            {
                return RedirectToRoute("ShoppingCart");
            }

            if (customer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
            {
                return new UnauthorizedResult();
            }

            var warnings = new List<string>();
            var validatingCartEvent = new ValidatingCartEvent(cart, warnings);
            await Services.EventPublisher.PublishAsync(validatingCartEvent);

            if (validatingCartEvent.Result != null)
            {
                return validatingCartEvent.Result;
            }

            if (warnings.Any())
            {
                warnings.Take(3).Each(x => NotifyWarning(x));

                return RedirectToRoute("ShoppingCart");
            }

            var model = new CheckoutConfirmModel();
            OrderPlacementResult placeOrderResult = null;

            try
            {
                if (!HttpContext.Session.TryGetObject<ProcessPaymentRequest>("OrderPaymentInfo", out var processPaymentRequest))
                {
                    // Check whether payment workflow is required.
                    var cartTotalBase = await _orderCalculationService.GetShoppingCartTotalAsync(cart, false);

                    if (!cartTotalBase.Total.HasValue && cartTotalBase.Total.Value != decimal.Zero
                        || !_checkoutStateAccessor.CheckoutState.IsPaymentSelectionSkipped)
                    {
                        return RedirectToAction(nameof(PaymentMethod));
                    }

                    processPaymentRequest = new ProcessPaymentRequest();
                }

                // Prevent two orders from being placed within a time span of x seconds.
                if (!await _orderProcessingService.IsMinimumOrderPlacementIntervalValidAsync(customer, store))
                {
                    throw new Exception(T("Checkout.MinOrderPlacementInterval"));
                }

                // Place the order.
                processPaymentRequest.StoreId = store.Id;
                processPaymentRequest.CustomerId = customer.Id;
                processPaymentRequest.PaymentMethodSystemName = customer.GenericAttributes.SelectedPaymentMethod;

                var placeOrderExtraData = new Dictionary<string, string>
                {
                    ["CustomerComment"] = HttpContext.Request.Form["customercommenthidden"].ToString(),
                    ["SubscribeToNewsletter"] = HttpContext.Request.Form["SubscribeToNewsletter"].ToString(),
                    ["AcceptThirdPartyEmailHandOver"] = HttpContext.Request.Form["AcceptThirdPartyEmailHandOver"].ToString()
                };

                placeOrderResult = await _orderProcessingService.PlaceOrderAsync(processPaymentRequest, placeOrderExtraData);

                if (!placeOrderResult.Success)
                {
                    model.Warnings.AddRange(placeOrderResult.Errors.Select(x => HtmlUtility.ConvertPlainTextToHtml(x)));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);

                if (!model.Warnings.Any(x => x == ex.Message))
                {
                    model.Warnings.Add(ex.Message);
                }
            }

            if (placeOrderResult == null || !placeOrderResult.Success || model.Warnings.Any())
            {
                var paymentMethod = await _paymentService.LoadPaymentMethodBySystemNameAsync(customer.GenericAttributes.SelectedPaymentMethod);
                if (paymentMethod != null && paymentMethod.Value.PaymentMethodType == PaymentMethodType.Button)
                {
                    model.Warnings.Take(3).Each(x => NotifyError(x));

                    // Redirect back to where the payment button is.
                    return RedirectToAction(nameof(ShoppingCartController.Cart), "ShoppingCart");
                }

                return View(model);
            }

            var postProcessPaymentRequest = new PostProcessPaymentRequest
            {
                Order = placeOrderResult.PlacedOrder
            };

            try
            {
                await _paymentService.PostProcessPaymentAsync(postProcessPaymentRequest);
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }
            finally
            {
                HttpContext.Session.TrySetObject<ProcessPaymentRequest>("OrderPaymentInfo", null);
                _checkoutStateAccessor.Abandon();
            }

            if (postProcessPaymentRequest.RedirectUrl.HasValue())
            {
                return Redirect(postProcessPaymentRequest.RedirectUrl);
            }

            return RedirectToAction(nameof(Completed));
        }

        public async Task<IActionResult> Completed()
        {
            var customer = Services.WorkContext.CurrentCustomer;

            if (customer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
            {
                return new UnauthorizedResult();
            }

            var store = Services.StoreContext.CurrentStore;

            var order = await _db.Orders
                .AsNoTracking()
                .Include(x => x.Customer)
                .ApplyStandardFilter(customer.Id, store.Id)
                .FirstOrDefaultAsync();

            if (order == null || customer.Id != order.CustomerId)
            {
                return NotFound();
            }

            // Disable "order completed" page?
            if (_orderSettings.DisableOrderCompletedPage)
            {
                return RedirectToAction(nameof(OrderController.Details), "Order", new { id = order.Id });
            }

            var model = new CheckoutCompletedModel
            {
                OrderId = order.Id,
                OrderNumber = order.GetOrderNumber(),
                Order = order
            };

            return View(model);
        }
    }
}