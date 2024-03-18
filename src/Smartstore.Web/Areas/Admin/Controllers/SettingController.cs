using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Smartstore.Admin.Models;
using Smartstore.Caching;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Catalog.Search.Modelling;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Configuration;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.DataExchange;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data.Caching;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public class SettingController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ICurrencyService _currencyService;
        private readonly ILanguageService _languageService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly MultiStoreSettingHelper _multiStoreSettingHelper;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ICookieConsentManager _cookieManager;
        private readonly IProviderManager _providerManager;
        private readonly Lazy<IMediaTracker> _mediaTracker;
        private readonly Lazy<IMenuService> _menuService;
        private readonly Lazy<ICatalogSearchQueryAliasMapper> _catalogSearchQueryAliasMapper;
        private readonly Lazy<IMediaMover> _mediaMover;
        private readonly Lazy<IConfigureOptions<IdentityOptions>> _identityOptionsConfigurer;
        private readonly IOptions<IdentityOptions> _identityOptions;
        private readonly PrivacySettings _privacySettings;
        private readonly Lazy<ModuleManager> _moduleManager;
        private readonly ICacheManager _cache;

        public SettingController(
            SmartDbContext db,
            ICurrencyService currencyService,
            ILanguageService languageService,
            ILocalizedEntityService localizedEntityService,
            MultiStoreSettingHelper multiStoreSettingHelper,
            IDateTimeHelper dateTimeHelper,
            ICookieConsentManager cookieManager,
            IProviderManager providerManager,
            Lazy<IMediaTracker> mediaTracker,
            Lazy<IMenuService> menuService,
            Lazy<ICatalogSearchQueryAliasMapper> catalogSearchQueryAliasMapper,
            Lazy<IMediaMover> mediaMover,
            Lazy<IConfigureOptions<IdentityOptions>> identityOptionsConfigurer,
            IOptions<IdentityOptions> identityOptions,
            PrivacySettings privacySettings,
            Lazy<ModuleManager> moduleManager,
            ICacheManager cache)
        {
            _db = db;
            _currencyService = currencyService;
            _languageService = languageService;
            _localizedEntityService = localizedEntityService;
            _multiStoreSettingHelper = multiStoreSettingHelper;
            _dateTimeHelper = dateTimeHelper;
            _cookieManager = cookieManager;
            _providerManager = providerManager;
            _mediaTracker = mediaTracker;
            _menuService = menuService;
            _catalogSearchQueryAliasMapper = catalogSearchQueryAliasMapper;
            _mediaMover = mediaMover;
            _identityOptionsConfigurer = identityOptionsConfigurer;
            _identityOptions = identityOptions;
            _privacySettings = privacySettings;
            _moduleManager = moduleManager;
            _cache = cache;
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        public IActionResult AllSettings(SettingListModel model)
        {
            model.IsSingleStoreMode = Services.StoreContext.IsSingleStoreMode();
            return View(model);
        }

        #region All setting grid

        [HttpPost]
        [Permission(Permissions.Configuration.Setting.Read)]
        public async Task<IActionResult> SettingList(GridCommand command, SettingListModel model)
        {
            var stores = Services.StoreContext.GetAllStores();

            var query = _db.Settings.AsNoTracking();

            if (model.SearchSettingName.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Name, model.SearchSettingName);
            }

            if (model.SearchSettingValue.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Value, model.SearchSettingValue);
            }

            if (model.SearchStoreId != 0)
            {
                query = query.Where(x => x.StoreId == model.SearchStoreId);
            }

            var settings = await query
                .OrderBy(x => x.Name)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var allStoresStr = T("Admin.Common.StoresAll").Value;
            var allStoreNames = Services.StoreContext.GetAllStores().ToDictionary(x => x.Id, x => x.Name);

            var rows = settings
                .Select(x => new SettingModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    Value = x.Value,
                    StoreId = x.StoreId,
                    Store = x.StoreId == 0 ? allStoresStr : allStoreNames.Get(x.StoreId).NaIfEmpty()
                })
                .ToList();

            var gridModel = new GridModel<SettingModel>
            {
                Rows = rows,
                Total = await settings.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Setting.Update)]
        public async Task<IActionResult> SettingUpdate(SettingModel model)
        {
            model.Name = model.Name.Trim();

            if (model.Value.HasValue())
            {
                model.Value = model.Value.Trim();
            }
            model.StoreId = model.StoreId.GetValueOrDefault();

            var success = false;
            var setting = await _db.Settings.FindByIdAsync(model.Id);

            if (setting != null)
            {
                await MapperFactory.MapAsync(model, setting);
                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Setting.Create)]
        public async Task<IActionResult> SettingInsert(SettingModel model)
        {
            model.Name = model.Name.Trim();

            if (model.Value.HasValue())
            {
                model.Value = model.Value.Trim();
            }

            var success = true;
            var setting = new Setting();
            await MapperFactory.MapAsync(model, setting);
            _db.Settings.Add(setting);
            await _db.SaveChangesAsync();

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Setting.Delete)]
        public async Task<IActionResult> SettingDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var settings = await _db.Settings.GetManyAsync(ids, true);

                _db.Settings.RemoveRange(settings);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        #endregion

        [LoadSetting]
        public async Task<IActionResult> GeneralCommon(
            int storeScope,
            StoreInformationSettings storeInformationSettings,
            SeoSettings seoSettings,
            DateTimeSettings dateTimeSettings,
            SecuritySettings securitySettings,
            CaptchaSettings captchaSettings,
            PdfSettings pdfSettings,
            LocalizationSettings localizationSettings,
            CompanyInformationSettings companySettings,
            ContactDataSettings contactDataSettings,
            BankConnectionSettings bankConnectionSettings,
            SocialSettings socialSettings,
            HomePageSettings homePageSettings,
            EmailAccountSettings emailAccountSettings)
        {
            var model = new GeneralCommonSettingsModel();

            // Map entities to model.
            MiniMapper.Map(storeInformationSettings, model.StoreInformationSettings);
            MiniMapper.Map(seoSettings, model.SeoSettings);
            MiniMapper.Map(dateTimeSettings, model.DateTimeSettings);
            MiniMapper.Map(securitySettings, model.SecuritySettings);
            MiniMapper.Map(captchaSettings, model.CaptchaSettings);
            MiniMapper.Map(pdfSettings, model.PdfSettings);
            MiniMapper.Map(localizationSettings, model.LocalizationSettings);
            MiniMapper.Map(companySettings, model.CompanyInformationSettings);
            MiniMapper.Map(contactDataSettings, model.ContactDataSettings);
            MiniMapper.Map(bankConnectionSettings, model.BankConnectionSettings);
            MiniMapper.Map(socialSettings, model.SocialSettings);
            MiniMapper.Map(homePageSettings, model.HomepageSettings);
            MiniMapper.Map(emailAccountSettings, model.EmailAccountSettings);

            #region SEO custom mapping

            // Fix for Disallows & Allows joined with comma in MiniMapper (we need NewLine).
            model.SeoSettings.ExtraRobotsDisallows = seoSettings.ExtraRobotsDisallows != null ? string.Join(Environment.NewLine, seoSettings.ExtraRobotsDisallows) : string.Empty;
            model.SeoSettings.ExtraRobotsAllows = seoSettings.ExtraRobotsAllows != null ? string.Join(Environment.NewLine, seoSettings.ExtraRobotsAllows) : string.Empty;

            model.SeoSettings.MetaTitle = seoSettings.MetaTitle;
            model.SeoSettings.MetaDescription = seoSettings.MetaDescription;
            model.SeoSettings.MetaKeywords = seoSettings.MetaKeywords;

            AddLocales(model.SeoSettings.Locales, (locale, languageId) =>
            {
                locale.MetaTitle = seoSettings.GetLocalizedSetting(x => x.MetaTitle, languageId, storeScope, false, false);
                locale.MetaDescription = seoSettings.GetLocalizedSetting(x => x.MetaDescription, languageId, storeScope, false, false);
                locale.MetaKeywords = seoSettings.GetLocalizedSetting(x => x.MetaKeywords, languageId, storeScope, false, false);
            });

            model.HomepageSettings.MetaTitle = homePageSettings.MetaTitle;
            model.HomepageSettings.MetaDescription = homePageSettings.MetaDescription;
            model.HomepageSettings.MetaKeywords = homePageSettings.MetaKeywords;

            AddLocales(model.HomepageSettings.Locales, (locale, languageId) =>
            {
                locale.MetaTitle = homePageSettings.GetLocalizedSetting(x => x.MetaTitle, languageId, storeScope, false, false);
                locale.MetaDescription = homePageSettings.GetLocalizedSetting(x => x.MetaDescription, languageId, storeScope, false, false);
                locale.MetaKeywords = homePageSettings.GetLocalizedSetting(x => x.MetaKeywords, languageId, storeScope, false, false);
            });

            #endregion

            await PrepareGeneralCommonConfigurationModelAsync(emailAccountSettings);

            return View(model);
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, SaveSetting, FormValueRequired("save")]
        public async Task<IActionResult> GeneralCommon(
            GeneralCommonSettingsModel model,
            int storeScope,
            StoreInformationSettings storeInformationSettings,
            SeoSettings seoSettings,
            DateTimeSettings dateTimeSettings,
            SecuritySettings securitySettings,
            CaptchaSettings captchaSettings,
            PdfSettings pdfSettings,
            LocalizationSettings localizationSettings,
            CompanyInformationSettings companySettings,
            ContactDataSettings contactDataSettings,
            BankConnectionSettings bankConnectionSettings,
            SocialSettings socialSettings,
            HomePageSettings homePageSeoSettings,
            EmailAccountSettings emailAccountSettings)
        {
            if (!ModelState.IsValid)
            {
                return await GeneralCommon(storeScope,
                    storeInformationSettings,
                    seoSettings,
                    dateTimeSettings,
                    securitySettings,
                    captchaSettings,
                    pdfSettings,
                    localizationSettings,
                    companySettings,
                    contactDataSettings,
                    bankConnectionSettings,
                    socialSettings,
                    homePageSeoSettings,
                    emailAccountSettings);
            }

            ModelState.Clear();

            // Necessary before mapping
            var prevPdfLogoId = pdfSettings.LogoPictureId;

            // Map model to entities
            MiniMapper.Map(model.StoreInformationSettings, storeInformationSettings);
            MiniMapper.Map(model.SeoSettings, seoSettings);
            MiniMapper.Map(model.DateTimeSettings, dateTimeSettings);
            MiniMapper.Map(model.SecuritySettings, securitySettings);
            MiniMapper.Map(model.CaptchaSettings, captchaSettings);
            MiniMapper.Map(model.PdfSettings, pdfSettings);
            MiniMapper.Map(model.LocalizationSettings, localizationSettings);
            MiniMapper.Map(model.CompanyInformationSettings, companySettings);
            MiniMapper.Map(model.ContactDataSettings, contactDataSettings);
            MiniMapper.Map(model.BankConnectionSettings, bankConnectionSettings);
            MiniMapper.Map(model.SocialSettings, socialSettings);
            MiniMapper.Map(model.HomepageSettings, homePageSeoSettings);
            MiniMapper.Map(model.EmailAccountSettings, emailAccountSettings);

            #region POST mapping

            // Set CountryId explicitly else it can't be resetted.
            companySettings.CountryId = model.CompanyInformationSettings.CountryId ?? 0;

            // (Un)track PDF logo id
            await _mediaTracker.Value.TrackAsync(pdfSettings, prevPdfLogoId, x => x.LogoPictureId);

            seoSettings.MetaTitle = model.SeoSettings.MetaTitle;
            seoSettings.MetaDescription = model.SeoSettings.MetaDescription;
            seoSettings.MetaKeywords = model.SeoSettings.MetaKeywords;

            foreach (var localized in model.SeoSettings.Locales)
            {
                await _localizedEntityService.ApplyLocalizedSettingAsync(seoSettings, x => x.MetaTitle, localized.MetaTitle, localized.LanguageId, storeScope);
                await _localizedEntityService.ApplyLocalizedSettingAsync(seoSettings, x => x.MetaDescription, localized.MetaDescription, localized.LanguageId, storeScope);
                await _localizedEntityService.ApplyLocalizedSettingAsync(seoSettings, x => x.MetaKeywords, localized.MetaKeywords, localized.LanguageId, storeScope);
            }

            homePageSeoSettings.MetaTitle = model.HomepageSettings.MetaTitle;
            homePageSeoSettings.MetaDescription = model.HomepageSettings.MetaDescription;
            homePageSeoSettings.MetaKeywords = model.HomepageSettings.MetaKeywords;

            foreach (var localized in model.HomepageSettings.Locales)
            {
                await _localizedEntityService.ApplyLocalizedSettingAsync(homePageSeoSettings, x => x.MetaTitle, localized.MetaTitle, localized.LanguageId, storeScope);
                await _localizedEntityService.ApplyLocalizedSettingAsync(homePageSeoSettings, x => x.MetaDescription, localized.MetaDescription, localized.LanguageId, storeScope);
                await _localizedEntityService.ApplyLocalizedSettingAsync(homePageSeoSettings, x => x.MetaKeywords, localized.MetaKeywords, localized.LanguageId, storeScope);
            }

            await _db.SaveChangesAsync();

            #endregion

            // Does not contain any store specific settings.
            await Services.SettingFactory.SaveSettingsAsync(securitySettings);

            return NotifyAndRedirect(nameof(GeneralCommon));
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public async Task<IActionResult> Catalog(int storeScope, CatalogSettings catalogSettings, PriceSettings priceSettings)
        {
            var model = await MapperFactory.MapAsync<CatalogSettings, CatalogSettingsModel>(catalogSettings);
            await MapperFactory.MapAsync(priceSettings, model.PriceSettings);

            await PrepareCatalogConfigurationModelAsync(model);

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.OfferBadgeLabel = priceSettings.GetLocalizedSetting(x => x.OfferBadgeLabel, languageId, storeScope, false, false);
                locale.LimitedOfferBadgeLabel = priceSettings.GetLocalizedSetting(x => x.LimitedOfferBadgeLabel, languageId, storeScope, false, false);
            });

            return View(model);
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, SaveSetting]
        public async Task<IActionResult> Catalog(int storeScope, CatalogSettings catalogSettings, PriceSettings priceSettings, CatalogSettingsModel model)
        {
            if (!ModelState.IsValid)
            {
                return await Catalog(storeScope, catalogSettings, priceSettings);
            }

            ModelState.Clear();

            // We need to clear the sitemap cache if MaxItemsToDisplayInCatalogMenu has changed.
            if (catalogSettings.MaxItemsToDisplayInCatalogMenu != model.MaxItemsToDisplayInCatalogMenu
                || catalogSettings.ShowCategoryProductNumberIncludingSubcategories != model.ShowCategoryProductNumberIncludingSubcategories)
            {
                // Clear cached navigation model.
                await _menuService.Value.ClearCacheAsync("Main");
            }

            await MapperFactory.MapAsync(model, catalogSettings);
            await MapperFactory.MapAsync(model.PriceSettings, priceSettings);

            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedSettingAsync(priceSettings, x => x.OfferBadgeLabel, localized.OfferBadgeLabel, localized.LanguageId, storeScope);
                await _localizedEntityService.ApplyLocalizedSettingAsync(priceSettings, x => x.LimitedOfferBadgeLabel, localized.LimitedOfferBadgeLabel, localized.LanguageId, storeScope);
            }

            return NotifyAndRedirect(nameof(Catalog));
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public async Task<IActionResult> CustomerUser(
            int storeScope,
            CustomerSettings customerSettings,
            AddressSettings addressSettings,
            PrivacySettings privacySettings)
        {
            var model = new CustomerUserSettingsModel();

            await MapperFactory.MapAsync(customerSettings, model.CustomerSettings);
            await MapperFactory.MapAsync(addressSettings, model.AddressSettings);
            await MapperFactory.MapAsync(privacySettings, model.PrivacySettings);

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Salutations = addressSettings.GetLocalizedSetting(x => x.Salutations, languageId, storeScope, false, false);
            });

            return View(model);
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, SaveSetting]
        public async Task<IActionResult> CustomerUser(
            CustomerUserSettingsModel model,
            int storeScope,
            CustomerSettings customerSettings,
            AddressSettings addressSettings,
            PrivacySettings privacySettings)
        {
            var ignoreKey = $"{nameof(model.CustomerSettings)}.{nameof(model.CustomerSettings.RegisterCustomerRoleId)}";

            foreach (var key in ModelState.Keys.Where(x => x.EqualsNoCase(ignoreKey)))
            {
                ModelState[key].Errors.Clear();
            }

            if (!ModelState.IsValid)
            {
                return await CustomerUser(storeScope, customerSettings, addressSettings, privacySettings);
            }

            ModelState.Clear();

            var updateIdentity = ShouldUpdateIdentityOptions(model.CustomerSettings, customerSettings);
            await MapperFactory.MapAsync(model.CustomerSettings, customerSettings);

            if (updateIdentity)
            {
                // Save customerSettings now so new values can be applied in IdentityOptionsConfigurer.
                await Services.SettingFactory.SaveSettingsAsync(customerSettings, storeScope);
                _identityOptionsConfigurer.Value.Configure(_identityOptions.Value);
            }

            await MapperFactory.MapAsync(model.AddressSettings, addressSettings);

            var tempCookieInfos = privacySettings.CookieInfos;
            await MapperFactory.MapAsync(model.PrivacySettings, privacySettings);
            privacySettings.CookieInfos = tempCookieInfos;

            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedSettingAsync(addressSettings, x => x.Salutations, localized.Salutations, localized.LanguageId, storeScope);
            }

            await _db.SaveChangesAsync();

            return NotifyAndRedirect(nameof(CustomerUser));
        }

        private static bool ShouldUpdateIdentityOptions(CustomerUserSettingsModel.CustomerSettingsModel model, CustomerSettings settings)
        {
            if (model.PasswordMinLength != settings.PasswordMinLength
                || model.PasswordRequireDigit != settings.PasswordRequireDigit
                || model.PasswordRequireUppercase != settings.PasswordRequireUppercase
                || model.PasswordRequiredUniqueChars != settings.PasswordRequiredUniqueChars
                || model.PasswordRequireLowercase != settings.PasswordRequireLowercase
                || model.PasswordRequireNonAlphanumeric != settings.PasswordRequireNonAlphanumeric
                || model.CustomerNameAllowedCharacters != settings.CustomerNameAllowedCharacters)
            {
                return true;
            }

            return false;
        }

        public async Task<IActionResult> CookieInfoList()
        {
            var data = await _cookieManager.GetCookieInfosAsync();
            var systemCookies = string.Join(",", data.Select(x => x.Name).ToArray());

            if (_privacySettings.CookieInfos.HasValue())
            {
                data.AddRange(JsonConvert.DeserializeObject<List<CookieInfo>>(_privacySettings.CookieInfos)
                    .OrderBy(x => x.CookieType)
                    .ThenBy(x => x.Name));
            }

            var gridModel = new GridModel<CookieInfoModel>
            {
                Rows = data
                    .Select(x =>
                    {
                        return new CookieInfoModel
                        {
                            CookieType = x.CookieType,
                            Name = x.Name,
                            Description = x.Description,
                            IsPluginInfo = systemCookies.Contains(x.Name),
                            CookieTypeName = x.CookieType.ToString()
                        };
                    })
                    .ToList(),
                Total = data.Count
            };

            return Json(gridModel);
        }

        public async Task<IActionResult> CookieInfoDelete(GridSelection selection)
        {
            var numDeleted = 0;

            // First deserialize setting.
            var ciList = JsonConvert.DeserializeObject<List<CookieInfo>>(_privacySettings.CookieInfos);
            foreach (var name in selection.SelectedKeys)
            {
                ciList.Remove(x => x.Name.EqualsNoCase(name));
                numDeleted++;
            }

            // Now serialize again.
            _privacySettings.CookieInfos = JsonConvert.SerializeObject(ciList, Formatting.None);

            // Save setting.
            await Services.Settings.ApplySettingAsync(_privacySettings, x => x.CookieInfos);
            await _db.SaveChangesAsync();

            return Json(new { Success = true, Count = numDeleted });
        }

        public IActionResult CookieInfoCreatePopup()
        {
            var model = new CookieInfoModel();
            AddLocales(model.Locales);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CookieInfoCreatePopup(string btnId, string formId, CookieInfoModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Deserialize
            var ciList = JsonConvert.DeserializeObject<List<CookieInfo>>(_privacySettings.CookieInfos);

            ciList ??= new List<CookieInfo>();

            var cookieInfo = ciList
                .Select(x => x)
                .Where(x => x.Name.EqualsNoCase(model.Name))
                .FirstOrDefault();

            if (cookieInfo != null)
            {
                // Remove item if it's already there.
                ciList.Remove(x => x.Name.EqualsNoCase(cookieInfo.Name));
            }

            cookieInfo = new CookieInfo
            {
                CookieType = model.CookieType,
                Name = model.Name,
                Description = model.Description,
                SelectedStoreIds = model.SelectedStoreIds
            };

            ciList.Add(cookieInfo);

            // Serialize
            _privacySettings.CookieInfos = JsonConvert.SerializeObject(ciList, Formatting.None);

            // Now apply & save again.
            await Services.Settings.ApplySettingAsync(_privacySettings, x => x.CookieInfos);

            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(cookieInfo, x => x.Name, localized.Name, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(cookieInfo, x => x.Description, localized.Description, localized.LanguageId);
            }

            await _db.SaveChangesAsync();

            ViewBag.RefreshPage = true;
            ViewBag.btnId = btnId;
            ViewBag.formId = formId;

            return View(model);
        }

        public IActionResult CookieInfoEditPopup(string name)
        {
            var ciList = JsonConvert.DeserializeObject<List<CookieInfo>>(_privacySettings.CookieInfos);
            var cookieInfo = ciList
                .Where(x => x.Name.EqualsNoCase(name))
                .FirstOrDefault();

            if (cookieInfo == null)
            {
                NotifyError(T("Admin.Configuration.Settings.CustomerUser.Privacy.Cookies.CookieInfoNotFound"));
                return View(new CookieInfoModel());
            }

            var model = new CookieInfoModel
            {
                CookieType = cookieInfo.CookieType,
                Name = cookieInfo.Name,
                Description = cookieInfo.Description,
                SelectedStoreIds = cookieInfo.SelectedStoreIds
            };

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Name = cookieInfo.GetLocalized(x => x.Name, languageId, false, false);
                locale.Description = cookieInfo.GetLocalized(x => x.Description, languageId, false, false);
            });

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CookieInfoEditPopup(string btnId, string formId, CookieInfoModel model)
        {
            ViewBag.RefreshPage = true;
            ViewBag.btnId = btnId;
            ViewBag.formId = formId;

            var ciList = JsonConvert.DeserializeObject<List<CookieInfo>>(_privacySettings.CookieInfos);
            var cookieInfo = ciList
                .Where(x => x.Name.EqualsNoCase(model.Name))
                .FirstOrDefault();

            if (cookieInfo == null)
            {
                NotifyError(T("Admin.Configuration.Settings.CustomerUser.Privacy.Cookies.CookieInfoNotFound"));
                return View(new CookieInfoModel());
            }

            if (ModelState.IsValid)
            {
                cookieInfo.Name = model.Name;
                cookieInfo.Description = model.Description;
                cookieInfo.CookieType = model.CookieType;
                cookieInfo.SelectedStoreIds = model.SelectedStoreIds;

                ciList.Remove(x => x.Name.EqualsNoCase(cookieInfo.Name));
                ciList.Add(cookieInfo);

                _privacySettings.CookieInfos = JsonConvert.SerializeObject(ciList, Formatting.None);

                await Services.Settings.ApplySettingAsync(_privacySettings, x => x.CookieInfos);

                foreach (var localized in model.Locales)
                {
                    await _localizedEntityService.ApplyLocalizedValueAsync(cookieInfo, x => x.Name, localized.Name, localized.LanguageId);
                    await _localizedEntityService.ApplyLocalizedValueAsync(cookieInfo, x => x.Description, localized.Description, localized.LanguageId);
                }

                await _db.SaveChangesAsync();
            }

            return View(model);
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public async Task<IActionResult> Search(SearchSettings settings, int storeScope)
        {
            var megaSearchDescriptor = Services.ApplicationContext.ModuleCatalog.GetModuleByName("Smartstore.MegaSearch");
            var megaSearchPlusDescriptor = Services.ApplicationContext.ModuleCatalog.GetModuleByName("Smartstore.MegaSearchPlus");

            var model = new SearchSettingsModel();
            MiniMapper.Map(settings, model);

            model.IsMegaSearchInstalled = megaSearchDescriptor != null;

            PrepareSearchConfigModel(model, settings, megaSearchPlusDescriptor);

            // Common facets.
            model.BrandFacet.Disabled = settings.BrandDisabled;
            model.BrandFacet.DisplayOrder = settings.BrandDisplayOrder;
            model.PriceFacet.Disabled = settings.PriceDisabled;
            model.PriceFacet.DisplayOrder = settings.PriceDisplayOrder;
            model.RatingFacet.Disabled = settings.RatingDisabled;
            model.RatingFacet.DisplayOrder = settings.RatingDisplayOrder;
            model.DeliveryTimeFacet.Disabled = settings.DeliveryTimeDisabled;
            model.DeliveryTimeFacet.DisplayOrder = settings.DeliveryTimeDisplayOrder;
            model.AvailabilityFacet.Disabled = settings.AvailabilityDisabled;
            model.AvailabilityFacet.DisplayOrder = settings.AvailabilityDisplayOrder;
            model.AvailabilityFacet.IncludeNotAvailable = settings.IncludeNotAvailable;
            model.NewArrivalsFacet.Disabled = settings.NewArrivalsDisabled;
            model.NewArrivalsFacet.DisplayOrder = settings.NewArrivalsDisplayOrder;

            await _multiStoreSettingHelper.DetectOverrideKeysAsync(settings, model);

            // Localized facet settings (CommonFacetSettingsLocalizedModel).
            var i = 0;
            foreach (var language in _languageService.GetAllLanguages(true))
            {
                var categoryFacetAliasSettingsKey = FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Category, language.Id);
                var brandFacetAliasSettingsKey = FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Brand, language.Id);
                var priceFacetAliasSettingsKey = FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Price, language.Id);
                var ratingFacetAliasSettingsKey = FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Rating, language.Id);
                var deliveryTimeFacetAliasSettingsKey = FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.DeliveryTime, language.Id);
                var availabilityFacetAliasSettingsKey = FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Availability, language.Id);
                var newArrivalsFacetAliasSettingsKey = FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.NewArrivals, language.Id);

                await _multiStoreSettingHelper.DetectOverrideKeyAsync($"CategoryFacet.Locales[{i}].Alias", categoryFacetAliasSettingsKey);
                await _multiStoreSettingHelper.DetectOverrideKeyAsync($"BrandFacet.Locales[{i}].Alias", brandFacetAliasSettingsKey);
                await _multiStoreSettingHelper.DetectOverrideKeyAsync($"PriceFacet.Locales[{i}].Alias", priceFacetAliasSettingsKey);
                await _multiStoreSettingHelper.DetectOverrideKeyAsync($"RatingFacet.Locales[{i}].Alias", ratingFacetAliasSettingsKey);
                await _multiStoreSettingHelper.DetectOverrideKeyAsync($"DeliveryTimeFacet.Locales[{i}].Alias", deliveryTimeFacetAliasSettingsKey);
                await _multiStoreSettingHelper.DetectOverrideKeyAsync($"AvailabilityFacet.Locales[{i}].Alias", availabilityFacetAliasSettingsKey);
                await _multiStoreSettingHelper.DetectOverrideKeyAsync($"NewArrivalsFacet.Locales[{i}].Alias", newArrivalsFacetAliasSettingsKey);

                model.CategoryFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(categoryFacetAliasSettingsKey, storeId: storeScope)
                });
                model.BrandFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(brandFacetAliasSettingsKey, storeId: storeScope)
                });
                model.PriceFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(priceFacetAliasSettingsKey, storeId: storeScope)
                });
                model.RatingFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(ratingFacetAliasSettingsKey, storeId: storeScope)
                });
                model.DeliveryTimeFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(deliveryTimeFacetAliasSettingsKey, storeId: storeScope)
                });
                model.AvailabilityFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(availabilityFacetAliasSettingsKey, storeId: storeScope)
                });
                model.NewArrivalsFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(newArrivalsFacetAliasSettingsKey, storeId: storeScope)
                });

                i++;
            }

            // Facet settings (CommonFacetSettingsModel).
            foreach (var prefix in new[] { "Brand", "Price", "Rating", "DeliveryTime", "Availability", "NewArrivals" })
            {
                await _multiStoreSettingHelper.DetectOverrideKeyAsync(prefix + "Facet.Disabled", prefix + "Disabled", settings);
                await _multiStoreSettingHelper.DetectOverrideKeyAsync(prefix + "Facet.DisplayOrder", prefix + "DisplayOrder", settings);
            }

            // Facet settings with a non-prefixed name.
            await _multiStoreSettingHelper.DetectOverrideKeyAsync("AvailabilityFacet.IncludeNotAvailable", "IncludeNotAvailable", settings);

            return View(model);
        }

        // INFO: do not use SaveSetting attribute here because it would delete all previously added facet settings if storeScope > 0.
        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, LoadSetting]
        public async Task<IActionResult> Search(SearchSettingsModel model, SearchSettings settings, int storeScope)
        {
            if (!ModelState.IsValid)
            {
                return await Search(settings, storeScope);
            }

            var form = Request.Form;
            CategoryTreeChangeReason? categoriesChange = model.AvailabilityFacet.IncludeNotAvailable != settings.IncludeNotAvailable
                ? CategoryTreeChangeReason.ElementCounts
                : null;

            ModelState.Clear();

            settings = ((ISettings)settings).Clone() as SearchSettings;
            MiniMapper.Map(model, settings);

            // Common facets.
            settings.BrandDisabled = model.BrandFacet.Disabled;
            settings.BrandDisplayOrder = model.BrandFacet.DisplayOrder;
            settings.PriceDisabled = model.PriceFacet.Disabled;
            settings.PriceDisplayOrder = model.PriceFacet.DisplayOrder;
            settings.RatingDisabled = model.RatingFacet.Disabled;
            settings.RatingDisplayOrder = model.RatingFacet.DisplayOrder;
            settings.DeliveryTimeDisabled = model.DeliveryTimeFacet.Disabled;
            settings.DeliveryTimeDisplayOrder = model.DeliveryTimeFacet.DisplayOrder;
            settings.AvailabilityDisabled = model.AvailabilityFacet.Disabled;
            settings.AvailabilityDisplayOrder = model.AvailabilityFacet.DisplayOrder;
            settings.IncludeNotAvailable = model.AvailabilityFacet.IncludeNotAvailable;
            settings.NewArrivalsDisabled = model.NewArrivalsFacet.Disabled;
            settings.NewArrivalsDisplayOrder = model.NewArrivalsFacet.DisplayOrder;

            await _multiStoreSettingHelper.UpdateSettingsAsync(settings, form);

            // Facet settings (CommonFacetSettingsModel).
            if (storeScope != 0)
            {
                foreach (var prefix in new[] { "Brand", "Price", "Rating", "DeliveryTime", "Availability", "NewArrivals" })
                {
                    await _multiStoreSettingHelper.ApplySettingAsync(prefix + "Facet.Disabled", prefix + "Disabled", settings, form);
                    await _multiStoreSettingHelper.ApplySettingAsync(prefix + "Facet.DisplayOrder", prefix + "DisplayOrder", settings, form);
                }
            }

            // Facet settings with a non-prefixed name.
            await _multiStoreSettingHelper.ApplySettingAsync("AvailabilityFacet.IncludeNotAvailable", "IncludeNotAvailable", settings, form);

            // Localized facet settings (CommonFacetSettingsLocalizedModel).
            var num = 0;
            num += await ApplyLocalizedFacetSettings(model.CategoryFacet, FacetGroupKind.Category, storeScope);
            num += await ApplyLocalizedFacetSettings(model.BrandFacet, FacetGroupKind.Brand, storeScope);
            num += await ApplyLocalizedFacetSettings(model.PriceFacet, FacetGroupKind.Price, storeScope);
            num += await ApplyLocalizedFacetSettings(model.RatingFacet, FacetGroupKind.Rating, storeScope);
            num += await ApplyLocalizedFacetSettings(model.DeliveryTimeFacet, FacetGroupKind.DeliveryTime, storeScope);
            num += await ApplyLocalizedFacetSettings(model.AvailabilityFacet, FacetGroupKind.Availability, storeScope);
            num += await ApplyLocalizedFacetSettings(model.NewArrivalsFacet, FacetGroupKind.NewArrivals, storeScope);

            await _db.SaveChangesAsync();

            if (num > 0)
            {
                await _catalogSearchQueryAliasMapper.Value.ClearCommonFacetCacheAsync();
            }

            if (categoriesChange.HasValue)
            {
                await Services.EventPublisher.PublishAsync(new CategoryTreeChangedEvent(categoriesChange.Value));
            }

            await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, settings, form));

            return NotifyAndRedirect(nameof(Search));
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public IActionResult DataExchange(DataExchangeSettings settings)
        {
            var model = new DataExchangeSettingsModel();
            MiniMapper.Map(settings, model);

            return View(model);
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, SaveSetting]
        public IActionResult DataExchange(DataExchangeSettings settings, DataExchangeSettingsModel model)
        {
            if (!ModelState.IsValid)
            {
                return DataExchange(settings);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            return NotifyAndRedirect(nameof(DataExchange));
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public async Task<IActionResult> Media(MediaSettings mediaSettings)
        {
            var model = await MapperFactory.MapAsync<MediaSettings, MediaSettingsModel>(mediaSettings);

            model.CurrentlyAllowedThumbnailSizes = mediaSettings.GetAllowedThumbnailSizes();

            // Media storage provider.
            var currentStorageProvider = Services.Settings.GetSettingByKey<string>("Media.Storage.Provider");
            var provider = _providerManager.GetProvider<IMediaStorageProvider>(currentStorageProvider);

            model.StorageProvider = provider != null ? _moduleManager.Value.GetLocalizedFriendlyName(provider.Metadata) : null;

            ViewBag.AvailableStorageProviders = _providerManager.GetAllProviders<IMediaStorageProvider>()
                .Where(x => !x.Metadata.SystemName.EqualsNoCase(currentStorageProvider))
                .Select(x => new SelectListItem { Text = _moduleManager.Value.GetLocalizedFriendlyName(x.Metadata), Value = x.Metadata.SystemName })
                .ToList();

            return View(model);
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, FormValueRequired("save")]
        [SaveSetting]
        public async Task<IActionResult> Media(MediaSettings settings, MediaSettingsModel model)
        {
            if (!ModelState.IsValid)
            {
                return await Media(settings);
            }

            ModelState.Clear();
            await MapperFactory.MapAsync(model, settings);

            return NotifyAndRedirect(nameof(Media));
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost]
        public async Task<IActionResult> ChangeMediaStorage(string targetProvider)
        {
            var currentStorageProvider = Services.Settings.GetSettingByKey<string>("Media.Storage.Provider");
            var source = _providerManager.GetProvider<IMediaStorageProvider>(currentStorageProvider);
            var target = _providerManager.GetProvider<IMediaStorageProvider>(targetProvider);

            var success = await _mediaMover.Value.MoveAsync(source, target);

            if (success)
            {
                NotifySuccess(T("Admin.Common.TaskSuccessfullyProcessed"));
            }

            return RedirectToAction("Media");
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public async Task<IActionResult> Payment(PaymentSettings settings)
        {
            var model = await MapperFactory.MapAsync<PaymentSettings, PaymentSettingsModel>(settings);
            var providers = _providerManager.GetAllProviders<IPaymentMethod>();

            var selectListItems = providers
                .Where(x => x.IsPaymentProviderEnabled(settings))
                .Select(x => new SelectListItem { Text = _moduleManager.Value.GetLocalizedFriendlyName(x.Metadata), Value = x.Metadata.SystemName })
                .ToList();

            ViewBag.ActivePaymentMethods = new MultiSelectList(selectListItems, "Value", "Text", model.ProductDetailPaymentMethodSystemNames);

            return View(model);
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, SaveSetting]
        public async Task<IActionResult> Payment(PaymentSettings settings, PaymentSettingsModel model)
        {
            if (!ModelState.IsValid)
            {
                return await Payment(settings);
            }

            ModelState.Clear();

            await MapperFactory.MapAsync(model, settings);

            await _cache.RemoveByPatternAsync(PaymentService.ProductDetailPaymentIconsPatternKey);

            return NotifyAndRedirect(nameof(Payment));
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public async Task<IActionResult> Finance(int storeScope, TaxSettings taxSettings, CurrencySettings currencySettings)
        {
            var model = new FinanceSettingsModel();

            await MapperFactory.MapAsync(taxSettings, model.TaxSettings);
            await MapperFactory.MapAsync(currencySettings, model.CurrencySettings);

            var taxCategories = await _db.TaxCategories
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            var shippingTaxCategories = new List<SelectListItem>();
            var paymentMethodAdditionalFeeTaxCategories = new List<SelectListItem>();

            foreach (var tc in taxCategories)
            {
                shippingTaxCategories.Add(new()
                {
                    Text = tc.Name,
                    Value = tc.Id.ToString(), 
                    Selected = tc.Id == taxSettings.ShippingTaxClassId
                });

                paymentMethodAdditionalFeeTaxCategories.Add(new()
                {
                    Text = tc.Name,
                    Value = tc.Id.ToString(),
                    Selected = tc.Id == taxSettings.PaymentMethodAdditionalFeeTaxClassId
                });
            }

            ViewBag.ShippingTaxCategories = shippingTaxCategories;
            ViewBag.PaymentMethodAdditionalFeeTaxCategories = paymentMethodAdditionalFeeTaxCategories;

            // Default tax address.
            var defaultAddress = await _db.Addresses.FindByIdAsync(taxSettings.DefaultTaxAddressId, false);
            var stateProvinces = await _db.StateProvinces.GetStateProvincesByCountryIdAsync(defaultAddress?.CountryId ?? 0, true);

            if (defaultAddress != null)
            {
                MiniMapper.Map(defaultAddress, model.TaxSettings.DefaultTaxAddress);
            }

            if (storeScope > 0 && await Services.Settings.SettingExistsAsync(taxSettings, x => x.DefaultTaxAddressId, storeScope))
            {
                _multiStoreSettingHelper.AddOverrideKey(taxSettings, nameof(model.TaxSettings.DefaultTaxAddress));
            }

            model.TaxSettings.DefaultTaxAddress.AvailableStates = stateProvinces.ToSelectListItems(defaultAddress?.StateProvinceId ?? 0) ?? new List<SelectListItem>
            {
                new() { Text = T("Address.OtherNonUS"), Value = "0" }
            };

            model.TaxSettings.DefaultTaxAddress.FirstNameEnabled = false;
            model.TaxSettings.DefaultTaxAddress.LastNameEnabled = false;
            model.TaxSettings.DefaultTaxAddress.EmailEnabled = false;
            model.TaxSettings.DefaultTaxAddress.CountryEnabled = true;
            model.TaxSettings.DefaultTaxAddress.StateProvinceEnabled = true;
            model.TaxSettings.DefaultTaxAddress.ZipPostalCodeEnabled = true;
            model.TaxSettings.DefaultTaxAddress.ZipPostalCodeRequired = true;

            ViewBag.ExchangeRateProviders = _currencyService.LoadAllExchangeRateProviders()
                .Select(x => new SelectListItem
                {
                    Text = _moduleManager.Value.GetLocalizedFriendlyName(x.Metadata),
                    Value = x.Metadata.SystemName,
                    Selected = x.Metadata.SystemName.EqualsNoCase(currencySettings.ActiveExchangeRateProviderSystemName)
                })
                .ToList();

            return View(model);
        }

        // INFO: do not use SaveSetting attribute here because it would delete a previously added default tax address if storeScope > 0.
        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, LoadSetting]
        public async Task<IActionResult> Finance(int storeScope, FinanceSettingsModel model, TaxSettings taxSettings, CurrencySettings currencySettings)
        {
            var form = Request.Form;

            if (!ModelState.IsValid)
            {
                return await Finance(storeScope, taxSettings, currencySettings);
            }

            ModelState.Clear();

            await MapperFactory.MapAsync(model.TaxSettings, taxSettings);
            await MapperFactory.MapAsync(model.CurrencySettings, currencySettings);

            await _multiStoreSettingHelper.UpdateSettingsAsync(taxSettings, form, propertyName =>
            {
                // Skip to prevent the address from being recreated every time you save.
                if (propertyName.EqualsNoCase(nameof(taxSettings.DefaultTaxAddressId)))
                    return string.Empty;

                return propertyName;
            });

            await _multiStoreSettingHelper.UpdateSettingsAsync(currencySettings, form);

            // Special case DefaultTaxAddressId\DefaultTaxAddress.
            var deleteAddressId = 0;
            if (storeScope == 0 || MultiStoreSettingHelper.IsOverrideChecked(taxSettings, nameof(FinanceSettingsModel.TaxSettingsModel.DefaultTaxAddress), form))
            {
                var addressId = await Services.Settings.SettingExistsAsync(taxSettings, x => x.DefaultTaxAddressId, storeScope) ? taxSettings.DefaultTaxAddressId : 0;
                var defaultAddress = await _db.Addresses.FindByIdAsync(addressId) ?? new Address { CreatedOnUtc = DateTime.UtcNow };

                // Update DefaultTaxAddressId (in case we are in multistore configuration mode it will be set to the shared one).
                model.TaxSettings.DefaultTaxAddress.Id = defaultAddress.Id == 0 ? 0 : addressId;
                await MapperFactory.MapAsync(model.TaxSettings.DefaultTaxAddress, defaultAddress);

                if (defaultAddress.Id == 0)
                {
                    _db.Addresses.Add(defaultAddress);
                    await _db.SaveChangesAsync();
                }

                taxSettings.DefaultTaxAddressId = defaultAddress.Id;
                await Services.Settings.ApplySettingAsync(taxSettings, x => x.DefaultTaxAddressId, storeScope);
            }
            else
            {
                deleteAddressId = taxSettings.DefaultTaxAddressId;
                await Services.Settings.RemoveSettingAsync(taxSettings, x => x.DefaultTaxAddressId, storeScope);
            }

            await _db.SaveChangesAsync();
            await CheckToDeleteAddress(deleteAddressId, $"{nameof(TaxSettings)}.{nameof(TaxSettings.DefaultTaxAddressId)}");

            return NotifyAndRedirect(nameof(Finance));
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public IActionResult RewardPoints(RewardPointsSettings settings)
        {
            var model = MiniMapper.Map<RewardPointsSettings, RewardPointsSettingsModel>(settings);

            model.PrimaryStoreCurrencyCode = _currencyService.PrimaryCurrency.CurrencyCode;

            return View(model);
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, LoadSetting]
        public async Task<IActionResult> RewardPoints(RewardPointsSettingsModel model, RewardPointsSettings settings, int storeScope)
        {
            if (!ModelState.IsValid)
            {
                return RewardPoints(settings);
            }

            var form = Request.Form;

            ModelState.Clear();

            settings = ((ISettings)settings).Clone() as RewardPointsSettings;
            MiniMapper.Map(model, settings);

            await _multiStoreSettingHelper.UpdateSettingsAsync(settings, form);

            if (storeScope != 0 && MultiStoreSettingHelper.IsOverrideChecked(settings, nameof(RewardPointsSettings.PointsForPurchases_Amount), form))
            {
                await Services.Settings.ApplySettingAsync(settings, x => x.PointsForPurchases_Points, storeScope);
            }

            await _db.SaveChangesAsync();

            return NotifyAndRedirect(nameof(RewardPoints));
        }

        public IActionResult RewardPointsForPurchasesInfo(decimal amount, int points)
        {
            if (amount == decimal.Zero && points == 0)
            {
                return new EmptyResult();
            }

            var amountFormatted = _currencyService.ConvertFromPrimaryCurrency(amount, Services.WorkContext.WorkingCurrency).ToString();
            var info = T("RewardPoints.PointsForPurchasesInfo", amountFormatted, points.ToString("N0"));

            return Content(info);
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public async Task<IActionResult> ShoppingCart(int storeScope, ShoppingCartSettings settings)
        {
            var model = await MapperFactory.MapAsync<ShoppingCartSettings, ShoppingCartSettingsModel>(settings);

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.ThirdPartyEmailHandOverLabel = settings.GetLocalizedSetting(x => x.ThirdPartyEmailHandOverLabel, languageId, storeScope, false, false);
            });

            ViewBag.Checkouts = new List<ExtendedSelectListItem>
            {
                CreateCheckoutProcessItem(CheckoutProcess.Standard),
                CreateCheckoutProcessItem(CheckoutProcess.Terminal),
                CreateCheckoutProcessItem(CheckoutProcess.TerminalWithPayment)
            };

            return View(model);

            ExtendedSelectListItem CreateCheckoutProcessItem(string process)
            {
                var item = new ExtendedSelectListItem
                {
                    Text = T("Checkout.Process." + process),
                    Value = process,
                    Selected = settings.CheckoutProcess.EqualsNoCase(process)
                };

                item.CustomProperties["Description"] = T($"Checkout.Process.{process}.Hint").Value;
                return item;
            }
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, SaveSetting]
        public async Task<IActionResult> ShoppingCart(ShoppingCartSettings settings, ShoppingCartSettingsModel model, int storeScope)
        {
            if (!ModelState.IsValid)
            {
                return await ShoppingCart(storeScope, settings);
            }

            ModelState.Clear();

            await MapperFactory.MapAsync(model, settings);

            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedSettingAsync(settings, x => x.ThirdPartyEmailHandOverLabel, localized.ThirdPartyEmailHandOverLabel, localized.LanguageId, storeScope);
            }

            return NotifyAndRedirect(nameof(ShoppingCart));
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public async Task<IActionResult> Shipping(int storeScope, ShippingSettings settings)
        {
            var model = await MapperFactory.MapAsync<ShippingSettings, ShippingSettingsModel>(settings);

            model.PrimaryStoreCurrencyCode = _currencyService.PrimaryCurrency.CurrencyCode;

            var todayShipmentHours = new List<SelectListItem>();

            for (var i = 1; i <= 24; ++i)
            {
                var hourStr = i.ToString();
                todayShipmentHours.Add(new()
                {
                    Text = hourStr,
                    Value = hourStr,
                    Selected = settings.TodayShipmentHour == i
                });
            }

            ViewBag.TodayShipmentHours = todayShipmentHours;

            // Shipping origin address.
            var originAddress = await _db.Addresses.FindByIdAsync(settings.ShippingOriginAddressId, false);
            var stateProvinces = await _db.StateProvinces.GetStateProvincesByCountryIdAsync(originAddress?.CountryId ?? 0, true);

            if (originAddress != null)
            {
                MiniMapper.Map(originAddress, model.ShippingOriginAddress);
            }

            if (storeScope > 0 && await Services.Settings.SettingExistsAsync(settings, x => x.ShippingOriginAddressId, storeScope))
            {
                _multiStoreSettingHelper.AddOverrideKey(null, nameof(model.ShippingOriginAddress));
            }

            model.ShippingOriginAddress.AvailableStates = stateProvinces.ToSelectListItems(originAddress?.StateProvinceId ?? 0) ?? new List<SelectListItem>
            {
                new() { Text = T("Address.OtherNonUS"), Value = "0" }
            };

            model.ShippingOriginAddress.FirstNameEnabled = false;
            model.ShippingOriginAddress.LastNameEnabled = false;
            model.ShippingOriginAddress.EmailEnabled = false;
            model.ShippingOriginAddress.CountryEnabled = true;
            model.ShippingOriginAddress.StateProvinceEnabled = true;
            model.ShippingOriginAddress.ZipPostalCodeEnabled = true;
            model.ShippingOriginAddress.ZipPostalCodeRequired = true;

            return View(model);
        }

        // INFO: do not use SaveSetting attribute here because it would delete a previously added origin shipping address if storeScope > 0.
        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, LoadSetting]
        public async Task<IActionResult> Shipping(int storeScope, ShippingSettings settings, ShippingSettingsModel model)
        {
            var form = Request.Form;

            if (!ModelState.IsValid)
            {
                return await Shipping(storeScope, settings);
            }

            ModelState.Clear();

            await MapperFactory.MapAsync(model, settings);

            await _multiStoreSettingHelper.UpdateSettingsAsync(settings, form, propertyName =>
            {
                // Skip to prevent the address from being recreated every time you save.
                if (propertyName.EqualsNoCase(nameof(settings.ShippingOriginAddressId)))
                    return string.Empty;

                return propertyName;
            });

            // Special case ShippingOriginAddressId\ShippingOriginAddress.
            var deleteAddressId = 0;
            if (storeScope == 0 || MultiStoreSettingHelper.IsOverrideChecked(settings, nameof(ShippingSettingsModel.ShippingOriginAddress), form))
            {
                var addressId = await Services.Settings.SettingExistsAsync(settings, x => x.ShippingOriginAddressId, storeScope) ? settings.ShippingOriginAddressId : 0;
                var originAddress = await _db.Addresses.FindByIdAsync(addressId) ?? new Address { CreatedOnUtc = DateTime.UtcNow };

                // Update DefaultTaxAddressId (in case we are in multistore configuration mode it will be set to the shared one).
                model.ShippingOriginAddress.Id = originAddress.Id == 0 ? 0 : addressId;
                await MapperFactory.MapAsync(model.ShippingOriginAddress, originAddress);

                if (originAddress.Id == 0)
                {
                    _db.Addresses.Add(originAddress);
                    await _db.SaveChangesAsync();
                }

                settings.ShippingOriginAddressId = originAddress.Id;
                await Services.Settings.ApplySettingAsync(settings, x => x.ShippingOriginAddressId, storeScope);
            }
            else
            {
                deleteAddressId = settings.ShippingOriginAddressId;
                await Services.Settings.RemoveSettingAsync(settings, x => x.ShippingOriginAddressId, storeScope);
            }

            await _db.SaveChangesAsync();
            await CheckToDeleteAddress(deleteAddressId, $"{nameof(ShippingSettings)}.{nameof(ShippingSettings.ShippingOriginAddressId)}");

            return NotifyAndRedirect(nameof(Shipping));
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public async Task<IActionResult> Order(int storeScope, OrderSettings settings)
        {
            var allStores = Services.StoreContext.GetAllStores();
            var store = storeScope == 0 ? Services.StoreContext.CurrentStore : allStores.FirstOrDefault(x => x.Id == storeScope);
            var model = await MapperFactory.MapAsync<OrderSettings, OrderSettingsModel>(settings);

            model.PrimaryStoreCurrencyCode = _currencyService.PrimaryCurrency.CurrencyCode;
            model.StoreCount = allStores.Count;

            if (settings.GiftCards_Activated_OrderStatusId > 0)
            {
                model.GiftCardsActivatedOrderStatusId = settings.GiftCards_Activated_OrderStatusId;
            }

            if (settings.GiftCards_Deactivated_OrderStatusId > 0)
            {
                model.GiftCardsDeactivatedOrderStatusId = settings.GiftCards_Deactivated_OrderStatusId;
            }

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.ReturnRequestActions = settings.GetLocalizedSetting(x => x.ReturnRequestActions, languageId, storeScope, false, false);
                locale.ReturnRequestReasons = settings.GetLocalizedSetting(x => x.ReturnRequestReasons, languageId, storeScope, false, false);
            });

            model.OrderIdent = _db.DataProvider.GetTableIdent<Order>();

            return View(model);
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, SaveSetting]
        public async Task<IActionResult> Order(OrderSettings settings, OrderSettingsModel model, int storeScope)
        {
            if (!ModelState.IsValid)
            {
                return await Order(storeScope, settings);
            }

            ModelState.Clear();

            await MapperFactory.MapAsync(model, settings);
            settings.GiftCards_Activated_OrderStatusId = Convert.ToInt32(model.GiftCardsActivatedOrderStatusId);
            settings.GiftCards_Deactivated_OrderStatusId = Convert.ToInt32(model.GiftCardsDeactivatedOrderStatusId);

            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedSettingAsync(settings, x => x.ReturnRequestActions, localized.ReturnRequestActions, localized.LanguageId, storeScope);
                await _localizedEntityService.ApplyLocalizedSettingAsync(settings, x => x.ReturnRequestReasons, localized.ReturnRequestReasons, localized.LanguageId, storeScope);
            }

            await _db.SaveChangesAsync();

            // Order ident.
            if (model.OrderIdent.HasValue)
            {
                try
                {
                    _db.DataProvider.SetTableIdent<Order>(model.OrderIdent.Value);
                }
                catch (Exception ex)
                {
                    NotifyError(ex.Message);
                }
            }

            return NotifyAndRedirect(nameof(Order));
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [HttpPost, LoadSetting]
        public IActionResult TestSeoNameCreation(SeoSettings settings, GeneralCommonSettingsModel model)
        {
            // We always test against persisted settings.
            var result = SlugUtility.Slugify(
                model.SeoSettings.TestSeoNameCreation,
                settings.ConvertNonWesternChars,
                settings.AllowUnicodeCharsInUrls,
                SeoSettings.CreateCharConversionMap(settings.SeoNameCharConversion));

            return Content(result);
        }

        public async Task<IActionResult> ChangeStoreScopeConfiguration(int storeid, string returnUrl = "")
        {
            var store = Services.StoreContext.GetStoreById(storeid);
            if (store != null || storeid == 0)
            {
                Services.WorkContext.CurrentCustomer.GenericAttributes.AdminAreaStoreScopeConfiguration = storeid;
                await _db.SaveChangesAsync();
            }

            return RedirectToReferrer(returnUrl, () => RedirectToAction("Index", "Home", new { area = "Admin" }));
        }

        private async Task<int> ApplyLocalizedFacetSettings(CommonFacetSettingsModel model, FacetGroupKind kind, int storeId = 0)
        {
            var num = 0;

            foreach (var localized in model.Locales)
            {
                var key = FacetUtility.GetFacetAliasSettingKey(kind, localized.LanguageId);
                var existingAlias = Services.Settings.GetSettingByKey<string>(key, storeId: storeId);

                if (existingAlias.EqualsNoCase(localized.Alias))
                {
                    continue;
                }

                if (localized.Alias.HasValue())
                {
                    await Services.Settings.ApplySettingAsync(key, localized.Alias, storeId);
                }
                else
                {
                    await Services.Settings.RemoveSettingAsync(key, storeId);
                }

                num++;
            }

            return num;
        }

        private RedirectToActionResult NotifyAndRedirect(string actionMethod)
        {
            NotifySuccess(T("Admin.Configuration.Updated"));
            return RedirectToAction(actionMethod);
        }

        private async Task PrepareGeneralCommonConfigurationModelAsync(EmailAccountSettings emailAccountSettings)
        {
            ViewBag.AvailableTimeZones = _dateTimeHelper.GetSystemTimeZones()
                .ToSelectListItems(_dateTimeHelper.DefaultStoreTimeZone.Id);

            var emailAccounts = await _db.EmailAccounts
                .AsNoTracking()
                .AsNoCaching()
                .OrderBy(x => x.Id)
                .ToListAsync();

            ViewBag.EmailAccounts = emailAccounts
                .Select(x => new SelectListItem { Text = x.FriendlyName, Value = x.Id.ToString(), Selected = x.Id == emailAccountSettings.DefaultEmailAccountId })
                .ToList();

            #region CompanyInfo custom mapping

            ViewBag.Salutations = new List<SelectListItem>();
            ViewBag.Salutations.AddRange(new[]
            {
                ResToSelectListItem("Admin.Address.Salutation.Mr"),
                ResToSelectListItem("Admin.Address.Salutation.Mrs")
            });

            var resRoot = "Admin.Configuration.Settings.GeneralCommon.CompanyInformationSettings.ManagementDescriptions.";
            ViewBag.ManagementDescriptions = new List<SelectListItem>();
            ViewBag.ManagementDescriptions.AddRange(new[]
            {
                ResToSelectListItem(resRoot + "Manager"),
                ResToSelectListItem(resRoot + "Shopkeeper"),
                ResToSelectListItem(resRoot + "Procurator"),
                ResToSelectListItem(resRoot + "Shareholder"),
                ResToSelectListItem(resRoot + "AuthorizedPartner"),
                ResToSelectListItem(resRoot + "Director"),
                ResToSelectListItem(resRoot + "ManagingPartner")
            });

            ViewBag.AvailableMetaContentValues = new List<SelectListItem>
            {
                new() { Text = "index", Value = "index" },
                new() { Text = "noindex", Value = "noindex" },
                new() { Text = "index, follow", Value = "index, follow" },
                new() { Text = "index, nofollow", Value = "index, nofollow" },
                new() { Text = "noindex, follow", Value = "noindex, follow" },
                new() { Text = "noindex, nofollow", Value = "noindex, nofollow" }
            };

            #endregion
        }

        private async Task PrepareCatalogConfigurationModelAsync(CatalogSettingsModel model)
        {
            ViewBag.AvailableDefaultViewModes = new List<SelectListItem>
            {
                new SelectListItem { Value = "grid", Text = T("Common.Grid"), Selected = model.DefaultViewMode.EqualsNoCase("grid") },
                new SelectListItem { Value = "list", Text = T("Common.List"), Selected = model.DefaultViewMode.EqualsNoCase("list") }
            };

            var priceLabels = await _db.PriceLabels
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            ViewBag.AvailableDefaultComparePriceLabels = new List<SelectListItem>();
            ViewBag.AvailableDefaultRegularPriceLabels = new List<SelectListItem>();

            foreach (var label in priceLabels)
            {
                ViewBag.AvailableDefaultComparePriceLabels.Add(new SelectListItem { 
                    Value = label.Id.ToString(), 
                    Text = label.GetLocalized(x => x.ShortName), 
                    Selected = model.PriceSettings.DefaultComparePriceLabelId == label.Id
                });

                ViewBag.AvailableDefaultRegularPriceLabels.Add(new SelectListItem
                {
                    Value = label.Id.ToString(),
                    Text = label.GetLocalized(x => x.ShortName),
                    Selected = model.PriceSettings.DefaultRegularPriceLabelId == label.Id
                });
            }

            ViewBag.LimitedOfferBadgeStyles = AddBadgeStyles(model.PriceSettings.LimitedOfferBadgeStyle);
            ViewBag.OfferBadgeStyles = AddBadgeStyles(model.PriceSettings.OfferBadgeStyle);

            static List<SelectListItem> AddBadgeStyles(string selectedValue)
            {
                var items = new List<SelectListItem>();

                foreach (var value in Enum.GetNames(typeof(BadgeStyle)))
                {
                    items.Add(new SelectListItem { Value = value.ToLower(), Text = value, Selected = selectedValue.EqualsNoCase(value) });
                }

                return items;
            }
        }

        private void PrepareSearchConfigModel(SearchSettingsModel model, SearchSettings searchSettings, IModuleDescriptor megaSearchPlusDescriptor)
        {
            var availableSearchFields = new List<SelectListItem>();
            var availableSearchModes = new List<SelectListItem>();

            if (!model.IsMegaSearchInstalled)
            {
                model.SearchFieldsNote = T("Admin.Configuration.Settings.Search.SearchFieldsNote");

                availableSearchFields.AddRange(new[]
                {
                    new SelectListItem { Text = T("Admin.Catalog.Products.Fields.ShortDescription"), Value = "shortdescription" },
                    new SelectListItem { Text = T("Admin.Catalog.Products.Fields.Sku"), Value = "sku" },
                });

                availableSearchModes = searchSettings.SearchMode.ToSelectList().Where(x => x.Value.ToInt() != (int)SearchMode.ExactMatch).ToList();
            }
            else
            {
                availableSearchFields.AddRange(new[]
                {
                    new SelectListItem { Text = T("Admin.Catalog.Products.Fields.ShortDescription"), Value = "shortdescription" },
                    new SelectListItem { Text = T("Admin.Catalog.Products.Fields.FullDescription"), Value = "fulldescription" },
                    new SelectListItem { Text = T("Admin.Catalog.Products.Fields.ProductTags"), Value = "tagname" },
                    new SelectListItem { Text = T("Admin.Catalog.Manufacturers"), Value = "manufacturer" },
                    new SelectListItem { Text = T("Admin.Catalog.Categories"), Value = "category" },
                    new SelectListItem { Text = T("Admin.Catalog.Products.Fields.Sku"), Value = "sku" },
                    new SelectListItem { Text = T("Admin.Catalog.Products.Fields.GTIN"), Value = "gtin" },
                    new SelectListItem { Text = T("Admin.Catalog.Products.Fields.ManufacturerPartNumber"), Value = "mpn" }
                });

                if (megaSearchPlusDescriptor != null)
                {
                    availableSearchFields.AddRange(new[]
                    {
                        new SelectListItem { Text = T("Search.Fields.SpecificationAttributeOptionName"), Value = "attrname" },
                        new SelectListItem { Text = T("Search.Fields.ProductAttributeOptionName"), Value = "variantname" }
                    });
                }

                availableSearchModes = searchSettings.SearchMode.ToSelectList().ToList();
            }

            ViewBag.AvailableSearchFields = availableSearchFields;
            ViewBag.AvailableSearchModes = availableSearchModes;
        }

        private SelectListItem ResToSelectListItem(string resourceKey)
        {
            var value = T(resourceKey).Value.EmptyNull();
            return new SelectListItem { Text = value, Value = value };
        }

        private async Task<bool> CheckToDeleteAddress(int addressId, string settingName)
        {
            if (addressId != 0 &&
                !await _db.Settings.AnyAsync(x => x.Value == addressId.ToStringInvariant() && x.Name == settingName))
            {
                // Address can be removed because it is not in use anymore.
                _db.Addresses.Remove(addressId);
                await _db.SaveChangesAsync();
                return true;
            }

            return false;
        }
    }
}