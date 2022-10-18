using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Web.Components;

namespace Smartstore.PayPal.Components
{
    public class PayPalViewComponent : SmartViewComponent
    {
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly PayPalSettings _settings;

        public PayPalViewComponent(IShoppingCartService shoppingCartService, IOrderCalculationService orderCalculationService, PayPalSettings settings)
        {
            _shoppingCartService = shoppingCartService;
            _orderCalculationService = orderCalculationService;
            _settings = settings;
        }

        /// <summary>
        /// Renders PayPal buttons widget.
        /// </summary>
        /// <param name="isPaymentInfoInvoker">Defines whether the widget is invoked from payment method's GetPaymentInfoWidget.</param>
        /// /// <param name="isSelected">Defines whether the payment method is selected on page load.</param>
        public async Task<IViewComponentResult> InvokeAsync(bool isPaymentInfoInvoker, bool isSelected)
        {
            // If client id or secret haven't been configured yet, don't render buttons.
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

            // Get displayable options from settings depending on location (OffCanvasCart or Cart).
            var isCartPage = controller == "ShoppingCart" && action == "Cart";
            var fundingEnumIds = isCartPage ? 
                _settings.FundingsCart.SplitSafe(',').Select(int.Parse).ToArray() : 
                _settings.FundingsOffCanvasCart.SplitSafe(',').Select(int.Parse).ToArray();

            var fundings = string.Empty;
            foreach (var fundingId in fundingEnumIds)
            {
                fundings += ((EnableFundingOptions)fundingId).ToStringInvariant() + ",";
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
                IsSelectedMethod = isSelected,
                Fundings = fundings
            };

            return View(model);
        }
    }
}