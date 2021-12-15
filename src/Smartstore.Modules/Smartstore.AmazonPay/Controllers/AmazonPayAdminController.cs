using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.AmazonPay.Models;
using Smartstore.AmazonPay.Services;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Http;
using Smartstore.Scheduling;
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
        private readonly ITaskStore _taskStore;
        private readonly CompanyInformationSettings _companyInformationSettings;

        public AmazonPayAdminController(
            SmartDbContext db,
            IAmazonPayService amazonPayService,
            ICurrencyService currencyService,
            StoreDependingSettingHelper settingHelper,
            ITaskStore taskStore,
            CompanyInformationSettings companyInformationSettings)
        {
            _db = db;
            _amazonPayService = amazonPayService;
            _currencyService = currencyService;
            _settingHelper = settingHelper;
            _taskStore = taskStore;
            _companyInformationSettings = companyInformationSettings;
        }

        [LoadSetting]
        public async Task<IActionResult> Configure(AmazonPaySettings settings)
        {
            var language = Services.WorkContext.WorkingLanguage;
            var store = Services.StoreContext.CurrentStore;
            var allStores = Services.StoreContext.GetAllStores();
            var module = Services.ApplicationContext.ModuleCatalog.GetModuleByName("Smartstore.AmazonPay");

            var model = MiniMapper.Map<AmazonPaySettings, ConfigurationModel>(settings);

            // TODO: (mg) (core) shop update forces the merchant to update URLs at Amazon's Celler Central.
            // Following URLs are configured at Amazon's Celler Central:
            // ~/Plugins/SmartStore.AmazonPay/AmazonPay/IPNHandler
            // ~/Plugins/SmartStore.AmazonPay/AmazonPayShoppingCart/PayButtonHandler
            // ~/Plugins/SmartStore.AmazonPay/AmazonPay/AuthenticationButtonHandler

            model.IpnUrl = Services.WebHelper.GetStoreLocation(true) + "amazonpay/IPNHandler";
            model.KeyShareUrl = Services.WebHelper.GetStoreLocation() + "amazonpay/ShareKey";
            model.ModuleVersion = module.Version.ToString();
            model.LeadCode = AmazonPayService.LeadCode;
            model.PlatformId = AmazonPayService.PlatformId;
            // Not implemented. Not available for europe at the moment.
            model.PublicKey = string.Empty;
            model.LanguageLocale = _amazonPayService.GetAmazonLanguageCode(language.UniqueSeoCode, '_');
            model.MerchantStoreDescription = store.Name.Truncate(2048);
            model.MerchantPrivacyNoticeUrl = WebHelper.GetAbsoluteUrl(await Url.TopicAsync("privacyinfo"), Request, true, store.SslEnabled ? "https" : "http");
            model.MerchantSandboxIpnUrl = model.IpnUrl;
            model.MerchantProductionIpnUrl = model.IpnUrl;

            foreach (var entity in allStores)
            {
                // SSL required!
                var shopUrl = entity.SslEnabled ? entity.SecureUrl : entity.Url;
                if (shopUrl.HasValue())
                {
                    var loginDomain = GetLoginDomain(shopUrl);
                    var payHandlerUrl = shopUrl.EnsureEndsWith("/") + "amazonpay/PayButtonHandler";
                    var authHandlerUrl = shopUrl.EnsureEndsWith("/") + "amazonpay/AuthenticationButtonHandler";

                    model.MerchantLoginDomains.Add(loginDomain);
                    model.MerchantLoginRedirectUrls.Add(payHandlerUrl);
                    model.MerchantLoginRedirectUrls.Add(authHandlerUrl);

                    if (entity.Id == store.Id)
                    {
                        model.CurrentMerchantLoginDomains.Add(loginDomain);
                        model.CurrentMerchantLoginRedirectUrls.Add(payHandlerUrl);
                        model.CurrentMerchantLoginRedirectUrls.Add(authHandlerUrl);
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

            ViewBag.DataFetchings = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Text = T("Common.Unspecified"),
                    Value = string.Empty
                },
                new SelectListItem
                {
                    Text = T("Plugins.Payments.AmazonPay.DataFetching.Ipn"),
                    Value = ((int)AmazonPayDataFetchingType.Ipn).ToString(),
                    Selected = model.DataFetching == AmazonPayDataFetchingType.Ipn
                },
                new SelectListItem
                {
                    Text = T("Plugins.Payments.AmazonPay.DataFetching.Polling"),
                    Value = ((int)AmazonPayDataFetchingType.Polling).ToString(),
                    Selected = model.DataFetching == AmazonPayDataFetchingType.Polling
                }
            };

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

            model.AccessKey = model.AccessKey.TrimSafe();
            model.ClientId = model.ClientId.TrimSafe();
            model.SecretKey = model.SecretKey.TrimSafe();
            model.SellerId = model.SellerId.TrimSafe();

            MiniMapper.Map(model, settings);

            await _settingHelper.UpdateSettingsAsync(settings, form, storeScope);

            await Services.Settings.ApplySettingAsync(settings, x => x.DataFetching);
            await Services.Settings.ApplySettingAsync(settings, x => x.PollingMaxOrderCreationDays);

            var pollingTaskEnabled = settings.DataFetching == AmazonPayDataFetchingType.Polling;
            var task = await _taskStore.GetTaskByTypeAsync<DataPollingTask>();
            
            if (task != null && task.Enabled != pollingTaskEnabled)
            {
                task.Enabled = pollingTaskEnabled;
                await _taskStore.UpdateTaskAsync(task);
            }

            await _db.SaveChangesAsync();

            NotifySuccess(T("Plugins.Payments.AmazonPay.ConfigSaveNote"));

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

        private static string GetLoginDomain(string shopUrl)
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
