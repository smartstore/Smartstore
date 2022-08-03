using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.AmazonPay.Services;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Configuration;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.AmazonPay.Controllers
{
    [Route("[area]/amazonpay/{action=index}/{id?}")]
    public class AmazonPayAdminController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IAmazonPayService _amazonPayService;
        private readonly IProviderManager _providerManager;
        private readonly MultiStoreSettingHelper _settingHelper;
        private readonly CompanyInformationSettings _companyInformationSettings;

        public AmazonPayAdminController(
            SmartDbContext db,
            IAmazonPayService amazonPayService,
            IProviderManager providerManager,
            MultiStoreSettingHelper settingHelper,
            CompanyInformationSettings companyInformationSettings)
        {
            _db = db;
            _amazonPayService = amazonPayService;
            _providerManager = providerManager;
            _settingHelper = settingHelper;
            _companyInformationSettings = companyInformationSettings;
        }

        [LoadSetting]
        public async Task<IActionResult> Configure(AmazonPaySettings settings)
        {
            var language = Services.WorkContext.WorkingLanguage;
            var store = Services.StoreContext.CurrentStore;
            var module = Services.ApplicationContext.ModuleCatalog.GetModuleByName("Smartstore.AmazonPay");
            var currentScheme = Services.WebHelper.IsCurrentConnectionSecured() ? "https" : "http";
            var topicUrl = await Url.TopicAsync("privacyinfo");

            var model = MiniMapper.Map<AmazonPaySettings, ConfigurationModel>(settings);

            model.IpnUrl = Url.Action(nameof(AmazonPayController.IPNHandler), "AmazonPay", new { area = string.Empty }, "https");
            model.KeyShareUrl = Url.Action(nameof(AmazonPayController.ShareKey), "AmazonPay", new { area = string.Empty }, currentScheme);
            model.ModuleVersion = module.Version.ToString();
            model.LeadCode = AmazonPayProvider.LeadCode;
            model.PlatformId = AmazonPayProvider.PlatformId;
            // Not implemented. Not available for europe at the moment.
            model.PublicKey = string.Empty;
            model.MerchantStoreDescription = store.Name.Truncate(2048);
            if (topicUrl.HasValue())
            {
                model.MerchantPrivacyNoticeUrl = WebHelper.GetAbsoluteUrl(topicUrl, Request, true, currentScheme);
            }
            model.MerchantSandboxIpnUrl = model.IpnUrl;
            model.MerchantProductionIpnUrl = model.IpnUrl;
            model.HasPrivateKey = settings.PrivateKey.HasValue();

            model.LanguageLocale = language.UniqueSeoCode.EmptyNull().ToLower() switch
            {
                "en" => "en_GB",
                "fr" => "fr_FR",
                "it" => "it_IT",
                "es" => "es_ES",
                _ => "de_DE",
            };

            if (_companyInformationSettings.CountryId != 0)
            {
                var merchantCountry = await _db.Countries.FindByIdAsync(_companyInformationSettings.CountryId, false);
                if (merchantCountry != null)
                {
                    model.MerchantCountry = merchantCountry.GetLocalized(x => x.Name, language, false, false);
                }
            }

            PrepareConfigurationModel(model);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model, IFormCollection form)
        {
            var storeScope = GetActiveStoreScopeConfiguration();
            var settings = await Services.SettingFactory.LoadSettingsAsync<AmazonPaySettings>(storeScope);

            if (!ModelState.IsValid)
            {
                return await Configure(settings);
            }

            ModelState.Clear();

            model.PublicKey = model.PublicKey.TrimSafe();
            model.ClientId = model.ClientId.TrimSafe();
            model.SellerId = model.SellerId.TrimSafe();

            settings = ((ISettings)settings).Clone() as AmazonPaySettings;
            MiniMapper.Map(model, settings);

            await _settingHelper.UpdateSettingsAsync(settings, form, storeScope);
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

            return RedirectToAction(nameof(Configure));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAccessKeys(string accessData)
        {
            try
            {
                var storeScope = GetActiveStoreScopeConfiguration();
                await _amazonPayService.UpdateAccessKeysAsync(accessData, storeScope);

                NotifySuccess(T("Plugins.Payments.AmazonPay.SaveAccessDataSucceeded"));
            }
            catch (Exception ex)
            {
                NotifyError(ex.Message);
            }

            return RedirectToAction(nameof(Configure));
        }

        [HttpPost]
        public async Task<IActionResult> UploadPrivateKey()
        {
            if (!Request.Form.Files.Any())
            {
                throw new BadHttpRequestException(T("Common.NoFileUploaded"));
            }

            var storeScope = GetActiveStoreScopeConfiguration();
            var settings = await Services.SettingFactory.LoadSettingsAsync<AmazonPaySettings>(storeScope);

            using var sourceStream = Request.Form.Files[0].OpenReadStream();
            var privateKey = await sourceStream.AsStringAsync();

            settings.PrivateKey = privateKey.TrimSafe();
            await Services.SettingFactory.SaveSettingsAsync(settings, storeScope);

            NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> RemovePrivateKey()
        {
            var storeScope = GetActiveStoreScopeConfiguration();
            var settings = await Services.SettingFactory.LoadSettingsAsync<AmazonPaySettings>(storeScope);

            settings.PrivateKey = null;
            await Services.SettingFactory.SaveSettingsAsync(settings, storeScope);

            return RedirectToAction(nameof(Configure));
        }

        private void PrepareConfigurationModel(ConfigurationModel model)
        {
            var store = Services.StoreContext.CurrentStore;
            var allStores = Services.StoreContext.GetAllStores();

            // INFO: it's unclear if it's necessary to specify additional URLs in Amazon Seller Central for Checkout v2,
            // as the return URLs are set on the server side and no longer via JavaScript (as in v1).
            // However, the documentation states to still add them:
            // https://developer.amazon.com/de/docs/amazon-pay-checkout/get-set-up-for-integration.html#2-add-domains-to-seller-central

            foreach (var entity in allStores)
            {
                // SSL required!
                if (Uri.TryCreate(entity.SslEnabled ? entity.SecureUrl : entity.Url, UriKind.Absolute, out var uri))
                {
                    var loginUrl = uri.GetLeftPart(UriPartial.Scheme | UriPartial.Authority).EmptyNull().TrimEnd('/');
                    if (loginUrl.HasValue())
                    {
                        model.MerchantLoginDomains.Add(loginUrl);

                        if (entity.Id == store.Id)
                        {
                            model.CurrentMerchantLoginDomains.Add(loginUrl);
                        }
                    }

                    var storeScheme = entity.SslEnabled ? "https" : "http";
                    var checkoutReviewUrl = Url.Action(nameof(AmazonPayController.CheckoutReview), "AmazonPay", new { area = string.Empty }, storeScheme).TrimEnd('/');
                    var signIn = Url.Action(nameof(AmazonPayController.SignIn), "AmazonPay", new { area = string.Empty }, storeScheme).TrimEnd('/');

                    model.MerchantLoginRedirectUrls.Add(checkoutReviewUrl);
                    model.MerchantLoginRedirectUrls.Add(signIn);

                    if (entity.Id == store.Id)
                    {
                        model.CurrentMerchantLoginRedirectUrls.Add(checkoutReviewUrl);
                        model.CurrentMerchantLoginRedirectUrls.Add(signIn);
                    }
                }
            }

            ViewBag.PrimaryStoreCurrencyCode = Services.CurrencyService.PrimaryCurrency.CurrencyCode;
            ViewBag.Provider = _providerManager.GetProvider(AmazonPayProvider.SystemName).Metadata;

            ViewBag.TransactionTypes = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Text = T("Plugins.Payments.AmazonPay.TransactionType.AuthAndCapture"),
                    Value = ((int)AmazonPayTransactionType.AuthorizeAndCapture).ToString(),
                    Selected = model.TransactionType == AmazonPayTransactionType.AuthorizeAndCapture,
                },
                new SelectListItem
                {
                    Text = T("Plugins.Payments.AmazonPay.TransactionType.Auth"),
                    Value = ((int)AmazonPayTransactionType.Authorize).ToString(),
                    Selected = model.TransactionType == AmazonPayTransactionType.Authorize
                }
            };

            ViewBag.SaveEmailAndPhones = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Text = T("Common.Unspecified"),
                    Value = string.Empty
                },
                new SelectListItem
                {
                    Text = T("Plugins.Payments.AmazonPay.AmazonPaySaveDataType.OnlyIfEmpty"),
                    Value = ((int)AmazonPaySaveDataType.OnlyIfEmpty).ToString(),
                    Selected = model.SaveEmailAndPhone == AmazonPaySaveDataType.OnlyIfEmpty,
                },
                new SelectListItem
                {
                    Text = T("Plugins.Payments.AmazonPay.AmazonPaySaveDataType.Always"),
                    Value = ((int)AmazonPaySaveDataType.Always).ToString(),
                    Selected = model.SaveEmailAndPhone == AmazonPaySaveDataType.Always
                }
            };
        }
    }
}
