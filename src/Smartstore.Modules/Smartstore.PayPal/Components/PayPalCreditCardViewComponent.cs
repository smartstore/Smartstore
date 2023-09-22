using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Smartstore.PayPal.Components
{
    /// <summary>
    /// Renders PayPal hosted fields for credit card processing.
    /// </summary>
    public class PayPalCreditCardViewComponent : PayPalViewComponentBase
    {
        protected override IViewComponentResult InvokeCore()
        {
            var model = new PublicCreditCardModel
            {
                HasClientToken = HttpContext.Session.GetString("PayPalClientToken").HasValue()
            };

            return View(model);
        }
    }
}