using System.Net;
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
using static Smartstore.PayPal.Module;

namespace Smartstore.PayPal.Controllers
{
    [Area("Admin")]
    [Route("[area]/paypal/{action=index}/{id?}")]
    public class PayPalAdminController : ModuleController
    {
        private readonly ICacheFactory _cacheFactory;
        private readonly IProviderManager _providerManager;
        private readonly PayPalHttpClient _client;
        private readonly ILocalizedEntityService _localizedEntityService;

        public PayPalAdminController(
            ICacheFactory cacheFactory, 
            IProviderManager providerManager, 
            PayPalHttpClient client, 
            ILocalizedEntityService localizedEntityService)
        {
            _cacheFactory = cacheFactory;
            _providerManager = providerManager;
            _client = client;
            _localizedEntityService = localizedEntityService;
        }

        [LoadSetting, AuthorizeAdmin]
        public IActionResult Configure(int storeId, PayPalSettings settings)
        {
            var model = MiniMapper.Map<PayPalSettings, ConfigurationModel>(settings);

            model.WebhookUrl = Url.Action(nameof(PayPalController.WebhookHandler), "PayPal", new { area = string.Empty }, "https");

            ViewBag.PrimaryStoreCurrencyCode = Services.CurrencyService.PrimaryCurrency.CurrencyCode;
            ViewBag.Provider = _providerManager.GetProvider(PayPalConstants.Standard).Metadata;

            if (settings.ClientId.HasValue() && settings.Secret.HasValue())
            {
                model.HasCredentials = true;
            }

            if (settings.WebhookId.HasValue())
            {
                model.WebHookCreated = true;
            }

            // Convert FundingOptions from settings to Array<int> so the corresponding taghelper in configure view can work with it.
            model.FundingsCart = settings.FundingsCart
                .SplitSafe(",")
                .Select(x => ((int)x.Convert<FundingOptions>()).ToString())
                .ToArray();

            model.FundingsOffCanvasCart = settings.FundingsOffCanvasCart
                .SplitSafe(",")
                .Select(x => ((int)x.Convert<FundingOptions>()).ToString())
                .ToArray();

            model.DisplayOnboarding = !settings.ClientId.HasValue() && !settings.Secret.HasValue();

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.CustomerServiceInstructions = settings.GetLocalizedSetting(x => x.CustomerServiceInstructions, languageId, storeId, false, false);
            });

