using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Caching;
using Smartstore.ComponentModel;
using Smartstore.Core.Security;
using Smartstore.Engine.Modularity;
using Smartstore.PayPal.Client;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.PayPal.Controllers
{
    [Area("Admin")]
    [Route("[area]/paypal/{action=index}/{id?}")]
    public class PayPalAdminController : ModuleController
    {
        private readonly ICacheFactory _cacheFactory;
        private readonly IProviderManager _providerManager;

        public PayPalAdminController(ICacheFactory cacheFactory, IProviderManager providerManager)
        {
            _cacheFactory = cacheFactory;
            _providerManager = providerManager;
        }

        [LoadSetting, AuthorizeAdmin]
        public IActionResult Configure(PayPalSettings settings)
        {
            var model = MiniMapper.Map<PayPalSettings, ConfigurationModel>(settings);

            model.EnabledFundings = settings.EnabledFundings.SplitSafe(',').ToArray();
            model.DisabledFundings = settings.DisabledFundings.SplitSafe(',').ToArray();
            model.WebhookUrl = Url.Action(nameof(PayPalController.WebhookHandler), "PayPal", null, "https");

            ViewBag.Provider = _providerManager.GetProvider("Payments.PayPalStandard").Metadata;

            return View(model);
        }

        [HttpPost, SaveSetting, AuthorizeAdmin]
        public async Task<IActionResult> Configure(ConfigurationModel model, PayPalSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return Configure(settings);
            }

            // Clear token from cache if ClientId or Secret have changed.
            if (model.ClientId != settings.ClientId || model.Secret != settings.Secret)
            {
                await _cacheFactory.GetMemoryCache().RemoveByPatternAsync(PayPalHttpClient.PAYPAL_ACCESS_TOKEN_PATTERN_KEY);
            }
            
            ModelState.Clear();
            MiniMapper.Map(model, settings);

            string.Join(',', model.EnabledFundings ?? Array.Empty<string>());
            string.Join(',', model.DisabledFundings ?? Array.Empty<string>());

            return RedirectToAction(nameof(Configure));
        }
    }
}
