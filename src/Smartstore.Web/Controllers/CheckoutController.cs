using Microsoft.AspNetCore.Http;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Seo.Routing;
using Smartstore.Core.Stores;
using Smartstore.Web.Models.Cart;
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

        public CheckoutController(
            SmartDbContext db,
            IStoreContext storeContext,
            IWorkContext workContext,
            ICheckoutWorkflow checkoutWorkflow,
            IPaymentService paymentService,
            IShoppingCartService shoppingCartService,
            ICheckoutStateAccessor checkoutStateAccessor,
            OrderSettings orderSettings)
        {
            _db = db;
            _storeContext = storeContext;
            _workContext = workContext;
            _checkoutWorkflow = checkoutWorkflow;
            _paymentService = paymentService;
            _shoppingCartService = shoppingCartService;
            _checkoutStateAccessor = checkoutStateAccessor;
            _orderSettings = orderSettings;
        }

        [DisallowRobot]
        [LocalizedRoute("/checkout", Name = "Checkout")]
        public async Task<IActionResult> Index()
        {
            var result = await _checkoutWorkflow.StartAsync(await CreateContext());

            return result.ActionResult ?? RedirectToRoute("ShoppingCart");
        }

        public async Task<IActionResult> BillingAddress()
        {
            var result = await _checkoutWorkflow.ProcessAsync(await CreateContext());
            if (result.ActionResult != null)
            {
                return result.ActionResult;
            }

            return View(await _workContext.CurrentCustomer.Addresses.MapAsync(false));
        }

        [HttpPost]
        public async Task<IActionResult> SelectBillingAddress(int addressId)
        {
            var result = await _checkoutWorkflow.AdvanceAsync(await CreateContext(addressId));

            return result.ActionResult ?? RedirectToAction(nameof(BillingAddress));
        }

        [HttpPost, ActionName("BillingAddress")]
        [FormValueRequired("nextstep")]
        public async Task<IActionResult> NewBillingAddress(CheckoutAddressModel model)
        {
            return await AddAddress(model, false) 
                ?? View(await _workContext.CurrentCustomer.Addresses.MapAsync(false));
        }

        [HttpPost, ActionName("ShippingAddress")]
        [FormValueRequired("nextstep")]
        public async Task<IActionResult> NewShippingAddress(CheckoutAddressModel model)
        {
            return await AddAddress(model, true) 
                ?? View(await _workContext.CurrentCustomer.Addresses.MapAsync(true));
        }

        private async Task<IActionResult> AddAddress(CheckoutAddressModel model, bool isShippingAddress)
        {
            var context = await CreateContext();
            var cart = context.Cart;

            if (!cart.HasItems)
            {
                return RedirectToRoute("ShoppingCart");
            }

            if (!_orderSettings.AnonymousCheckoutAllowed && !cart.Customer.IsRegistered())
            {
                return ChallengeOrForbid();
            }

            if (ModelState.IsValid)
            {
                var address = await MapperFactory.MapAsync<AddressModel, Address>(model.NewAddress);
                cart.Customer.Addresses.Add(address);

                // Save to avoid duplicate addresses.
                await _db.SaveChangesAsync();

                if (isShippingAddress)
                {
                    cart.Customer.ShippingAddress = address;
                }
                else
                {
                    cart.Customer.BillingAddress = address;
                    cart.Customer.ShippingAddress = model.ShippingAddressDiffers || !cart.IsShippingRequired() ? null : address;

                    _checkoutStateAccessor.CheckoutState.CustomProperties["SkipShippingAddress"] = !model.ShippingAddressDiffers;
                }

                await _db.SaveChangesAsync();

                var result = await _checkoutWorkflow.AdvanceAsync(context);

                return result.ActionResult ?? RedirectToAction(isShippingAddress ? nameof(ShippingMethod) : nameof(ShippingAddress));
            }

            return null;
        }

        public async Task<IActionResult> ShippingAddress()
        {
            var result = await _checkoutWorkflow.ProcessAsync(await CreateContext());
            if (result.ActionResult != null)
            {
                return result.ActionResult;
            }

            return View(await _workContext.CurrentCustomer.Addresses.MapAsync(true));
        }

        [HttpPost]
        public async Task<IActionResult> SelectShippingAddress(int addressId)
        {
            var result = await _checkoutWorkflow.AdvanceAsync(await CreateContext(addressId));

            return result.ActionResult ?? RedirectToAction(nameof(ShippingAddress));
        }

        public async Task<IActionResult> ShippingMethod()
        {
            var result = await _checkoutWorkflow.ProcessAsync(await CreateContext());
            if (result.ActionResult != null)
            {
                return result.ActionResult;
            }

            var cart = await _shoppingCartService.GetCartAsync(storeId: _storeContext.CurrentStore.Id);
            var model = await MapperFactory.MapAsync<ShoppingCart, CheckoutShippingMethodModel>(cart);

            result.Errors.Each(x => model.Warnings.Add(x.ErrorMessage));

            return View(model);
        }

        [HttpPost, ActionName("ShippingMethod")]
        [FormValueRequired("nextstep")]
        public async Task<IActionResult> SelectShippingMethod(string shippingOption)
        {
            var result = await _checkoutWorkflow.AdvanceAsync(await CreateContext(shippingOption));

            result.Errors.Take(3).Each(x => NotifyError(x.ErrorMessage));

            return result.ActionResult ?? RedirectToAction(nameof(ShippingMethod));
        }

        public async Task<IActionResult> PaymentMethod()
        {
            var result = await _checkoutWorkflow.ProcessAsync(await CreateContext());
            if (result.ActionResult != null)
            {
                return result.ActionResult;
            }

            var cart = await _shoppingCartService.GetCartAsync(storeId: _storeContext.CurrentStore.Id);
            var model = await MapperFactory.MapAsync<ShoppingCart, CheckoutPaymentMethodModel>(cart);

            return View(model);
        }

        [HttpPost, ActionName("PaymentMethod")]
        [FormValueRequired("nextstep")]
        public async Task<IActionResult> SelectPaymentMethod(string paymentMethod, IFormCollection form)
        {
            var result = await _checkoutWorkflow.AdvanceAsync(await CreateContext(paymentMethod));

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
            var context = await CreateContext();
            var result = await _checkoutWorkflow.ProcessAsync(context);
            if (result.ActionResult != null)
            {
                return result.ActionResult;
            }

            var model = await MapperFactory.MapAsync<ShoppingCart, CheckoutConfirmModel>(context.Cart);

            return View(model);
        }

        [HttpPost, ActionName("Confirm")]
        public async Task<IActionResult> ConfirmOrder()
        {
            var result = await _checkoutWorkflow.CompleteAsync(await CreateContext());

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

        private async Task<CheckoutContext> CreateContext(object model = null)
        {
            var cart = await _shoppingCartService.GetCartAsync(storeId: _storeContext.CurrentStore.Id);
            
            return new(HttpContext, cart)
            {
                Model = model
            };
        }
    }
}