            return View(model);
        }

        [HttpPost, SaveSetting, AuthorizeAdmin]
        public async Task<IActionResult> Configure(int storeId, ConfigurationModel model, PayPalSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return Configure(storeId, settings);
            }

            // Clear token from cache if ClientId or Secret have changed.
            if (model.ClientId != settings.ClientId || model.Secret != settings.Secret)
            {
                await _cacheFactory.GetMemoryCache().RemoveByPatternAsync(PayPalHttpClient.PAYPAL_ACCESS_TOKEN_PATTERN_KEY);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            // Convert FundingOptions for cart & OffCanvasCart to comma separated string.
            var fundingsCart = model.FundingsCart?.Select(x => x.Convert<FundingOptions>().ToString()) ?? Array.Empty<string>();
            var fundingsOffCanvasCart = model.FundingsOffCanvasCart?.Select(x => x.Convert<FundingOptions>().ToString()) ?? Array.Empty<string>();

            settings.FundingsCart = string.Join(',', fundingsCart);
            settings.FundingsOffCanvasCart = string.Join(',', fundingsOffCanvasCart);

            // Localization
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedSettingAsync(settings, x => x.CustomerServiceInstructions, localized.CustomerServiceInstructions, localized.LanguageId, storeId);
            }

            return RedirectToAction(nameof(Configure));
        }

        [HttpPost]
        [AuthorizeAdmin]
        [FormValueRequired("createwebhook"), ActionName("Configure")]
        public async Task<IActionResult> CreateWebhook(ConfigurationModel model)
        {
            var storeScope = GetActiveStoreScopeConfiguration();
            var settings = await Services.SettingFactory.LoadSettingsAsync<PayPalSettings>(storeScope);

            if (settings.ClientId.HasValue() && settings.Secret.HasValue() && !settings.WebhookId.HasValue())
            {
                // Get Webhook ID vie API.
                try
                {
                    settings.WebhookId = await GetWebHookIdAsync();
                    await Services.SettingFactory.SaveSettingsAsync(settings, storeScope);
                }
                catch (Exception ex)
                {
                    NotifyError(ex.Message);
                }
            }

            return RedirectToAction(nameof(Configure));
        }

        [LoadSetting, AuthorizeAdmin]
        public async Task<IActionResult> SupportTools(PayPalSettings settings)
        {
            var model = new MerchantStatusModel();

            if (settings.PayerId.HasValue() && settings.ClientId.HasValue() && settings.Secret.HasValue())
            {
                try
                {
                    var getMerchantStatusRequest = new GetMerchantStatusRequest(PayPalConstants.PartnerId, settings.PayerId);
                    var getMerchantStatusResponse = await _client.ExecuteRequestAsync(getMerchantStatusRequest);
                    var merchantStatus = getMerchantStatusResponse.Body<MerchantStatus>();

                    model.LegalName = merchantStatus.LegalName;
                    model.MerchantId = merchantStatus.MerchantId;
                    model.TrackingId = merchantStatus.TrackingId;
                    model.PaymentsReceivable = merchantStatus.PaymentsReceivable;
                    model.PrimaryEmailConfirmed = merchantStatus.PrimaryEmailConfirmed;
                    model.Products = merchantStatus.Products;
                    model.Capabilities = merchantStatus.Capabilities;

                    // TODO: (mh) (core) Maybe test via API if the webhook is still active.
                    model.WebHookCreated = settings.WebhookId.HasValue();

                    return View(model);
                }
                catch (Exception ex)
                {
                    NotifyError(ex.Message);

                    model.DisplayOnboardingHint = true;
                }
            }
            else
            {
                // Inform shop admin about onboarding before checking status.
                model.DisplayOnboardingHint = true;
            }

            return View(model);
        }

        /// <summary>
        /// Called by Ajax request after onboarding to get ClientId & Secret.
        /// </summary>
        [HttpPost]
        [AuthorizeAdmin]
        public async Task<IActionResult> GetCredentials(string authCode, string sharedId, string sellerNonce)
        {
            var success = false;
            var message = string.Empty;
            
            try
            {
                var accessTokenRequest = new AccessTokenRequest(authCode: authCode, sharedId: sharedId, sellerNonce: sellerNonce);
                var accessTokenResponse = await _client.ExecuteRequestAsync(accessTokenRequest);

                if (accessTokenResponse.Status == HttpStatusCode.OK)
                {
                    var accesstoken = accessTokenResponse.Body<AccessToken>();
                    var credentialsRequest = new GetSellerCredentialsRequest(PayPalConstants.PartnerId, accesstoken.Token);
                    var credentialsResponse = await _client.ExecuteRequestAsync(credentialsRequest);

                    if (credentialsResponse.Status == HttpStatusCode.OK)
                    {
                        var credentials = credentialsResponse.Body<SellerCredentials>();

                        var storeScope = GetActiveStoreScopeConfiguration();
                        var settings = await Services.SettingFactory.LoadSettingsAsync<PayPalSettings>(storeScope);

                        // Save settings
                        settings.ClientId = credentials.ClientId;
                        settings.Secret = credentials.ClientSecret;
                        settings.PayerId = credentials.PayerId;

                        await Services.SettingFactory.SaveSettingsAsync(settings, storeScope);

                        success = true;
                        message = T("Plugins.Smartstore.PayPal.Onboarding.Success").Value;

                        object data = new
                        {
                            clientId = credentials.ClientId,
                            clientSecret = credentials.ClientSecret,
                            payerId = credentials.PayerId,
                            success,
                            message
                        };

                        return new JsonResult(data);
                    }
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }

        /// <summary>
        /// Called by Ajax request after onboarding to get status about confirmed email & if payments are receivable.
        /// </summary>
        [HttpPost]
        [AuthorizeAdmin]
        public async Task<IActionResult> GetStatus()
        {
            var success = false;
            var paymentsReceivable = false;
            var primaryEmailConfirmed = false;

            try
            {
                var storeScope = GetActiveStoreScopeConfiguration();
                var settings = await Services.SettingFactory.LoadSettingsAsync<PayPalSettings>(storeScope);

                var getMerchantStatusRequest = new GetMerchantStatusRequest(PayPalConstants.PartnerId, settings.PayerId);
                var getMerchantStatusResponse = await _client.ExecuteRequestAsync(getMerchantStatusRequest);
                var merchantStatus = getMerchantStatusResponse.Body<MerchantStatus>();

                if (merchantStatus.PaymentsReceivable)
                {
                    paymentsReceivable = true;
                }

                if (merchantStatus.PrimaryEmailConfirmed)
                {
                    primaryEmailConfirmed = true;
                }

                success = true;
            }
            catch (Exception ex)
            {
                NotifyError(ex.Message);
            }

            return new JsonResult(new { success, paymentsReceivable, primaryEmailConfirmed });
        }

        private async Task<string> GetWebHookIdAsync()
        {
            var listWebhooksResponse = await _client.ListWebhooksAsync(new ListWebhooksRequest());
            var webhooks = listWebhooksResponse.Body<Webhooks>();

            // Get store URL
            var storeScope = GetActiveStoreScopeConfiguration();
            var store = storeScope == 0 ? Services.StoreContext.CurrentStore : Services.StoreContext.GetStoreById(storeScope);
            var storeUrl = store.GetBaseUrl();

            if (webhooks.Hooks.Length < 1 || !webhooks.Hooks.Any(x => x.Url.ContainsNoCase(storeUrl)))
            {
                // Create webhook
                var webhook = new Webhook
                {
                    EventTypes = new EventType[]
                    {
                        new() { Name = "*" }
                    },
                    Url = storeUrl + "paypal/webhookhandler"
                };

                var request = new CreateWebhookRequest().WithBody(webhook);
                var response = await _client.CreateWebhookAsync(request);
                webhook = response.Body<Webhook>();

                return webhook.Id;
            }
            else
            {
                return webhooks.Hooks.Where(x => x.Url.ContainsNoCase(storeUrl)).FirstOrDefault()?.Id;
            }
        }
    }
}