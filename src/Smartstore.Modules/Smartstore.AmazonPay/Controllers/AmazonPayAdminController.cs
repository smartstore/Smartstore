using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.AmazonPay.Services;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;
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
        private readonly ICurrencyService _currencyService;
        private readonly StoreDependingSettingHelper _settingHelper;
        private readonly CompanyInformationSettings _companyInformationSettings;

        public AmazonPayAdminController(
            SmartDbContext db,
            IAmazonPayService amazonPayService,
            ICurrencyService currencyService,
            StoreDependingSettingHelper settingHelper,
            CompanyInformationSettings companyInformationSettings)
        {
            _db = db;
            _amazonPayService = amazonPayService;
            _currencyService = currencyService;
            _settingHelper = settingHelper;
            _companyInformationSettings = companyInformationSettings;
        }

        [LoadSetting]
        public async Task<IActionResult> Configure(AmazonPaySettings settings)
        {
            var language = Services.WorkContext.WorkingLanguage;
            var store = Services.StoreContext.CurrentStore;
            var allStores = Services.StoreContext.GetAllStores();
            var module = Services.ApplicationContext.ModuleCatalog.GetModuleByName("Smartstore.AmazonPay");
            var currentScheme = Services.WebHelper.IsCurrentConnectionSecured() ? "https" : "http";

            var model = MiniMapper.Map<AmazonPaySettings, ConfigurationModel>(settings);

            model.IpnUrl = Url.Action(nameof(AmazonPayController.IPNHandler), "AmazonPay", null, "https");
            model.KeyShareUrl = Url.Action(nameof(AmazonPayController.ShareKey), "AmazonPay", null, currentScheme);
            model.ModuleVersion = module.Version.ToString();
            model.LeadCode = AmazonPayProvider.LeadCode;
            model.PlatformId = AmazonPayProvider.PlatformId;
            // Not implemented. Not available for europe at the moment.
            model.PublicKey = string.Empty;
            model.MerchantStoreDescription = store.Name.Truncate(2048);
            model.MerchantPrivacyNoticeUrl = WebHelper.GetAbsoluteUrl(await Url.TopicAsync("privacyinfo"), Request, true, currentScheme);
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

            foreach (var entity in allStores)
            {
                // SSL required!
                var loginUrl = GetLoginUrl(entity.SslEnabled ? entity.SecureUrl : entity.Url);
                if (loginUrl.HasValue())
                {
                    var redirectUrl = loginUrl.EnsureEndsWith("/");

                    model.MerchantLoginDomains.Add(loginUrl);
                    model.MerchantLoginRedirectUrls.Add(redirectUrl);

                    if (entity.Id == store.Id)
                    {
                        model.CurrentMerchantLoginDomains.Add(loginUrl);
                        model.CurrentMerchantLoginRedirectUrls.Add(redirectUrl);
                    }
                }
            }

            if (_companyInformationSettings.CountryId != 0)
            {
                var merchantCountry = await _db.Countries.FindByIdAsync(_companyInformationSettings.CountryId, false);
                if (merchantCountry != null)
                {
                    model.MerchantCountry = merchantCountry.GetLocalized(x => x.Name, language, false, false);
                }
            }

            ViewBag.PrimaryStoreCurrencyCode = _currencyService.PrimaryCurrency.CurrencyCode;

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
            model.AccessKey = model.AccessKey.TrimSafe();
            model.ClientId = model.ClientId.TrimSafe();
            model.SecretKey = model.SecretKey.TrimSafe();
            model.SellerId = model.SellerId.TrimSafe();

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

        private static string GetLoginUrl(string shopUrl)
        {
            try
            {
                // Only protocol and domain name.
                var uri = new Uri(shopUrl);
                return uri.GetLeftPart(UriPartial.Scheme | UriPartial.Authority).EmptyNull().TrimEnd('/');
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
