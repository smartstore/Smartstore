using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Caching;
using Smartstore.ComponentModel;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.PayPal.Client;
using Smartstore.PayPal.Client.Messages;
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
        private readonly PayPalHttpClient _client;

        public PayPalAdminController(ICacheFactory cacheFactory, IProviderManager providerManager, PayPalHttpClient client)
        {
            _cacheFactory = cacheFactory;
            _providerManager = providerManager;
            _client = client;
        }

        [LoadSetting, AuthorizeAdmin]
        public IActionResult Configure(PayPalSettings settings)
        {
            var model = MiniMapper.Map<PayPalSettings, ConfigurationModel>(settings);

            model.EnabledFundings = settings.EnabledFundings.SplitSafe(',').ToArray();
            model.DisabledFundings = settings.DisabledFundings.SplitSafe(',').ToArray();
            model.WebhookUrl = Url.Action(nameof(PayPalController.WebhookHandler), "PayPal", new { area = string.Empty }, "https");

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

            if (settings.ClientId.HasValue() && settings.Secret.HasValue() && !settings.WebhookId.HasValue())
            {
                // Get Webhook ID vie API.
                settings.WebhookId = await GetWebHookIdAsync();
            }

            return RedirectToAction(nameof(Configure));
        }

        private async Task<string> GetWebHookIdAsync()
        {
            var listWebhooksResponse = await _client.ListWebhooksAsync(new ListWebhooksRequest());
            var webhooks = listWebhooksResponse.Body<Webhooks>();

            if (webhooks.Hooks.Length < 1)
            {
                // Get store URL
                var storeScope = GetActiveStoreScopeConfiguration();
                Store store;
                if (storeScope == 0)
                {
                    store = Services.StoreContext.CurrentStore;
                }
                else
                {
                    store = Services.StoreContext.GetStoreById(storeScope);
                }

                // Create webhook
                var webhook = new Webhook
                {
                    EventTypes = new EventType[]
                    {
                            new EventType { Name = "*" }
                    },
                    Url = store.GetHost(true)
                };

                var request = new CreateWebhookRequest().WithBody(webhook);
                var response = await _client.CreateWebhookAsync(request);
                webhook = response.Body<Webhook>();

                return webhook.Id;
            }
            else
            {
                // TODO: (mh) (core) Why can there be several webhooks?
                return webhooks.Hooks[0].Id;
            }
        }
    }
}
