using FluentValidation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models;
using Smartstore.Caching;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Search.Modelling;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public partial class SettingController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ICurrencyService _currencyService;
        private readonly ILanguageService _languageService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly MultiStoreSettingHelper _multiStoreSettingHelper;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IProviderManager _providerManager;
        private readonly Lazy<IMediaTracker> _mediaTracker;
        private readonly Lazy<IMenuService> _menuService;
        private readonly Lazy<ICatalogSearchQueryAliasMapper> _catalogSearchQueryAliasMapper;
        private readonly Lazy<IMediaMover> _mediaMover;
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
            IProviderManager providerManager,
            Lazy<IMediaTracker> mediaTracker,
            Lazy<IMenuService> menuService,
            Lazy<ICatalogSearchQueryAliasMapper> catalogSearchQueryAliasMapper,
            Lazy<IMediaMover> mediaMover,
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
            _providerManager = providerManager;
            _mediaTracker = mediaTracker;
            _menuService = menuService;
            _catalogSearchQueryAliasMapper = catalogSearchQueryAliasMapper;
            _mediaMover = mediaMover;
            _privacySettings = privacySettings;
            _moduleManager = moduleManager;
            _cache = cache;
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public async Task<IActionResult> Catalog(int storeScope, CatalogSettings catalogSettings, PriceSettings priceSettings)
        {
            var model = await MapperFactory.MapAsync<CatalogSettings, CatalogSettingsModel>(catalogSettings);
            await MapperFactory.MapAsync(catalogSettings, model.GroupedProductSettings);
            await MapperFactory.MapAsync(priceSettings, model.PriceSettings);

            await PrepareCatalogConfigurationModelAsync(model, catalogSettings);

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.OfferBadgeLabel = priceSettings.GetLocalizedSetting(x => x.OfferBadgeLabel, languageId, storeScope, false, false);
                locale.LimitedOfferBadgeLabel = priceSettings.GetLocalizedSetting(x => x.LimitedOfferBadgeLabel, languageId, storeScope, false, false);
            });

            AddLocales(model.GroupedProductSettings.Locales, (locale, languageId) =>
            {
                locale.AssociatedProductsTitle = catalogSettings.GetLocalizedSetting(x => x.AssociatedProductsTitle, languageId, storeScope, false, false);
            });

            return View(model);
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, SaveSetting]
        public async Task<IActionResult> Catalog(
            int storeScope,
            CatalogSettingsModel model,
            CatalogSettings catalogSettings, 
            PriceSettings priceSettings,
            GroupedProductSettingsModel groupedProductSettings)
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
            await MapperFactory.MapAsync(groupedProductSettings, catalogSettings);
            await MapperFactory.MapAsync(model.PriceSettings, priceSettings);

            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedSettingAsync(priceSettings, x => x.OfferBadgeLabel, localized.OfferBadgeLabel, localized.LanguageId, storeScope);
                await _localizedEntityService.ApplyLocalizedSettingAsync(priceSettings, x => x.LimitedOfferBadgeLabel, localized.LimitedOfferBadgeLabel, localized.LanguageId, storeScope);
            }

            foreach (var localized in groupedProductSettings.Locales)
            {
                await _localizedEntityService.ApplyLocalizedSettingAsync(catalogSettings, x => x.AssociatedProductsTitle, localized.AssociatedProductsTitle, localized.LanguageId, storeScope);
            }

            return NotifyAndRedirect(nameof(Catalog));
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

        private async Task PrepareCatalogConfigurationModelAsync(CatalogSettingsModel model, CatalogSettings catalogSettings)
        {
            ViewBag.AvailableDefaultViewModes = new List<SelectListItem>
            {
                new() { Value = "grid", Text = T("Common.Grid"), Selected = model.DefaultViewMode.EqualsNoCase("grid") },
                new() { Value = "list", Text = T("Common.List"), Selected = model.DefaultViewMode.EqualsNoCase("list") }
            };

            var priceLabels = await _db.PriceLabels
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            ViewBag.AvailableDefaultComparePriceLabels = new List<SelectListItem>();
            ViewBag.AvailableDefaultRegularPriceLabels = new List<SelectListItem>();

            foreach (var label in priceLabels)
            {
                ViewBag.AvailableDefaultComparePriceLabels.Add(new SelectListItem
                { 
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
            ViewBag.AssociatedProductsHeaderFields = ProductController.CreateAssociatedProductsHeaderFieldsList(catalogSettings.CollapsibleAssociatedProductsHeaders, T);
            ViewBag.AvailableProductSortings = CreateProductSortingsList(model.DefaultSortOrder);

            static List<SelectListItem> AddBadgeStyles(string selectedValue)
            {
                var items = new List<SelectListItem>();

                foreach (var value in Enum.GetNames(typeof(BadgeStyle)))
                {
                    items.Add(new() { Value = value.ToLower(), Text = value, Selected = selectedValue.EqualsNoCase(value) });
                }

                return items;
            }
        }
    }
}