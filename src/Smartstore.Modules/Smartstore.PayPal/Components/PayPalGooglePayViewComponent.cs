using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Smartstore.PayPal.Components
{
    /// <summary>
    /// Renders PayPal button for Google Pay.
    /// </summary>
    public class PayPalGooglePayViewComponent : PayPalViewComponentBase
    {
        protected override IViewComponentResult InvokeCore()
        {
            if (HttpContext.Connection.IsLocal())
            {
                return Empty();
            }

            // Get displayable options from settings depending on location (OffCanvasCart or Cart).
            var isCartPage = RouteIdent == "ShoppingCart.Cart";
            if (isCartPage && !Settings.FundingsCart.Contains(FundingOptions.googlepay.ToString()))
            {
                return Empty();
            }

            var model = new GooglePayModel
            {
                IsSandbox = Settings.UseSandbox,
                RouteIdent = RouteIdent
            };

            return View(model);
        }
    }
}