using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Components;

namespace Smartstore.PayPal.Components
{
    public class PayPalSepaViewComponent : SmartViewComponent
    {
        private readonly PayPalSettings _settings;

        public PayPalSepaViewComponent(PayPalSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Renders PayPal buttons widget.
        /// </summary>
        /// <param name="isPaymentInfoInvoker">Defines whether the widget is invoked from payment method's GetPaymentInfoWidget.</param>
        /// <param name="isSelected">Defines whether the payment method is selected on page load.</param>
        public IViewComponentResult Invoke(string funding, bool isPaymentInfoInvoker, bool isSelected)
        {
            // If client id or secret haven't been configured yet, don't render buttons.
            if (!_settings.ClientId.HasValue() || !_settings.Secret.HasValue())
            {
                return Empty();
            }

            var routeIdent = Request.RouteValues.GenerateRouteIdentifier();
            var isPaymentSelectionPage = routeIdent == "Checkout.PaymentMethod";

            if (isPaymentSelectionPage && isPaymentInfoInvoker)
            {
                return Empty();
            }

            // Get displayable options from settings depending on location (OffCanvasCart or Cart).
            var isCartPage = routeIdent == "ShoppingCart.Cart";
            if (isCartPage && !_settings.ShowButtonOnCartPage)
            {
                return Empty();
            }

            var model = new PublicPaymentMethodModel
            {
                IsPaymentSelection = isPaymentSelectionPage,
                ButtonColor = _settings.ButtonColor,
                ButtonShape = _settings.ButtonShape,
                IsSelectedMethod = isSelected,
                Funding = funding
            };

            return View(model);
        }
    }
}