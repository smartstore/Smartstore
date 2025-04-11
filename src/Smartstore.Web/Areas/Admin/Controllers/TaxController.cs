using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Tax;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Security;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public class TaxController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ITaxService _taxService;
        private readonly ModuleManager _moduleManager;
        private readonly MultiStoreSettingHelper _multiStoreSettingHelper;
        private readonly TaxSettings _taxSettings;

        public TaxController(
            SmartDbContext db,
            ITaxService taxService,
            ModuleManager moduleManager,
            MultiStoreSettingHelper multiStoreSettingHelper,
            TaxSettings taxSettings)
        {
            _db = db;
            _taxService = taxService;
            _moduleManager = moduleManager;
            _multiStoreSettingHelper = multiStoreSettingHelper;
            _taxSettings = taxSettings;
        }

        #region Tax Providers

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Providers));
        }

        [Permission(Permissions.Configuration.Tax.Read)]
        public async Task<IActionResult> Providers()
        {
            var taxProviderModels = await _taxService.LoadAllTaxProviders()
                .SelectAwait(async x =>
                {
                    var model = _moduleManager.ToProviderModel<ITaxProvider, TaxProviderModel>(x);
                    if (x.Metadata.SystemName.Equals(_taxSettings.ActiveTaxProviderSystemName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        model.IsPrimaryTaxProvider = true;
                    }
                    else
                    {
                        await _moduleManager.ActivateDependentWidgetsAsync(x.Metadata, false);
                    }

                    return model;
                })
                .AsyncToList();

            return View(taxProviderModels);
        }

        [Permission(Permissions.Configuration.Tax.Activate)]
        public async Task<IActionResult> ActivateProvider(string systemName)
        {
            if (systemName.HasValue())
            {
                var taxProvider = _taxService.LoadTaxProviderBySystemName(systemName);
                if (taxProvider != null)
                {
                    _taxSettings.ActiveTaxProviderSystemName = systemName;
                    await Services.SettingFactory.SaveSettingsAsync(_taxSettings);
                    await _moduleManager.ActivateDependentWidgetsAsync(taxProvider.Metadata, true);
                }
            }

            return RedirectToAction(nameof(Providers));
        }

        #endregion

        #region Tax categories

        [Permission(Permissions.Configuration.Tax.Read)]
        public IActionResult List()
        {
            return View();
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Tax.Read)]
        public async Task<IActionResult> TaxCategoryList(GridCommand command)
        {
            var categories = await _db.TaxCategories
                .AsNoTracking()
                // INFO: we use OrderBy to circumvent EF caching issue.
                .OrderBy(x => x.DisplayOrder)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var categoriesModels = await categories
                .SelectAwait(x => MapperFactory.MapAsync<TaxCategory, TaxCategoryModel>(x))
                .AsyncToList();

            var gridModel = new GridModel<TaxCategoryModel>
            {
                Rows = categoriesModels,
                Total = await categories.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Tax.Create)]
        public async Task<IActionResult> TaxCategoryInsert(TaxCategoryModel model)
        {
            var success = false;

            if (!await _db.TaxCategories.AnyAsync(x => x.Name == model.Name))
            {
                _db.TaxCategories.Add(new TaxCategory
                {
                    Name = model.Name,
                    DisplayOrder = model.DisplayOrder
                });

                await _db.SaveChangesAsync();
                success = true;
            }
            else
            {
                NotifyError(T("Admin.Tax.Categories.NoDuplicatesAllowed"));
            }

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Tax.Update)]
        public async Task<IActionResult> TaxCategoryUpdate(TaxCategoryModel model)
        {
            var success = false;
            var taxCategory = await _db.TaxCategories.FindByIdAsync(model.Id, true);

            if (taxCategory != null)
            {
                await MapperFactory.MapAsync(model, taxCategory);
                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Tax.Delete)]
        public async Task<IActionResult> TaxCategoryDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var taxCategories = await _db.TaxCategories.GetManyAsync(ids, true);

                _db.TaxCategories.RemoveRange(taxCategories);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        #endregion

        #region Settings

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public async Task<IActionResult> FinanceSettings
            (int storeScope, 
            TaxSettings taxSettings, 
            CurrencySettings currencySettings)
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

            model.TaxSettings.DefaultTaxAddress.AvailableStates = stateProvinces.ToSelectListItems(defaultAddress?.StateProvinceId ?? 0) ??
            [
                new() { Text = T("Address.OtherNonUS"), Value = "0" }
            ];

            model.TaxSettings.DefaultTaxAddress.FirstNameEnabled = false;
            model.TaxSettings.DefaultTaxAddress.LastNameEnabled = false;
            model.TaxSettings.DefaultTaxAddress.EmailEnabled = false;
            model.TaxSettings.DefaultTaxAddress.CountryEnabled = true;
            model.TaxSettings.DefaultTaxAddress.StateProvinceEnabled = true;
            model.TaxSettings.DefaultTaxAddress.ZipPostalCodeEnabled = true;
            model.TaxSettings.DefaultTaxAddress.ZipPostalCodeRequired = true;

            ViewBag.ExchangeRateProviders = Services.CurrencyService.LoadAllExchangeRateProviders()
                .Select(x => new SelectListItem
                {
                    Text = _moduleManager.GetLocalizedFriendlyName(x.Metadata),
                    Value = x.Metadata.SystemName,
                    Selected = x.Metadata.SystemName.EqualsNoCase(currencySettings.ActiveExchangeRateProviderSystemName)
                })
                .ToList();

            return View(model);
        }

        // INFO: do not use SaveSetting attribute here because it would delete a previously added default tax address if storeScope > 0.
        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, LoadSetting]
        public async Task<IActionResult> FinanceSettings(
            int storeScope, 
            FinanceSettingsModel model, 
            TaxSettings taxSettings, 
            CurrencySettings currencySettings)
        {
            var form = Request.Form;

            if (!ModelState.IsValid)
            {
                return await FinanceSettings(storeScope, taxSettings, currencySettings);
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
            var addressIdSettingName = $"{nameof(TaxSettings)}.{nameof(TaxSettings.DefaultTaxAddressId)}";

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

            if (deleteAddressId != 0 && !await _db.Settings.AnyAsync(x => x.Value == deleteAddressId.ToStringInvariant() && x.Name == addressIdSettingName))
            {
                // Address can be removed because it is not in use anymore.
                _db.Addresses.Remove(deleteAddressId);
                await _db.SaveChangesAsync();
            }

            NotifySuccess(T("Admin.Configuration.Updated"));
            return RedirectToAction(nameof(FinanceSettings));
        }

        #endregion
    }
}
