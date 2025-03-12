using Microsoft.AspNetCore.Mvc;
using Smartstore.PayPal.Services;

namespace Smartstore.PayPal.Components
{
    /// <summary>
    /// Renders PayPal default button widget (funding source: paypal).
    /// </summary>
    public class PayPalViewComponent : PayPalViewComponentBase
    {
        protected override IViewComponentResult InvokeCore()
        {
            // Get displayable options from settings depending on location (OffCanvasCart or Cart).
            if (PayPalHelper.IsCartRoute(RouteIdent) && !Settings.FundingsCart.Contains(FundingOptions.paypal.ToString()))
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