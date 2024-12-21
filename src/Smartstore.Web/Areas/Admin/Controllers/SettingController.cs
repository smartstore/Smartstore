using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Smartstore.Admin.Models;
using Smartstore.Caching;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Pricing;
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
        [LoadSetting]
        public async Task<IActionResult> Performance(PerformanceSettings performanceSettings, ResiliencySettings resiliencySettings)
        {
            var model = new PerformanceSettingsModel();

            // Map entities to model
            await MapperFactory.MapAsync(performanceSettings, model.PerformanceSettings);
            await MapperFactory.MapAsync(resiliencySettings, model.ResiliencySettings);

            return View(model);
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, SaveSetting, FormValueRequired("save")]
        public async Task<IActionResult> Performance(
            PerformanceSettingsModel model,
            PerformanceSettings performanceSettings, 
            ResiliencySettings resiliencySettings)
        {
            if (!ModelState.IsValid)
            {
                return await Performance(performanceSettings, resiliencySettings);
            }

            ModelState.Clear();

            // Map model to entities
            await MapperFactory.MapAsync(model.PerformanceSettings, performanceSettings);
            await MapperFactory.MapAsync(model.ResiliencySettings, resiliencySettings);

            return NotifyAndRedirect(nameof(Performance));
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