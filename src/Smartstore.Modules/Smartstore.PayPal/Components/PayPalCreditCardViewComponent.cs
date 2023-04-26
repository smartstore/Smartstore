using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Components;

namespace Smartstore.PayPal.Components
{
    public class PayPalCreditCardViewComponent : SmartViewComponent
    {
        private readonly PayPalSettings _settings;

        public PayPalCreditCardViewComponent(PayPalSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Renders PayPal hosted fields for credit card processing & and input elements for address data.
        /// </summary>
        public IViewComponentResult Invoke()
        {
            // If client id or secret haven't been configured yet, don't render hosted fields.
            if (!_settings.ClientId.HasValue() || !_settings.Secret.HasValue())
            {
                return Empty();
            }

            var model = new PublicCreditCardModel();

            return View(model);
        }
    }
}