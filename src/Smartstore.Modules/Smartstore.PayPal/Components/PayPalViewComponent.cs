using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Components;
using Smartstore.PayPal.Settings;
using Smartstore.PayPal.Models;
using Smartstore.Core.Checkout.Orders;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.PayPal.Components
{
    public class PayPalViewComponent : SmartViewComponent
    {
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly PayPalSettings _settings;
        
        public PayPalViewComponent(
            IShoppingCartService shoppingCartService,
            IOrderCalculationService orderCalculationService, 
            PayPalSettings settings)
        {
            _shoppingCartService = shoppingCartService;
            _orderCalculationService = orderCalculationService;
            _settings = settings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var controller = HttpContext.Request.RouteValues.GetControllerName().EmptyNull();
            var action = HttpContext.Request.RouteValues.GetActionName().EmptyNull();
            var isPaymentSelectionPage = controller == "Checkout" && action == "PaymentMethod";

            if (isPaymentSelectionPage)
            {
                return Empty();
            }

            var cart = await _shoppingCartService.GetCartAsync(Services.WorkContext.CurrentCustomer, ShoppingCartType.ShoppingCart, Services.StoreContext.CurrentStore.Id);
            var cartSubTotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart);
            var model = new PublicPaymentMethodModel
            {
                Intent = _settings.Intent,
                Amount = cartSubTotal.SubtotalWithDiscount.Amount,
                IsPaymentSelection = isPaymentSelectionPage
            };

            return View(model);
        }
    }
}