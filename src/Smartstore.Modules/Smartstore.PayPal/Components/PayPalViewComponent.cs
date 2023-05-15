using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Components;

namespace Smartstore.PayPal.Components
{
    public class PayPalViewComponent : SmartViewComponent
    {
        private readonly PayPalSettings _settings;

        public PayPalViewComponent(PayPalSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Renders PayPal default button widget (funding source: paypal).
        /// </summary>
        public IViewComponentResult Invoke()
        {
            // If client id or secret haven't been configured yet, don't render buttons.
            if (!_settings.ClientId.HasValue() || !_settings.Secret.HasValue())
            {
                return Empty();
            }

            var routeIdent = Request.RouteValues.GenerateRouteIdentifier();
            
            // Get displayable options from settings depending on location (OffCanvasCart or Cart).
            var isCartPage = routeIdent == "ShoppingCart.Cart";
            if (isCartPage && !_settings.FundingsCart.Contains(((int)FundingOptions.paypal).ToString()))
            {
                return Empty();
            }

            var model = new PublicPaymentMethodModel
            {
                ButtonColor = _settings.ButtonColor,
                ButtonShape = _settings.ButtonShape
            };

            return View(model);
        }
    }
}