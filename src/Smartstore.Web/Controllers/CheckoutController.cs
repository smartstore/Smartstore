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
                Fee = "5€",
                FeeRaw = 5m,
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
                if (referrer.EndsWith("/Confirm"))
                {
                    return RedirectToAction(nameof(ShippingMethod));
                }

                // TODO: (ms) (core) Implement missing action Confirm
                return RedirectToAction("Confirm");
            }

            return View(model);
        }
    }
}