using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.PayPal.Services;

namespace Smartstore.PayPal.Components
{
    /// <summary>
    /// Renders PayPal button for Apple Pay.
    /// </summary>
    public class PayPalApplePayViewComponent : PayPalViewComponentBase
    {
        protected override IViewComponentResult InvokeCore()
        {
            if (HttpContext.Connection.IsLocal())
            {
                return Empty();
            }

            if (PayPalHelper.IsCartRoute(RouteIdent) && !Settings.FundingsCart.Contains(FundingOptions.applepay.ToString()))
            {
                return Empty();
            }

            var model = new ApplePayModel
            {
                IsSandbox = Settings.UseSandbox,
                RouteIdent = RouteIdent
            };

            return View(model);
        }
    }
}
