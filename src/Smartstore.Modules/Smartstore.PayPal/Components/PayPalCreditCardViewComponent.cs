using Microsoft.AspNetCore.Mvc;

namespace Smartstore.PayPal.Components
{
    /// <summary>
    /// Renders PayPal hosted fields for credit card processing & and input elements for address data.
    /// </summary>
    public class PayPalCreditCardViewComponent : PayPalViewComponentBase
    {
        protected override IViewComponentResult InvokeCore()
        {
            var model = new PublicCreditCardModel();
            return View(model);
        }
    }
}