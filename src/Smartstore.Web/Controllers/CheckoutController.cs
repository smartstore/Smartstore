using Microsoft.AspNetCore.Http;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Seo.Routing;
using Smartstore.Core.Stores;
using Smartstore.Web.Models.Checkout;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Controllers
{
    public class CheckoutController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly ICheckoutWorkflow _checkoutWorkflow;
        private readonly IPaymentService _paymentService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly OrderSettings _orderSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public CheckoutController(
            SmartDbContext db,
            IStoreContext storeContext,
            IWorkContext workContext,
            ICheckoutWorkflow checkoutWorkflow,
            IPaymentService paymentService,
            IShoppingCartService shoppingCartService,
            ICheckoutStateAccessor checkoutStateAccessor,
            OrderSettings orderSettings,
            ShoppingCartSettings shoppingCartSettings)
        {
            _db = db;
            _storeContext = storeContext;
            _workContext = workContext;
            _checkoutWorkflow = checkoutWorkflow;
            _paymentService = paymentService;
            _shoppingCartService = shoppingCartService;
            _checkoutStateAccessor = checkoutStateAccessor;
            _orderSettings = orderSettings;
            _shoppingCartSettings = shoppingCartSettings;
        }

        [DisallowRobot]
        [LocalizedRoute("/checkout", Name = "Checkout")]
        public async Task<IActionResult> Index()
        {
            var result = await _checkoutWorkflow.StartAsync(await CreateCheckoutContext());

            return result.ActionResult ?? RedirectToRoute("ShoppingCart");
        }

        public async Task<IActionResult> BillingAddress()
        {
            var context = await CreateCheckoutContext();
            var result = await _checkoutWorkflow.ProcessAsync(context);
            if (result.ActionResult != null)
            {
                return result.ActionResult;
            }

            var model = await context.MapAddressesAsync(false);

            return View(result.ViewPath, model);
        }

        [HttpPost]
        public async Task<IActionResult> SelectBillingAddress(int addressId)
        {
            var result = await _checkoutWorkflow.AdvanceAsync(await CreateCheckoutContext(addressId));

            return result.ActionResult ?? RedirectToAction(nameof(BillingAddress));
        }

        [HttpPost, ActionName(CheckoutActionNames.BillingAddress)]
        [FormValueRequired("nextstep")]
        public async Task<IActionResult> NewBillingAddress(CheckoutAddressModel model)
        {
            var context = await CreateCheckoutContext();
            var result = await AddAddress(model, context, false);
            if (result.ActionResult != null)
            {
                return result.ActionResult;
            }

            model = await context.MapAddressesAsync(false);

            return View(model);
        }

        [HttpPost, ActionName(CheckoutActionNames.ShippingAddress)]
        [FormValueRequired("nextstep")]
        public async Task<IActionResult> NewShippingAddress(CheckoutAddressModel model)
        {
            var context = await CreateCheckoutContext();
            var result = await AddAddress(model, context, true);
            if (result.ActionResult != null)
            {
                return result.ActionResult;
            }

            model = await context.MapAddressesAsync(true);

            return View(model);
        }

        private async Task<CheckoutResult> AddAddress(CheckoutAddressModel model, CheckoutContext context, bool isShippingAddress)
        {
            var cart = context.Cart;
            var customer = cart.Customer;
            var ga = customer.GenericAttributes;

            if (!cart.HasItems)
            {
                return new(RedirectToRoute("ShoppingCart"));
            }

            if (!_orderSettings.AnonymousCheckoutAllowed && !customer.IsRegistered())
            {
                return new(ChallengeOrForbid());
            }

            if (ModelState.IsValid)
            {
                var address = await MapperFactory.MapAsync<AddressModel, Address>(model.NewAddress);
                customer.Addresses.Add(address);

                // Save to avoid duplicate addresses.
                await _db.SaveChangesAsync();

                if (isShippingAddress)
                {
                    customer.ShippingAddress = address;
                    if (_shoppingCartSettings.QuickCheckoutEnabled)
                    {
                        ga.DefaultShippingAddressId = customer.ShippingAddress.Id;
                    }
                }
                else
                {
                    customer.BillingAddress = address;
                    customer.ShippingAddress = model.ShippingAddressDiffers || !cart.IsShippingRequired ? null : address;

                    var state = _checkoutStateAccessor.CheckoutState;
                    state.CustomProperties["SkipShippingAddress"] = !model.ShippingAddressDiffers;
                    state.CustomProperties["ShippingAddressDiffers"] = model.ShippingAddressDiffers;

                    if (_shoppingCartSettings.QuickCheckoutEnabled)
                    {
                        ga.DefaultBillingAddressId = customer.BillingAddress.Id;
                        if (customer.ShippingAddress != null)
                        {
                            ga.DefaultShippingAddressId = customer.ShippingAddress.Id;
                        }
                    }
                }

                await _db.SaveChangesAsync();

                var result = await _checkoutWorkflow.AdvanceAsync(context);
                result.ActionResult ??= RedirectToAction(isShippingAddress ? nameof(ShippingMethod) : nameof(ShippingAddress));

                return result;
            }

            return null;
        }

        public async Task<IActionResult> ShippingAddress()
        {
            var context = await CreateCheckoutContext();
            var result = await _checkoutWorkflow.ProcessAsync(context);
            if (result.ActionResult != null)
            {
                return result.ActionResult;
            }

            var model = await context.MapAddressesAsync(true);

            return View(result.ViewPath, model);
        }

        [HttpPost]
        public async Task<IActionResult> SelectShippingAddress(int addressId)
        {
            var result = await _checkoutWorkflow.AdvanceAsync(await CreateCheckoutContext(addressId));

            return result.ActionResult ?? RedirectToAction(nameof(ShippingAddress));
        }

        public async Task<IActionResult> ShippingMethod()
        {
            var context = await CreateCheckoutContext();
            var result = await _checkoutWorkflow.ProcessAsync(context);
            if (result.ActionResult != null)
            {
                return result.ActionResult;
            }

            var model = await MapperFactory.MapAsync<CheckoutContext, CheckoutShippingMethodModel>(context);

            result.Errors.Each(x => model.Warnings.Add(x.ErrorMessage));

            return View(result.ViewPath, model);
        }

        [HttpPost, ActionName(CheckoutActionNames.ShippingMethod)]
        [FormValueRequired("nextstep")]
        public async Task<IActionResult> SelectShippingMethod(string shippingOption)
        {
            var result = await _checkoutWorkflow.AdvanceAsync(await CreateCheckoutContext(shippingOption));

            result.Errors.Take(3).Each(x => NotifyError(x.ErrorMessage));

            return result.ActionResult ?? RedirectToAction(nameof(ShippingMethod));
        }

        public async Task<IActionResult> PaymentMethod()
        {
            var context = await CreateCheckoutContext();
            var result = await _checkoutWorkflow.ProcessAsync(context);
            if (result.ActionResult != null)
            {
                return result.ActionResult;
            }

            var model = await MapperFactory.MapAsync<CheckoutContext, CheckoutPaymentMethodModel>(context);

            return View(result.ViewPath, model);
        }

        [HttpPost, ActionName(CheckoutActionNames.PaymentMethod)]
        [FormValueRequired("nextstep")]
        public async Task<IActionResult> SelectPaymentMethod(string paymentMethod, IFormCollection form)
        {
            var result = await _checkoutWorkflow.AdvanceAsync(await CreateCheckoutContext(paymentMethod));

            result.Errors.Each(x => ModelState.AddModelError(x.PropertyName, x.ErrorMessage));

            if (!ModelState.IsValid)
            {
                return await PaymentMethod();
            }

            return result.ActionResult ?? RedirectToAction(nameof(PaymentMethod));
        }

        [HttpPost]
        public async Task<IActionResult> PaymentInfoAjax(string paymentMethodSystemName)
        {
            if (!_orderSettings.AnonymousCheckoutAllowed && !_workContext.CurrentCustomer.IsRegistered())
            {
                return new EmptyResult();
            }

            var paymentMethod = await _paymentService.LoadPaymentProviderBySystemNameAsync(paymentMethodSystemName);
            if (paymentMethod == null)
            {
                return new NotFoundResult();
            }

            var infoWidget = paymentMethod.Value.GetPaymentInfoWidget();
            if (infoWidget == null)
            {
                return new EmptyResult();
            }

            try
            {
                var widgetContent = await infoWidget.InvokeAsync(new WidgetContext(ControllerContext));
                return Content(widgetContent.ToHtmlString().ToString());
            }
            catch (Exception ex)
            {
                // Log all but do not display inner exceptions.
                Logger.Error(ex);
                NotifyError(ex.Message);

                return new EmptyResult();
            }
        }

        public async Task<IActionResult> Confirm()
        {
            var context = await CreateCheckoutContext();
            var result = await _checkoutWorkflow.ProcessAsync(context);
            if (result.ActionResult != null)
            {
                return result.ActionResult;
            }

            var model = await MapperFactory.MapAsync<CheckoutContext, CheckoutConfirmModel>(context);

            return View(result.ViewPath, model);
        }

        [HttpPost, ActionName(CheckoutActionNames.Confirm)]
        public async Task<IActionResult> ConfirmOrder()
        {
            var result = await _checkoutWorkflow.CompleteAsync(await CreateCheckoutContext());

            if (result.Errors.Length > 0)
            {
                var model = new CheckoutConfirmModel();

                result.Errors.Each(x => model.Warnings.Add(x.ErrorMessage));

                return View(model);
            }

            return result.ActionResult ?? RedirectToAction(nameof(Confirm));
        }

        public async Task<IActionResult> Completed()
        {
            var store = _storeContext.CurrentStore;
            var customer = _workContext.CurrentCustomer;

            if (!_orderSettings.AnonymousCheckoutAllowed && !_workContext.CurrentCustomer.IsRegistered())
            {
                return ChallengeOrForbid();
            }

            var order = await _db.Orders
                .AsNoTracking()
                .Include(x => x.Customer)
                .ApplyStandardFilter(customer.Id, store.Id)
                .FirstOrDefaultAsync();

            if (order == null || customer.Id != order.CustomerId)
            {
                return NotFound();
            }

            if (_orderSettings.DisableOrderCompletedPage)
            {
                return RedirectToAction(nameof(OrderController.Details), "Order", new { id = order.Id });
            }

            return View(new CheckoutCompletedModel
            {
                OrderId = order.Id,
                OrderNumber = order.GetOrderNumber(),
                Order = order
            });
        }

        private async Task<CheckoutContext> CreateCheckoutContext(object model = null)
        {
            var cart = await _shoppingCartService.GetCartAsync(storeId: _storeContext.CurrentStore.Id);
            
            return new(cart, HttpContext, Url)
            {
                Model = model
            };
        }
    }
}