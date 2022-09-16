using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Components;

namespace Smartstore.PayPal.Components
{
    public class PayPalInvoiceViewComponent : SmartViewComponent
    {
        private readonly PayPalSettings _settings;

        public PayPalInvoiceViewComponent(PayPalSettings settings)
        {
            _settings = settings;
        }

        public IViewComponentResult Invoke()
        {
            // If client id or secret haven't been configured yet, don't render button.
            if (!_settings.ClientId.HasValue() || !_settings.Secret.HasValue())
            {
                return Empty();
            }

            // TODO: (mh) (core) Prepare model if customer ist loggeds in and has already entered his data of birth
            // Also prepare Phonenumber if its available somewhere either in CustomerInfo or billing address.
            var model = new PublicInvoiceModel();
            return View(model);
        }
    }
}