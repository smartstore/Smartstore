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

            var customer = Services.WorkContext.CurrentCustomer;
            var model = new PublicInvoiceModel();

            if (customer.BillingAddress != null && customer.BillingAddress.Country != null)
            {
                var diallingCode = customer.BillingAddress.Country.DiallingCode;
                model.DiallingCode = diallingCode.Value.ToString();
            }
            else
            {
                // If there's no BillingAddress or no country, we can't offer invoice.
                return Empty();
            }

            // Prepare model if customer is logged in and has already entered his data of birth
            if (customer.BirthDate != null)
            {
                model.DateOfBirthDay = customer.BirthDate.Value.Day;
                model.DateOfBirthMonth = customer.BirthDate.Value.Month;
                model.DateOfBirthYear = customer.BirthDate.Value.Year;
            }

            return View(model);
        }
    }
}