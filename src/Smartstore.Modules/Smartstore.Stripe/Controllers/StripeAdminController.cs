using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.StripeElements.Models;
using Smartstore.StripeElements.Providers;
using Smartstore.StripeElements.Settings;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.StripeElements.Controllers
{
    [Area("Admin")]
    public class StripeAdminController : ModuleController
    {
        private readonly SmartDbContext _db;
        private readonly IProviderManager _providerManager;

        public StripeAdminController(SmartDbContext db, IProviderManager providerManager)
        {
            _db = db;
            _providerManager = providerManager;
        }

        [LoadSetting, AuthorizeAdmin]
        public IActionResult Configure(StripeSettings settings)
        {
            ViewBag.Provider = _providerManager.GetProvider(StripeElementsProvider.SystemName).Metadata;

            var model = MiniMapper.Map<StripeSettings, ConfigurationModel>(settings);

            model.WebhookUrl = Url.Action(nameof(StripeController.WebhookHandler), "Stripe", new { area = string.Empty }, "https");

            ViewBag.AvailableCaptureMethods = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Text = T("Plugins.Smartstore.Stripe.CaptureMethod.Automatic"),
                    Value = "automatic",
                    Selected = "automatic" == settings.CaptureMethod
                },
                new SelectListItem
                {
                    Text = T("Plugins.Smartstore.Stripe.CaptureMethod.Manual"),
                    Value = "manual",
                    Selected = "manual" == settings.CaptureMethod
                }
            };

            return View(model);
        }

        [HttpPost, SaveSetting, AuthorizeAdmin]
        public IActionResult Configure(ConfigurationModel model, StripeSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return Configure(settings);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            return RedirectToAction(nameof(Configure));
        }

        [HttpPost]
        [AuthorizeAdmin]
        [FormValueRequired("createwebhook"), ActionName("Configure")]
        public async Task<IActionResult> CreateWebhook(ConfigurationModel model)
        {
            var storeScope = GetActiveStoreScopeConfiguration();
            var settings = await Services.SettingFactory.LoadSettingsAsync<StripeSettings>(storeScope);

            if (settings.PublicApiKey.HasValue() && settings.SecrectApiKey.HasValue() && !settings.WebhookSecret.HasValue())
            {
                // Get Webhook ID vie API.
                try
                {
                    settings.WebhookSecret = await GetWebHookIdAsync(settings.SecrectApiKey);
                    await Services.SettingFactory.SaveSettingsAsync(settings, storeScope);
                }
                catch (Exception ex)
                {
                    NotifyError(ex.Message);
                }
            }

            return RedirectToAction(nameof(Configure));
        }

        private async Task<string> GetWebHookIdAsync(string secrectApiKey)
        {
            StripeConfiguration.ApiKey = secrectApiKey;

            var service = new WebhookEndpointService();
            var webhooks = await service.ListAsync(new WebhookEndpointListOptions
            {
                Limit = 10
            });

            // Get store URL
            var storeScope = GetActiveStoreScopeConfiguration();
            var store = storeScope == 0 ? Services.StoreContext.CurrentStore : Services.StoreContext.GetStoreById(storeScope);
            var storeUrl = store.GetHost(true).EnsureEndsWith('/');

            // Check if webhook already exists
            if (webhooks.Data.Count < 1 || !webhooks.Data.Any(x => x.Url.ContainsNoCase(storeUrl)))
            {
                // Create webhook
                var createOptions = new WebhookEndpointCreateOptions
                {
                    // INFO: Update API Version when updating Stripe.net dll
                    ApiVersion = "2022-08-01",
                    Url = storeUrl + "stripe/webhookhandler",
                    EnabledEvents = new List<string>
                    {
                        "payment_intent.succeeded",
                        "payment_intent.canceled",
                        "charge.refunded"
                    }
                };

                var webhook = await service.CreateAsync(createOptions);

                return webhook.Id;
            }
            else
            {
                return webhooks.Data.Where(x => x.Url.ContainsNoCase(storeUrl)).FirstOrDefault()?.Id;
            }
        }
    }
}