using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Web.Components;

namespace Smartstore.PayPal.Components
{
    public class PayPalViewComponent : SmartViewComponent
    {
        private readonly ICommonServices _services;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly PayPalSettings _settings;

        public PayPalViewComponent(
            ICommonServices services,
            IShoppingCartService shoppingCartService,
            IOrderCalculationService orderCalculationService,
            PayPalSettings settings)
        {
            _services = services;
            _shoppingCartService = shoppingCartService;
            _orderCalculationService = orderCalculationService;
            _settings = settings;
        }

        /// <summary>
        /// Renders PayPal button widget.
        /// </summary>
        /// <param name="isPaymentInfoInvoker">Defines whether the widget is invoked from payment method's GetPaymentInfoWidget.</param>
        /// /// <param name="isSelected">Defines whether the payment method is selected on page load.</param>
        public async Task<IViewComponentResult> InvokeAsync(bool isPaymentInfoInvoker, bool isSelected)
        {
            // If client id or secret haven't been configured yet, don't render button.
            if (!_settings.ClientId.HasValue() || !_settings.Secret.HasValue())
            {
                return Empty();
            }

            var controller = HttpContext.Request.RouteValues.GetControllerName().EmptyNull();
            var action = HttpContext.Request.RouteValues.GetActionName().EmptyNull();
            var isPaymentSelectionPage = controller == "Checkout" && action == "PaymentMethod";

            if (isPaymentSelectionPage && isPaymentInfoInvoker)
            {
                return Empty();
            }

            var cart = await _shoppingCartService.GetCartAsync(Services.WorkContext.CurrentCustomer, ShoppingCartType.ShoppingCart, Services.StoreContext.CurrentStore.Id);
            var cartSubTotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart);
            var model = new PublicPaymentMethodModel
            {
                Intent = _settings.Intent.ToString().ToLower(),
                Amount = cartSubTotal.SubtotalWithDiscount.Amount,
                IsPaymentSelection = isPaymentSelectionPage,
                ButtonColor = _settings.ButtonColor,
                ButtonShape = _settings.ButtonShape,
                IsSelectedMethod = isSelected
            };

            return View(model);
        }
    }
}