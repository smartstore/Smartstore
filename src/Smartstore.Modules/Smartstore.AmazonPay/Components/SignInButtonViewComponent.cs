using Microsoft.AspNetCore.Mvc;
using Smartstore.AmazonPay.Services;
using Smartstore.Web.Components;

namespace Smartstore.AmazonPay.Components
{
    /// <summary>
    /// Renders the AmazonPay sign-in button.
    /// </summary>
    public class SignInButtonViewComponent : SmartViewComponent
    {
        private readonly AmazonPaySettings _settings;

        public SignInButtonViewComponent(AmazonPaySettings amazonPaySettings)
        {
            _settings = amazonPaySettings;
        }

        public IViewComponentResult Invoke()
        {
            if (_settings.PublicKeyId.IsEmpty() || _settings.PrivateKey.IsEmpty())
            {
                return Empty();
            }

            // INFO: AmazonPay script does not render anything if current currency is not supported.
            var currencyCode = Services.CurrencyService.PrimaryCurrency.CurrencyCode;
            if (!AmazonPayService.IsLedgerCurrencySupported(currencyCode))
            {
                return Empty();
            }

            var model = new AmazonPayButtonModel(
                _settings,
                "SignIn",
                currencyCode,
                Services.WorkContext.WorkingLanguage.UniqueSeoCode);

            return View(model);
        }
    }
}
