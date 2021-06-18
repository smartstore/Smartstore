using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Smartstore.Admin.Models;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Catalog.Search.Modelling;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Configuration;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.DataExchange;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.DataGrid;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smartstore.Admin.Controllers
{
    public class SettingController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly ILanguageService _languageService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly StoreDependingSettingHelper _storeDependingSettingHelper;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ICookieConsentManager _cookieManager;
        private readonly Lazy<IMediaTracker> _mediaTracker;
        private readonly Lazy<IMenuService> _menuService;
        private readonly Lazy<ICatalogSearchQueryAliasMapper> _catalogSearchQueryAliasMapper;
        private readonly PrivacySettings _privacySettings;

        public SettingController(
            SmartDbContext db,
            ILanguageService languageService,
            ILocalizedEntityService localizedEntityService,
            StoreDependingSettingHelper storeDependingSettingHelper,
            IDateTimeHelper dateTimeHelper,
            ICookieConsentManager cookieManager,
            Lazy<IMediaTracker> mediaTracker,
            Lazy<IMenuService> menuService,
            Lazy<ICatalogSearchQueryAliasMapper> catalogSearchQueryAliasMapper,
            PrivacySettings privacySettings)
        {
            _db = db;
            _languageService = languageService;
            _localizedEntityService = localizedEntityService;
            _storeDependingSettingHelper = storeDependingSettingHelper;
            _dateTimeHelper = dateTimeHelper;
            _cookieManager = cookieManager;
            _mediaTracker = mediaTracker;
            _menuService = menuService;
            _catalogSearchQueryAliasMapper = catalogSearchQueryAliasMapper;
            _privacySettings = privacySettings;
        }

        [LoadSetting(IsRootedModel = true)]
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
            HomePageSettings homePageSettings)
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

            await PrepareConfigurationModelAsync(model);

            return View(model);
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, SaveSetting(IsRootedModel = true), FormValueRequired("save")]
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
            HomePageSettings homePageSeoSettings)
        {
            if (!ModelState.IsValid)
            {
                await PrepareConfigurationModelAsync(model);
                return View(model);
            }

            ModelState.Clear();

            // Necessary before mapping
            var resetUserSeoCharacterTable = seoSettings.SeoNameCharConversion != model.SeoSettings.SeoNameCharConversion;
            var clearSeoFriendlyUrls = localizationSettings.SeoFriendlyUrlsForLanguagesEnabled != model.LocalizationSettings.SeoFriendlyUrlsForLanguagesEnabled;
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

            #region POST mapping

            // Set CountryId explicitly else it can't be resetted.
            companySettings.CountryId = model.CompanyInformationSettings.CountryId ?? 0;

            //// (Un)track PDF logo id
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

            if (resetUserSeoCharacterTable)
            {
                SeoHelper.ResetUserSeoCharacterTable();
            }

            // TODO: (mh) (core) Do this right, if still needed.
            //if (clearSeoFriendlyUrls)
            //{
            //    LocalizedRoute.ClearSeoFriendlyUrlsCachedValue();
            //}

            #endregion

            // Does not contain any store specific settings.
            await Services.SettingFactory.SaveSettingsAsync(securitySettings);

            return NotifyAndRedirect("GeneralCommon");
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public async Task<IActionResult> Catalog(CatalogSettings catalogSettings)
        {
            var model = await MapperFactory.MapAsync<CatalogSettings, CatalogSettingsModel>(catalogSettings);

            ViewBag.AvailableDefaultViewModes = new List<SelectListItem>
            {
                new SelectListItem { Value = "grid", Text = T("Common.Grid"), Selected = model.DefaultViewMode.EqualsNoCase("grid") },
                new SelectListItem { Value = "list", Text = T("Common.List"), Selected = model.DefaultViewMode.EqualsNoCase("list") }
            };
            
            return View(model);
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, SaveSetting]
        public async Task<IActionResult> Catalog(CatalogSettings catalogSettings, CatalogSettingsModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ModelState.Clear();

            // We need to clear the sitemap cache if MaxItemsToDisplayInCatalogMenu has changed.
            if (catalogSettings.MaxItemsToDisplayInCatalogMenu != model.MaxItemsToDisplayInCatalogMenu)
            {
                // Clear cached navigation model.
                await _menuService.Value.ClearCacheAsync("Main");
            }

            await MapperFactory.MapAsync(model, catalogSettings);

            return NotifyAndRedirect("Catalog");
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting(IsRootedModel = true)]
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
        [HttpPost, SaveSetting(IsRootedModel = true)]
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
                return View(model);
            }

            ModelState.Clear();

            await MapperFactory.MapAsync(model.CustomerSettings, customerSettings);
            await MapperFactory.MapAsync(model.AddressSettings, addressSettings);
            await MapperFactory.MapAsync(model.PrivacySettings, privacySettings);

            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedSettingAsync(addressSettings, x => x.Salutations, localized.Salutations, localized.LanguageId, storeScope);
            }

            await _db.SaveChangesAsync();

            return NotifyAndRedirect("CustomerUser");
        }

        public async Task<IActionResult> CookieInfoList(GridCommand command)
        {
            var data = await _cookieManager.GetAllCookieInfosAsync();
            var systemCookies = string.Join(",", data.Select(x => x.Name).ToArray());

            if (_privacySettings.CookieInfos.HasValue())
            {
                data.AddRange(JsonConvert.DeserializeObject<List<CookieInfo>>(_privacySettings.CookieInfos)
                    .OrderBy(x => x.CookieType)
                    .ThenBy(x => x.Name));
            }

            // TODO: (mh) (core) Remove test cookie
            systemCookies += " ,Test";
            data.Add(new CookieInfo {
                CookieType = CookieType.Required,
                Name = "Test",
                Description = "Test"
            });

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
            foreach(var name in selection.SelectedKeys)
            {
                ciList.Remove(x => x.Name.EqualsNoCase(name));
                numDeleted++;
            }
            
            // Now serialize again.
            _privacySettings.CookieInfos = JsonConvert.SerializeObject(ciList, Formatting.None);

            // Save setting.
            await Services.Settings.ApplySettingAsync(_privacySettings, x => x.CookieInfos, 0);

            return Json(new { Success = true, Count = numDeleted });
        }

        public IActionResult CookieInfoCreatePopup()
        {
            var model = new CookieInfoModel();

            AddLocales(model.Locales);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CookieInfoCreatePopup(CookieInfoModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Deserialize
            var ciList = JsonConvert.DeserializeObject<List<CookieInfo>>(_privacySettings.CookieInfos);

            if (ciList == null)
                ciList = new List<CookieInfo>();

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
                // TODO: Use MiniMapper
                CookieType = model.CookieType,
                Name = model.Name,
                Description = model.Description,
                SelectedStoreIds = model.SelectedStoreIds
            };

            ciList.Add(cookieInfo);

            // Serialize
            _privacySettings.CookieInfos = JsonConvert.SerializeObject(ciList, Formatting.None);

            // Now apply & save again.
            await Services.Settings.ApplySettingAsync(_privacySettings, x => x.CookieInfos, 0);

            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(cookieInfo, x => x.Name, localized.Name, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(cookieInfo, x => x.Description, localized.Description, localized.LanguageId);
            }

            await _db.SaveChangesAsync();

            return View(model);
        }

        public IActionResult CookieInfoEditPopup(string name)
        {
            var ciList = JsonConvert.DeserializeObject<List<CookieInfo>>(_privacySettings.CookieInfos);
            var cookieInfo = ciList
                .Select(x => x)
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
        public async Task<IActionResult> CookieInfoEditPopup(CookieInfoModel model)
        {
            var ciList = JsonConvert.DeserializeObject<List<CookieInfo>>(_privacySettings.CookieInfos);
            var cookieInfo = ciList
                .Select(x => x)
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

                await Services.Settings.ApplySettingAsync(_privacySettings, x => x.CookieInfos, 0);

                foreach (var localized in model.Locales)
                {
                    await _localizedEntityService.ApplyLocalizedValueAsync(cookieInfo, x => x.Name, localized.Name, localized.LanguageId);
                    await _localizedEntityService.ApplyLocalizedValueAsync(cookieInfo, x => x.Description, localized.Description, localized.LanguageId);
                }
            }

            return View(model);
        }

        // TODO: (mh) (core) Enough time wasted today with this view. Lets try again on Monday.
        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting(IsRootedModel = true)]
        public async Task<IActionResult> Search(int storeScope, SearchSettings searchSettings)
        {
            // TODO: (mh) (core) Uncomment when _pluginFinder is available.
            //var megaSearchDescriptor = _pluginFinder.GetPluginDescriptorBySystemName("SmartStore.MegaSearch");
            //var megaSearchPlusDescriptor = _pluginFinder.GetPluginDescriptorBySystemName("SmartStore.MegaSearchPlus");

            var megaSearchDescriptor = new object();
            var megaSearchPlusDescriptor = new object();

            var model = new SearchSettingsModel();
            MiniMapper.Map(searchSettings, model);

            model.IsMegaSearchInstalled = megaSearchDescriptor != null;

            // TODO: (mh) (core) Implement in forum module (by tab injection).
            //var fsettings = Services.Settings.LoadSetting<ForumSearchSettings>(storeScope);
            //MiniMapper.Map(fsettings, model.ForumSearchSettings);
            //model.ForumSearchSettings.AvailableDefaultSortOrders = fsettings.DefaultSortOrder.ToSelectList();

            if (!model.IsMegaSearchInstalled)
            {
                model.SearchFieldsNote = T("Admin.Configuration.Settings.Search.SearchFieldsNote");

                model.AvailableSearchFields = new List<SelectListItem>
                {
                    new SelectListItem { Text = T("Admin.Catalog.Products.Fields.ShortDescription"), Value = "shortdescription" },
                    new SelectListItem { Text = T("Admin.Catalog.Products.Fields.Sku"), Value = "sku" },
                };

                model.AvailableSearchModes = searchSettings.SearchMode.ToSelectList().Where(x => x.Value.ToInt() != (int)SearchMode.ExactMatch).ToList();

                // TODO: (mh) (core) Implement in forum module (by tab injection).
                //model.ForumSearchSettings.AvailableSearchModes = fsettings.SearchMode.ToSelectList().Where(x => x.Value.ToInt() != (int)SearchMode.ExactMatch).ToList();
            }
            else
            {
                model.AvailableSearchFields = new List<SelectListItem>
                {
                    new SelectListItem { Text = T("Admin.Catalog.Products.Fields.ShortDescription"), Value = "shortdescription" },
                    new SelectListItem { Text = T("Admin.Catalog.Products.Fields.FullDescription"), Value = "fulldescription" },
                    new SelectListItem { Text = T("Admin.Catalog.Products.Fields.ProductTags"), Value = "tagname" },
                    new SelectListItem { Text = T("Admin.Catalog.Manufacturers"), Value = "manufacturer" },
                    new SelectListItem { Text = T("Admin.Catalog.Categories"), Value = "category" },
                    new SelectListItem { Text = T("Admin.Catalog.Products.Fields.Sku"), Value = "sku" },
                    new SelectListItem { Text = T("Admin.Catalog.Products.Fields.GTIN"), Value = "gtin" },
                    new SelectListItem { Text = T("Admin.Catalog.Products.Fields.ManufacturerPartNumber"), Value = "mpn" }
                };

                if (megaSearchPlusDescriptor != null)
                {
                    model.AvailableSearchFields.Add(new SelectListItem { Text = T("Search.Fields.SpecificationAttributeOptionName"), Value = "attrname" });
                    model.AvailableSearchFields.Add(new SelectListItem { Text = T("Search.Fields.ProductAttributeOptionName"), Value = "variantname" });
                }

                model.AvailableSearchModes = searchSettings.SearchMode.ToSelectList().ToList();

                // TODO: (mh) (core) Implement in forum module (by tab injection).
                //model.ForumSearchSettings.AvailableSearchModes = fsettings.SearchMode.ToSelectList().ToList();
            }

            // TODO: (mh) (core) Implement in forum module (by tab injection).
            //model.ForumSearchSettings.AvailableSearchFields = new List<SelectListItem>
            //{
            //    new SelectListItem { Text = T("Admin.Customers.Customers.Fields.Username"), Value = "username" },
            //    new SelectListItem { Text = T("Forum.PostText"), Value = "text" },
            //};

            // Common facets.
            model.BrandFacet.Disabled = searchSettings.BrandDisabled;
            model.BrandFacet.DisplayOrder = searchSettings.BrandDisplayOrder;
            model.PriceFacet.Disabled = searchSettings.PriceDisabled;
            model.PriceFacet.DisplayOrder = searchSettings.PriceDisplayOrder;
            model.RatingFacet.Disabled = searchSettings.RatingDisabled;
            model.RatingFacet.DisplayOrder = searchSettings.RatingDisplayOrder;
            model.DeliveryTimeFacet.Disabled = searchSettings.DeliveryTimeDisabled;
            model.DeliveryTimeFacet.DisplayOrder = searchSettings.DeliveryTimeDisplayOrder;
            model.AvailabilityFacet.Disabled = searchSettings.AvailabilityDisabled;
            model.AvailabilityFacet.DisplayOrder = searchSettings.AvailabilityDisplayOrder;
            model.AvailabilityFacet.IncludeNotAvailable = searchSettings.IncludeNotAvailable;
            model.NewArrivalsFacet.Disabled = searchSettings.NewArrivalsDisabled;
            model.NewArrivalsFacet.DisplayOrder = searchSettings.NewArrivalsDisplayOrder;

            // TODO: (mh) (core) Implement in forum module (by tab injection).
            //model.ForumSearchSettings.ForumFacet.Disabled = fsettings.ForumDisabled;
            //model.ForumSearchSettings.ForumFacet.DisplayOrder = fsettings.ForumDisplayOrder;
            //model.ForumSearchSettings.CustomerFacet.Disabled = fsettings.CustomerDisabled;
            //model.ForumSearchSettings.CustomerFacet.DisplayOrder = fsettings.CustomerDisplayOrder;
            //model.ForumSearchSettings.DateFacet.Disabled = fsettings.DateDisabled;
            //model.ForumSearchSettings.DateFacet.DisplayOrder = fsettings.DateDisplayOrder;

            // Localized facet settings (CommonFacetSettingsLocalizedModel).
            foreach (var language in _languageService.GetAllLanguages(true))
            {
                model.CategoryFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Category, language.Id))
                });
                model.BrandFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Brand, language.Id))
                });
                model.PriceFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Price, language.Id))
                });
                model.RatingFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Rating, language.Id))
                });
                model.DeliveryTimeFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.DeliveryTime, language.Id))
                });
                model.AvailabilityFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Availability, language.Id))
                });
                model.NewArrivalsFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.NewArrivals, language.Id))
                });

                // TODO: (mh) (core) Implement in forum module (by tab injection).
                //model.ForumSearchSettings.ForumFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                //{
                //    LanguageId = language.Id,
                //    Alias = Services.Settings.GetSettingByKey<string>(FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Forum, language.Id, "Forum"))
                //});
                //model.ForumSearchSettings.CustomerFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                //{
                //    LanguageId = language.Id,
                //    Alias = Services.Settings.GetSettingByKey<string>(FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Customer, language.Id, "Forum"))
                //});
                //model.ForumSearchSettings.DateFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                //{
                //    LanguageId = language.Id,
                //    Alias = Services.Settings.GetSettingByKey<string>(FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Date, language.Id, "Forum"))
                //});
            }

            //// Facet settings (CommonFacetSettingsModel).
            foreach (var prefix in new string[] { "Brand", "Price", "Rating", "DeliveryTime", "Availability", "NewArrivals" })
            {
                await _storeDependingSettingHelper.GetOverrideKeyAsync(prefix + "Facet.Disabled", prefix + "Disabled", searchSettings, storeScope);
                await _storeDependingSettingHelper.GetOverrideKeyAsync(prefix + "Facet.DisplayOrder", prefix + "DisplayOrder", searchSettings, storeScope);
            }

            // Facet settings with a non-prefixed name.
            await _storeDependingSettingHelper.GetOverrideKeyAsync("AvailabilityFacet.IncludeNotAvailable", "IncludeNotAvailable", searchSettings, storeScope);

            // TODO: (mh) (core) Implement in forum module (by tab injection).
            //StoreDependingSettings.GetOverrideKeys(fsettings, model.ForumSearchSettings, storeScope, Services.Settings, false);
            //foreach (var prefix in new string[] { "ForumSearchSettings.Forum", "ForumSearchSettings.Customer", "ForumSearchSettings.Date" })
            //{
            //    StoreDependingSettings.GetOverrideKey(prefix + "Facet.Disabled", prefix + "Disabled", fsettings, storeScope, Services.Settings);
            //    StoreDependingSettings.GetOverrideKey(prefix + "Facet.DisplayOrder", prefix + "DisplayOrder", fsettings, storeScope, Services.Settings);
            //}

            return View(model);
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, SaveSetting(IsRootedModel = true)]
        public async Task<IActionResult> Search(SearchSettingsModel model, int storeScope, SearchSettings settings)
        {
            var form = Request.Form;
            // TODO: (mh) (core) TBD
            //var validator = new SearchSettingValidator(T, x =>
            //{
            //    return storeScope == 0 || _storeDependingSettingHelper.IsOverrideChecked(settings, x, form);
            //});
            //validator.Validate(model);

            // TODO: (mh) (core) Implement in forum module (by tab injection).
            //var fsettings = Services.Settings.LoadSetting<ForumSearchSettings>(storeScope);
            //var fvalidator = new ForumSearchSettingValidator(T, x =>
            //{
            //    return storeScope == 0 || StoreDependingSettings.IsOverrideChecked(fsettings, x, form);
            //});
            //var fvResult = fvalidator.Validate(model.ForumSearchSettings);
            //if (!fvResult.IsValid)
            //{
            //    fvResult.Errors.Each(x => ModelState.AddModelError(string.Concat(nameof(model.ForumSearchSettings), ".", x.PropertyName), x.ErrorMessage));
            //}

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            CategoryTreeChangeReason? categoriesChange = model.AvailabilityFacet.IncludeNotAvailable != settings.IncludeNotAvailable
                ? CategoryTreeChangeReason.ElementCounts
                : null;

            ModelState.Clear();
            MiniMapper.Map(model, settings);
            // TODO: (mh) (core) Implement in forum module (by tab injection).
            //MiniMapper.Map(model.ForumSearchSettings, fsettings);

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

            await Services.SettingFactory.SaveSettingsAsync(settings, storeScope);

            //fsettings.ForumDisabled = model.ForumSearchSettings.ForumFacet.Disabled;
            //fsettings.ForumDisplayOrder = model.ForumSearchSettings.ForumFacet.DisplayOrder;
            //fsettings.CustomerDisabled = model.ForumSearchSettings.CustomerFacet.Disabled;
            //fsettings.CustomerDisplayOrder = model.ForumSearchSettings.CustomerFacet.DisplayOrder;
            //fsettings.DateDisabled = model.ForumSearchSettings.DateFacet.Disabled;
            //fsettings.DateDisplayOrder = model.ForumSearchSettings.DateFacet.DisplayOrder;

            var clearCatalogFacetCache = false;
            //var clearForumFacetCache = false;
            
            await Services.Settings.ApplySettingAsync(settings, x => x.SearchFields, 0);
            //Services.Settings.SaveSetting(fsettings, x => x.SearchFields, 0, false);

            // Facet settings (CommonFacetSettingsModel).
            //if(storeScope != 0)
            //{
            //    foreach (var prefix in new string[] { "Brand", "Price", "Rating", "DeliveryTime", "Availability", "NewArrivals" })
            //    {
            //        await _storeDependingSettingHelper.UpdateSettingAsync(prefix + "Facet.Disabled", prefix + "Disabled", settings, form, storeScope);
            //        await _storeDependingSettingHelper.UpdateSettingAsync(prefix + "Facet.DisplayOrder", prefix + "DisplayOrder", settings, form, storeScope);
            //    }
            //}

            await _db.SaveChangesAsync();

            //foreach (var prefix in new string[] { "ForumSearchSettings.Forum", "ForumSearchSettings.Customer", "ForumSearchSettings.Date" })
            //{
            //    StoreDependingSettings.UpdateSetting(prefix + "Facet.Disabled", prefix + "Disabled", settings, form, storeScope, Services.Settings);
            //    StoreDependingSettings.UpdateSetting(prefix + "Facet.DisplayOrder", prefix + "DisplayOrder", fsettings, form, storeScope, Services.Settings);
            //}

            // Facet settings with a non-prefixed name.
            await _storeDependingSettingHelper.UpdateSettingAsync("AvailabilityFacet.IncludeNotAvailable", "IncludeNotAvailable", settings, form, storeScope);

            // Localized facet settings (CommonFacetSettingsLocalizedModel).
            await UpdateLocalizedFacetSettingAsync(model.CategoryFacet, FacetGroupKind.Category, clearCatalogFacetCache);
            await UpdateLocalizedFacetSettingAsync(model.BrandFacet, FacetGroupKind.Brand, clearCatalogFacetCache);
            await UpdateLocalizedFacetSettingAsync(model.PriceFacet, FacetGroupKind.Price, clearCatalogFacetCache);
            await UpdateLocalizedFacetSettingAsync(model.RatingFacet, FacetGroupKind.Rating, clearCatalogFacetCache);
            await UpdateLocalizedFacetSettingAsync(model.DeliveryTimeFacet, FacetGroupKind.DeliveryTime, clearCatalogFacetCache);
            await UpdateLocalizedFacetSettingAsync(model.AvailabilityFacet, FacetGroupKind.Availability, clearCatalogFacetCache);
            await UpdateLocalizedFacetSettingAsync(model.NewArrivalsFacet, FacetGroupKind.NewArrivals, clearCatalogFacetCache);

            await _db.SaveChangesAsync();

            //UpdateLocalizedFacetSetting(model.ForumSearchSettings.ForumFacet, FacetGroupKind.Forum, ref clearForumFacetCache, "Forum");
            //UpdateLocalizedFacetSetting(model.ForumSearchSettings.CustomerFacet, FacetGroupKind.Customer, ref clearForumFacetCache, "Forum");
            //UpdateLocalizedFacetSetting(model.ForumSearchSettings.DateFacet, FacetGroupKind.Date, ref clearForumFacetCache, "Forum");

            if (clearCatalogFacetCache)
            {
                await _catalogSearchQueryAliasMapper.Value.ClearCommonFacetCacheAsync();
            }

            //if (clearForumFacetCache)
            //{
            //    _forumSearchQueryAliasMapper.Value.ClearCommonFacetCache();
            //}

            if (categoriesChange.HasValue)
            {
                await Services.EventPublisher.PublishAsync(new CategoryTreeChangedEvent(categoriesChange.Value));
            }

            return NotifyAndRedirect("Search");
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
        [ValidateAntiForgeryToken]
        [HttpPost, SaveSetting]
        public IActionResult DataExchange(DataExchangeSettings settings, DataExchangeSettingsModel model)
        {
            if (!ModelState.IsValid)
            {
                return DataExchange(settings);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            return NotifyAndRedirect("DataExchange");
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public IActionResult Payment(PaymentSettings settings)
        {
            var model = new PaymentSettingsModel
            {
                CapturePaymentReason = settings.CapturePaymentReason
            };
            
            return View(model);
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, SaveSetting]
        public IActionResult Payment(PaymentSettings settings, PaymentSettingsModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ModelState.Clear();

            settings.CapturePaymentReason = model.CapturePaymentReason;

            return NotifyAndRedirect("Payment");
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public async Task<IActionResult> RewardPoints(RewardPointsSettings settings, int storeScope)
        {
            var store = storeScope == 0 ? Services.StoreContext.CurrentStore : Services.StoreContext.GetStoreById(storeScope);
            var model = await MapperFactory.MapAsync<RewardPointsSettings, RewardPointsSettingsModel>(settings);

            model.PrimaryStoreCurrencyCode = store.PrimaryStoreCurrency.CurrencyCode;

            return View(model);
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, SaveSetting]
        public async Task<IActionResult> RewardPoints(RewardPointsSettings settings, RewardPointsSettingsModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ModelState.Clear();
            
            await MapperFactory.MapAsync(model, settings);

            return NotifyAndRedirect("RewardPoints");
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

            return View(model);
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, SaveSetting]
        public async Task<IActionResult> ShoppingCart(int storeScope, ShoppingCartSettings settings, ShoppingCartSettingsModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ModelState.Clear();

            await MapperFactory.MapAsync(model, settings);

            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedSettingAsync(settings, x => x.ThirdPartyEmailHandOverLabel, localized.ThirdPartyEmailHandOverLabel, localized.LanguageId, storeScope);
            }

            return NotifyAndRedirect("ShoppingCart");
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public async Task<IActionResult> Shipping(int storeScope, ShippingSettings settings)
        {
            var store = storeScope == 0 ? Services.StoreContext.CurrentStore : Services.StoreContext.GetStoreById(storeScope);
            var model = await MapperFactory.MapAsync<ShippingSettings, ShippingSettingsModel>(settings);

            model.PrimaryStoreCurrencyCode = store.PrimaryStoreCurrency.CurrencyCode;

            var todayShipmentHours = new List<SelectListItem>();

            for (var i = 1; i <= 24; ++i)
            {
                var hourStr = i.ToString();
                todayShipmentHours.Add(new SelectListItem
                {
                    Text = hourStr,
                    Value = hourStr,
                    Selected = settings.TodayShipmentHour == i
                });
            }

            ViewBag.TodayShipmentHours = todayShipmentHours;

            await _storeDependingSettingHelper.GetOverrideKeysAsync(settings, model, storeScope);

            // Shipping origin
            if (storeScope > 0 && await Services.Settings.SettingExistsAsync(settings, x => x.ShippingOriginAddressId, storeScope))
            {
                _storeDependingSettingHelper.AddOverrideKey(settings, "ShippingOriginAddress");
            }

            var originAddress = settings.ShippingOriginAddressId > 0
                ? await _db.Addresses.FindByIdAsync(settings.ShippingOriginAddressId, false)
                : null;

            if (originAddress != null)
            {
                MiniMapper.Map(originAddress, model.ShippingOriginAddress);
            }

            var countries = await _db.Countries
                .AsNoTracking()
                .Include(x => x.StateProvinces.OrderBy(x => x.DisplayOrder))
                .ApplyStandardFilter(true)
                .ToListAsync();

            foreach (var c in countries)
            {
                model.ShippingOriginAddress.AvailableCountries.Add(
                    new SelectListItem { Text = c.Name, Value = c.Id.ToString(), Selected = (originAddress != null && c.Id == originAddress.CountryId) }
                );
            }

            var states = originAddress != null && originAddress.Country != null
                ? countries.FirstOrDefault(x => x.Id == originAddress.Country.Id).StateProvinces.ToList()
                : new List<StateProvince>();

            if (states.Count > 0)
            {
                foreach (var s in states)
                {
                    model.ShippingOriginAddress.AvailableStates.Add(
                        new SelectListItem { Text = s.Name, Value = s.Id.ToString(), Selected = (s.Id == originAddress.StateProvinceId) }
                    );
                }
            }
            else
            {
                model.ShippingOriginAddress.AvailableStates.Add(new SelectListItem { Text = T("Admin.Address.OtherNonUS"), Value = "0" });
            }

            model.ShippingOriginAddress.CountryEnabled = true;
            model.ShippingOriginAddress.StateProvinceEnabled = true;
            model.ShippingOriginAddress.ZipPostalCodeEnabled = true;
            model.ShippingOriginAddress.ZipPostalCodeRequired = true;

            return View(model);
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, SaveSetting]
        public async Task<IActionResult> Shipping(int storeScope, ShippingSettings settings, ShippingSettingsModel model)
        {
            var form = Request.Form;

            // Note, model state is invalid here due to ShippingOriginAddress validation.
            await MapperFactory.MapAsync(model, settings);

            await _storeDependingSettingHelper.UpdateSettingsAsync(settings, form, storeScope, propertyName =>
            {
                // Skip to prevent the address from being recreated every time you save.
                if (propertyName.EqualsNoCase(nameof(settings.ShippingOriginAddressId)))
                    return null;

                return propertyName;
            });

            // Special case ShippingOriginAddressId\ShippingOriginAddress.
            if (storeScope == 0 || _storeDependingSettingHelper.IsOverrideChecked(settings, "ShippingOriginAddress", form))
            {
                var addressId = await Services.Settings.SettingExistsAsync(settings, x => x.ShippingOriginAddressId, storeScope) ? settings.ShippingOriginAddressId : 0;
                var originAddress = await _db.Addresses.FindByIdAsync(addressId) ?? new Address { CreatedOnUtc = DateTime.UtcNow };

                // Update ID manually (in case we're in multi-store configuration mode it'll be set to the shared one).
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
                _db.Addresses.Remove(settings.ShippingOriginAddressId);
                await Services.Settings.RemoveSettingAsync(settings, x => x.ShippingOriginAddressId, storeScope);
            }

            await _db.SaveChangesAsync();

            return NotifyAndRedirect("Shipping");
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public async Task<IActionResult> Order(int storeScope, OrderSettings settings)
        {
            var allStores = Services.StoreContext.GetAllStores();
            var store = storeScope == 0 ? Services.StoreContext.CurrentStore : allStores.FirstOrDefault(x => x.Id == storeScope);
            var model = await MapperFactory.MapAsync<OrderSettings, OrderSettingsModel>(settings);

            model.PrimaryStoreCurrencyCode = store.PrimaryStoreCurrency.CurrencyCode;
            model.StoreCount = allStores.Count;

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
        public async Task<IActionResult> Order(int storeScope, OrderSettings settings, OrderSettingsModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ModelState.Clear();

            await MapperFactory.MapAsync(model, settings);

            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedSettingAsync(settings, x => x.ReturnRequestActions, localized.ReturnRequestActions, localized.LanguageId, storeScope);
                await _localizedEntityService.ApplyLocalizedSettingAsync(settings, x => x.ReturnRequestReasons, localized.ReturnRequestReasons, localized.LanguageId, storeScope);
            }

            if (model.GiftCards_Activated_OrderStatusId.HasValue)
            {
                await Services.Settings.ApplySettingAsync(settings, x => x.GiftCards_Activated_OrderStatusId, 0);
            }
            else
            {
                await Services.Settings.RemoveSettingAsync(settings, x => x.GiftCards_Activated_OrderStatusId);
            }

            if (model.GiftCards_Deactivated_OrderStatusId.HasValue)
            {
                await Services.Settings.ApplySettingAsync(settings, x => x.GiftCards_Deactivated_OrderStatusId, 0);
            }
            else
            {
                await Services.Settings.RemoveSettingAsync(settings, x => x.GiftCards_Deactivated_OrderStatusId);
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

            return NotifyAndRedirect("Order");
        }

        private async Task UpdateLocalizedFacetSettingAsync(CommonFacetSettingsModel model, FacetGroupKind kind, bool clearCache, string scope = null)
        {
            foreach (var localized in model.Locales)
            {
                var key = FacetUtility.GetFacetAliasSettingKey(kind, localized.LanguageId, scope);
                var existingAlias = Services.Settings.GetSettingByKey<string>(key);

                if (existingAlias.EqualsNoCase(localized.Alias))
                    continue;

                if (localized.Alias.HasValue())
                {
                    await Services.Settings.ApplySettingAsync(key, localized.Alias, 0);
                }
                else
                {
                    await Services.Settings.RemoveSettingAsync(key);
                }

                clearCache = true;
            }
        }

        private ActionResult NotifyAndRedirect(string actionMethod)
        {
            NotifySuccess(T("Admin.Configuration.Updated"));
            return RedirectToAction(actionMethod);
        }

        private async Task PrepareConfigurationModelAsync(GeneralCommonSettingsModel model)
        {
            foreach (var timeZone in _dateTimeHelper.GetSystemTimeZones())
            {
                model.DateTimeSettings.AvailableTimeZones.Add(new SelectListItem
                {
                    Text = timeZone.DisplayName,
                    Value = timeZone.Id,
                    Selected = timeZone.Id.Equals(_dateTimeHelper.DefaultStoreTimeZone.Id, StringComparison.InvariantCultureIgnoreCase)
                });
            }

            #region CompanyInfo custom mapping

            ViewBag.AvailableCountries = new List<SelectListItem>
            {
                new SelectListItem { Text = T("Common.Unspecified"), Value = "0" }
            };
            
            ViewBag.Salutations = new List<SelectListItem>();

            var countries = await _db.Countries
                .AsNoTracking()
                .ApplyStandardFilter()
                .ToListAsync();

            foreach (var c in countries)
            {
                ViewBag.AvailableCountries.Add(new SelectListItem
                {
                    Text = c.GetLocalized(x => x.Name),
                    Value = c.Id.ToString(),
                    Selected = c.Id == model.CompanyInformationSettings.CountryId
                });
            }

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
                new SelectListItem { Text = "index", Value = "index" },
                new SelectListItem { Text = "noindex", Value = "noindex" },
                new SelectListItem { Text = "index, follow", Value = "index, follow" },
                new SelectListItem { Text = "index, nofollow", Value = "index, nofollow" },
                new SelectListItem { Text = "noindex, follow", Value = "noindex, follow" },
                new SelectListItem { Text = "noindex, nofollow", Value = "noindex, nofollow" }
            };

            #endregion
        }

        private SelectListItem ResToSelectListItem(string resourceKey)
        {
            var value = T(resourceKey).Value.EmptyNull();
            return new SelectListItem { Text = value, Value = value };
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
    }
}