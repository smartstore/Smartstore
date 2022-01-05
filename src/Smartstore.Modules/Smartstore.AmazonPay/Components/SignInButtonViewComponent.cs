using Microsoft.AspNetCore.Mvc;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Components;

namespace Smartstore.AmazonPay.Components
{
    /// <summary>
    /// Renders the AmazonPay sign-in button.
    /// </summary>
    public class SignInButtonViewComponent : SmartViewComponent
    {
        private readonly IProviderManager _providerManager;
        private readonly AmazonPaySettings _settings;

        public SignInButtonViewComponent(
            IProviderManager providerManager,
            AmazonPaySettings amazonPaySettings)
        {
            _providerManager = providerManager;
            _settings = amazonPaySettings;
        }

        public IViewComponentResult Invoke()
        {
            if (_settings.PublicKeyId.IsEmpty() || _settings.PrivateKey.IsEmpty())
            {
                return Empty();
            }

            var module = Services.ApplicationContext.ModuleCatalog.GetModuleByAssembly(typeof(SignInButtonViewComponent).Assembly);
            if (!_providerManager.IsActiveForStore(module, Services.StoreContext.CurrentStore.Id))
            {
                return Empty();
            }

            var model = new AmazonPayButtonModel(
                _settings, 
                "SignIn",
                Services.CurrencyService.PrimaryCurrency.CurrencyCode,
                Services.WorkContext.WorkingLanguage.UniqueSeoCode);

            return View(model);
        }
    }
}
