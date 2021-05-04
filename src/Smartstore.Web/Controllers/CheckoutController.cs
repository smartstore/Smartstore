using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Cart.Events;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Localization.Routing;
using Smartstore.Engine.Modularity;
using Smartstore.Utilities.Html;
using Smartstore.Web.Models.Checkout;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.ShoppingCart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smartstore.Web.Controllers
{
    public class CheckoutController : PublicControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IShippingService _shippingService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ShippingSettings _shippingSettings;
        private readonly PaymentSettings _paymentSettings;
        private readonly OrderSettings _orderSettings;

        public CheckoutController(
            SmartDbContext db,
            IOrderService orderService,
            IPaymentService paymentService,
            IShippingService shippingService,
            IShoppingCartService shoppingCartService,
            IOrderProcessingService orderProcessingService,
            IOrderCalculationService orderCalculationService,
            IShoppingCartValidator shoppingCartValidator,
            ShippingSettings shippingSettings,
            PaymentSettings paymentSettings,
            OrderSettings orderSettings)
        {
            _db = db;
            _orderService = orderService;
            _paymentService = paymentService;
            _shippingService = shippingService;
            _shoppingCartService = shoppingCartService;
            _orderProcessingService = orderProcessingService;
            _orderCalculationService = orderCalculationService;
            _shoppingCartValidator = shoppingCartValidator;
            _shippingSettings = shippingSettings;
            _paymentSettings = paymentSettings;
            _orderSettings = orderSettings;
        }

        [NonAction]
        protected async Task<CheckoutAddressModel> PrepareCheckoutAddressModelAsync(bool shipping = false, int? selectedCountryId = null)
        {
            // Get existing addresses.
            var customer = Services.WorkContext.CurrentCustomer;
            var addresses = new List<Address>(customer.Addresses).AsEnumerable();

            // And map billing / shipping addresses.
            var model = new CheckoutAddressModel();
            await addresses.MapAsync(model, shipping, selectedCountryId);

            return model;
        }

        [LocalizedRoute("/checkout", Name = "Checkout")]
        public async Task<IActionResult> Index()
        {
            var storeId = Services.StoreContext.CurrentStore.Id;
            var customer = Services.WorkContext.CurrentCustomer;
            var cart = await _shoppingCartService.GetCartItemsAsync(storeId: storeId);

            if (!cart.Any())
            {
                return RedirectToRoute("ShoppingCart");
            }

            if (customer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
            {
                return new UnauthorizedResult();
            }

            customer.ResetCheckoutData(storeId);

            // Validate checkout attributes.
            var checkoutAttributes = customer.GenericAttributes.CheckoutAttributes;

            var warnings = new List<string>();
            var isValid = await _shoppingCartValidator.ValidateCartItemsAsync(cart, warnings, true, checkoutAttributes);
            if (!isValid)
            {
                NotifyWarning(string.Join(Environment.NewLine, warnings.Take(3)));
                return RedirectToRoute("ShoppingCart");
            }

            var validatingCartEvent = new ValidatingCartEvent(cart, warnings, customer);
            await Services.EventPublisher.PublishAsync(validatingCartEvent);

            if (validatingCartEvent.Result != null)
            {
                return validatingCartEvent.Result;
            }

            if (warnings.Any())
            {
                NotifyWarning(string.Join(Environment.NewLine, warnings.Take(3)));
                return RedirectToRoute("ShoppingCart");
            }

            // Validate each shopping cart item.
            foreach (var cartItem in cart)
            {
                var ctx = new AddToCartContext
                {
                    StoreId = storeId,
                    Product = cartItem.Item.Product,
                    BundleItem = cartItem.Item.BundleItem,
                    ChildItems = cartItem.ChildItems.Select(x => x.Item).ToList()
                };

                isValid = await _shoppingCartValidator.ValidateAddToCartItemAsync(ctx, cartItem.Item, cart);
                if (!isValid)
                {
                    warnings.AddRange(ctx.Warnings);
                    NotifyWarning(string.Join(Environment.NewLine, warnings.Take(3)));
                    return RedirectToRoute("ShoppingCart");
                }
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(BillingAddress));
        }

        public async Task<IActionResult> BillingAddress()
        {
            var cart = await _shoppingCartService.GetCartItemsAsync(storeId: Services.StoreContext.CurrentStore.Id);

            if (cart.Count == 0)
            {
                return RedirectToRoute("ShoppingCart");
            }

            if (Services.WorkContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
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

        public async Task<IActionResult> ShippingAddress()
        {
            var cart = await _shoppingCartService.GetCartItemsAsync(storeId: Services.StoreContext.CurrentStore.Id);

            if (!cart.Any())
            {
                return RedirectToRoute("ShoppingCart");
            }

            var customer = Services.WorkContext.CurrentCustomer;

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
            var cart = await _shoppingCartService.GetCartItemsAsync(storeId: storeId);

            if (cart.Count == 0)
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

            // TODO: (ms) (core) Wait with implementation until any provider for shipping rate computation has been implemented.
            //var response = _shippingService.GetShippingOptions(cart, customer.ShippingAddress, storeId: storeId);
            //var options = response.ShippingOptions;
            //var state = HttpContext.GetCheckoutState();

            //if (state.CustomProperties.ContainsKey("HasOnlyOneActiveShippingMethod"))
            //{
            //    state.CustomProperties["HasOnlyOneActiveShippingMethod"] = options.Count == 1;
            //}
            //else
            //{
            //    state.CustomProperties.Add("HasOnlyOneActiveShippingMethod", options.Count == 1);
            //}

            //if (options.Count <= 1 && _shippingSettings.SkipShippingIfSingleOption && response.Success)
            //{
            //    customer.GenericAttributes.SelectedShippingOption = options.FirstOrDefault();
            //    await customer.GenericAttributes.SaveChangesAsync();

            //    var referrer = Services.WebHelper.GetUrlReferrer();
            //    if (referrer.EndsWith("/PaymentMethod") || referrer.EndsWith("/Confirm"))
            //    {
            //        return RedirectToAction(nameof(ShippingAddress));
            //    }

            //    return RedirectToAction(nameof(PaymentMethod));
            //}

            var model = new CheckoutShippingMethodModel();
            await cart.AsEnumerable().MapAsync(model);

            // TODO: (ms) (core) Remove dummy shipping method model.
            // This creates a dummy ShippingMethodModel. It is needed as long as no other shipping method is implemented.
            var methodModel = new CheckoutShippingMethodModel.ShippingMethodModel
            {
                ShippingMethodId = 100,
                ShippingRateComputationMethodSystemName = "TestDummy",
                Name = "Test",
                BrandUrl = "test.de",
                Description = "Dies ist eine dummy shipping method",
                Fee = new(5, Services.StoreContext.CurrentStore.PrimaryStoreCurrency),
                Selected = true
            };

            model.ShippingMethods.Add(methodModel);

            return View(model);
        }

        [HttpPost, ActionName("ShippingMethod")]
        [FormValueRequired("nextstep")]
        public async Task<IActionResult> SelectShippingMethod(string shippingOption)
        {
            var storeId = Services.StoreContext.CurrentStore.Id;

            var cart = await _shoppingCartService.GetCartItemsAsync(storeId: storeId);
            if (!cart.Any())
            {
                return RedirectToRoute("ShoppingCart");
            }

            var customer = Services.WorkContext.CurrentCustomer;

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
                // TODO: (ms) (core) Remove dummy shipping option if at least one shipping option has been implemented.
                // Shipping option was not found in customer attributes. Load via shipping service.
                //shippingOptions = _shippingService
                //    .GetShippingOptions(cart, customer.ShippingAddress, shippingRateComputationMethodSystemName, storeId)
                //    .ShippingOptions
                //    .ToList();
                shippingOptions = new List<ShippingOption>
                {
                    new ShippingOption()
                    {
                        Name = "Test",
                        Description = "This is dummy shipping option since no other shipping options are implemented yet",
                        Rate = new(5m, Services.WorkContext.WorkingCurrency),
                        ShippingMethodId = -100,
                        ShippingRateComputationMethodSystemName = "Dummy"
                    }
                };
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

            var cart = await _shoppingCartService.GetCartItemsAsync(storeId: storeId);
            if (!cart.Any())
            {
                return RedirectToRoute("ShoppingCart");
            }

            if (!_orderSettings.AnonymousCheckoutAllowed && customer.IsGuest())
            {
                return new UnauthorizedResult();
            }

            // Check whether payment workflow is required. We ignore reward points during cart total calculation.
            Money? shoppingCartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(cart, false);
            var isPaymentWorkflowRequired = shoppingCartTotal.GetValueOrDefault() != decimal.Zero;

            var model = new CheckoutPaymentMethodModel();
            await cart.AsEnumerable().MapAsync(model);
            model.PaymentMethods.Add(new CheckoutPaymentMethodModel.PaymentMethodModel()
            {
                Name = "Super dummy payment",
                PaymentMethodSystemName = "Super dummy payments",
                Selected = true,
                BrandUrl = "test.de",
                Description = "This is a test payment method.",
                FullDescription = new("This is a test payment methods full description.", Services.WorkContext.WorkingLanguage, Services.WorkContext.WorkingLanguage),
                Fee = new(5, Services.StoreContext.CurrentStore.PrimaryStoreCurrency),
                RequiresInteraction = true,
            });

            var onlyOnePassiveMethod = model.PaymentMethods.Count == 1 && !model.PaymentMethods[0].RequiresInteraction;

            var checkoutState = HttpContext.GetCheckoutState();
            checkoutState.CustomProperties["HasOnlyOneActivePaymentMethod"] = model.PaymentMethods.Count == 1;
            checkoutState.IsPaymentSelectionSkipped = !isPaymentWorkflowRequired || _paymentSettings.BypassPaymentMethodSelectionIfOnlyOne && onlyOnePassiveMethod;

            if (checkoutState.IsPaymentSelectionSkipped)
            {
                // If there is nothing to pay for OR if only one passive payment method is active and Reward Points are disabled
                // OR the current customer has no Reward Points, then the customer does not need to select a payment method.

                customer.GenericAttributes.SelectedPaymentMethod = model.PaymentMethods?.FirstOrDefault()?.PaymentMethodSystemName;
                await customer.GenericAttributes.SaveChangesAsync();

                var referrer = Services.WebHelper.GetUrlReferrer();
                if (referrer.EndsWith("/" + nameof(Confirm)))
                {
                    return RedirectToAction(nameof(ShippingMethod));
                }

                return RedirectToAction(nameof(Confirm));
            }

            return View(model);
        }

        [HttpPost, ActionName("PaymentMethod")]
        [FormValueRequired("nextstep")]
        public async Task<IActionResult> SelectPaymentMethod(string paymentMethod, CheckoutPaymentMethodModel model)
        {            
            var storeId = Services.StoreContext.CurrentStore.Id;
            var customer = Services.WorkContext.CurrentCustomer;

            var cart = await _shoppingCartService.GetCartItemsAsync(storeId: storeId);
            if (!cart.Any())
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

            // TODO: (ms) (core) Implement load payment method provider if at least one payment method provider is implemented.
            //var paymentMethodProvider = await _paymentService.LoadPaymentMethodBySystemNameAsync(paymentMethod, true, storeId);
            //if (paymentMethodProvider == null)
            //{
            //    return RedirectToAction(nameof(PaymentMethod));
            //}

            //// Save
            //customer.GenericAttributes.SelectedPaymentMethod = paymentMethod;
            //await customer.GenericAttributes.SaveChangesAsync();


            //// Calidate info
            //if (!IsValidPaymentForm(paymentMethodProvider.Value, form))
            //{
            //    return RedirectToAction(nameof(PaymentMethod));
            //}

            //// Save payment data so that the user must not re-enter it
            //form.CopyTo(HttpContext.GetCheckoutState().PaymentData, true);

            return RedirectToAction(nameof(Confirm));
        }

        [HttpPost]
        public async Task<IActionResult> PaymentInfoAjax(string paymentMethodSystemName)
        {
            if (Services.WorkContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
            {
                return Content(string.Empty);
            }

            if (paymentMethodSystemName.IsEmpty())
            {
                return new StatusCodeResult(404);
            }

            // TODO: (ms) (core) Wait until any payment method has been implemented. Remove true from if clause.
            var paymentMethod = await _paymentService.LoadPaymentMethodBySystemNameAsync(paymentMethodSystemName);
            if (paymentMethod == null)
            {
                return new StatusCodeResult(404);
            }

            var infoWidget = paymentMethod.Value.GetPaymentInfoWidget();
            if (infoWidget == null)
            {
                return Content(string.Empty);
            }

            // TODO: (ms) (core) Wait for mc implementation of WidgetInvoker.Invoke for action context.
            // Return as string and render with Content.
            //var xx = await infoWidget.InvokeAsync(ControllerContext);

            return Content(string.Empty);
        }

        public async Task<IActionResult> Confirm()
        {
            var cart = await _shoppingCartService.GetCartItemsAsync(storeId: Services.StoreContext.CurrentStore.Id);
            if (!cart.Any())
            {
                return RedirectToRoute("ShoppingCart");
            }

            if (Services.WorkContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
            {
                return new UnauthorizedResult();
            }

            var model = new CheckoutConfirmModel();
            await cart.AsEnumerable().MapAsync(model);

            return View(model);
        }

        [HttpPost, ActionName("Confirm")]
        public async Task<IActionResult> ConfirmOrder()
        {
            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var cart = await _shoppingCartService.GetCartItemsAsync(storeId: store.Id);

            if (!cart.Any())
            {
                return RedirectToRoute("ShoppingCart");
            }

            if (customer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
            {
                return new UnauthorizedResult();
            }

            var warnings = new List<string>();
            var validatingCartEvent = new ValidatingCartEvent(cart, warnings, customer);
            await Services.EventPublisher.PublishAsync(validatingCartEvent);

            if (validatingCartEvent.Result != null)
            {
                return validatingCartEvent.Result;
            }

            if (warnings.Any())
            {
                NotifyWarning(string.Join(Environment.NewLine, warnings.Take(3)));
                return RedirectToRoute("ShoppingCart");
            }

            var model = new CheckoutConfirmModel();
            OrderPlacementResult placeOrderResult = null;
            PostProcessPaymentRequest postProcessPaymentRequest = null;

            try
            {
                HttpContext.Session.TryGetObject<ProcessPaymentRequest>("OrderPaymentInfo", out var processPaymentRequest);
                if (processPaymentRequest == null)
                {
                    // Check whether payment workflow is required.
                    var cartTotalBase = await _orderCalculationService.GetShoppingCartTotalAsync(cart, false);

                    if (cartTotalBase.Total.GetValueOrDefault() != decimal.Zero && !HttpContext.GetCheckoutState().IsPaymentSelectionSkipped)
                    {
                        return RedirectToAction("PaymentMethod");
                    }

                    processPaymentRequest = new ProcessPaymentRequest();
                }

                // Prevent 2 orders being placed within an X seconds time frame.
                if (!await _orderProcessingService.IsMinimumOrderPlacementIntervalValidAsync(customer, store))
                {
                    throw new Exception(T("Checkout.MinOrderPlacementInterval"));
                }

                // Place order.
                processPaymentRequest.StoreId = store.Id;
                processPaymentRequest.CustomerId = customer.Id;
                processPaymentRequest.PaymentMethodSystemName = customer.GenericAttributes.SelectedPaymentMethod;

                var placeOrderExtraData = new Dictionary<string, string>
                {
                    ["CustomerComment"] = HttpContext.Request.Form["customercommenthidden"],
                    ["SubscribeToNewsLetter"] = HttpContext.Request.Form["SubscribeToNewsLetter"],
                    ["AcceptThirdPartyEmailHandOver"] = HttpContext.Request.Form["AcceptThirdPartyEmailHandOver"]
                };

                placeOrderResult = await _orderProcessingService.PlaceOrderAsync(processPaymentRequest, placeOrderExtraData);

                if (!placeOrderResult.Success)
                {
                    model.Warnings.AddRange(placeOrderResult.Errors.Select(x => HtmlUtils.ConvertPlainTextToHtml(x)));
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, ex.Message);

                if (!model.Warnings.Any(x => x == ex.Message))
                {
                    model.Warnings.Add(ex.Message);
                }
            }

            if (placeOrderResult == null || !placeOrderResult.Success || model.Warnings.Any())
            {
                return View(model);
            }

            try
            {
                postProcessPaymentRequest = new PostProcessPaymentRequest
                {
                    Order = placeOrderResult.PlacedOrder
                };

                await _paymentService.PostProcessPaymentAsync(postProcessPaymentRequest);
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }
            finally
            {
                HttpContext.Session.TrySetObject<ProcessPaymentRequest>("OrderPaymentInfo", null);
                HttpContext.Session.TryRemove(CheckoutState.CheckoutStateSessionKey);
            }

            if (postProcessPaymentRequest != null && postProcessPaymentRequest.RedirectUrl.HasValue())
            {
                return Redirect(postProcessPaymentRequest.RedirectUrl);
            }

            return RedirectToAction("Completed");
        }
    }
}