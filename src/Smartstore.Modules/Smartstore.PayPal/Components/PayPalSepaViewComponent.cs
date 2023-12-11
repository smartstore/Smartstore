using Microsoft.AspNetCore.Mvc;

namespace Smartstore.PayPal.Components
{
    /// <summary>
    /// Renders PayPal button widget (funding source: sepa).
    /// </summary>
    public class PayPalSepaViewComponent : PayPalViewComponentBase
    {
        protected override IViewComponentResult InvokeCore()
        {
            // Get displayable options from settings depending on location (OffCanvasCart or Cart).
            var isCartPage = RouteIdent == "ShoppingCart.Cart";
            if (isCartPage && !Settings.FundingsCart.Contains(FundingOptions.sepa.ToString()))
            {
                return Empty();
            }

            var model = new PublicPaymentMethodModel
            {
                ButtonColor = Settings.ButtonColor,
                ButtonShape = Settings.ButtonShape,
                RouteIdent = RouteIdent
            };

            return View(model);
        }
    }
}