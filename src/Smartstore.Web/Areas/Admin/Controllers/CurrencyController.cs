using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Directory;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.DataGrid;

namespace Smartstore.Web.Areas.Admin.Controllers
{
    public class CurrencyController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly ILanguageService _languageService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly ModuleManager _moduleManager;
        private readonly ICommonServices _services;
        private readonly IPaymentService _paymentService;

        public CurrencyController(
            SmartDbContext db,
            ICurrencyService currencyService,
            CurrencySettings currencySettings,
            IDateTimeHelper dateTimeHelper,
            ILocalizedEntityService localizedEntityService,
            ILanguageService languageService,
            IStoreMappingService storeMappingService,
            ModuleManager moduleManager,
            ICommonServices services,
            IPaymentService paymentService)
        {
            _db = db;
            _currencyService = currencyService;
            _currencySettings = currencySettings;
            _dateTimeHelper = dateTimeHelper;
            _localizedEntityService = localizedEntityService;
            _languageService = languageService;
            _storeMappingService = storeMappingService;
            _moduleManager = moduleManager;
            _services = services;
            _paymentService = paymentService;
        }

        #region Utilities

        [NonAction]
        public async Task UpdateLocalesAsync(Currency currency, CurrencyModel model)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(currency, x => x.Name, localized.Name, localized.LanguageId);
            }
        }

        private async Task PrepareCurrencyModelAsync(CurrencyModel model, Currency currency, bool excludeProperties)
        {
            Guard.NotNull(model, nameof(model));

            var paymentMethods = await _paymentService.GetAllPaymentMethodsAsync();
            var paymentProviders = await _paymentService.LoadAllPaymentMethodsAsync();

            foreach (var provider in paymentProviders)
            {
                if (paymentMethods.TryGetValue(provider.Metadata.SystemName, out var paymentMethod) && paymentMethod.RoundOrderTotalEnabled)
                {
                    var friendlyName = _moduleManager.GetLocalizedFriendlyName(provider.Metadata);
                    model.RoundOrderTotalPaymentMethods[provider.Metadata.SystemName] = friendlyName ?? provider.Metadata.SystemName;
                }
            }

            if (currency != null)
            {
                var allStores = _services.StoreContext.GetAllStores();

                model.PrimaryStoreCurrencyStores = allStores
                    .Where(x => x.PrimaryStoreCurrencyId == currency.Id)
                    .Select(x => new SelectListItem
                    {
                        Text = x.Name,
                        Value = Url.Action("Edit", "Store", new { id = x.Id })
                    })
                    .ToList();

                model.PrimaryExchangeRateCurrencyStores = allStores
                    .Where(x => x.PrimaryExchangeRateCurrencyId == currency.Id)
                    .Select(x => new SelectListItem
                    {
                        Text = x.Name,
                        Value = Url.Action("Edit", "Store", new { id = x.Id })
                    })
                    .ToList();
            }

            if (!excludeProperties)
            {
                model.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(currency);
            }
        }

        private async Task<CurrencyModel> CreateCurrencyListModelAsync(Currency currency)
        {
            var store = _services.StoreContext.CurrentStore;
            var model = await MapperFactory.MapAsync<Currency, CurrencyModel>(currency);

            model.IsPrimaryStoreCurrency = store.PrimaryStoreCurrencyId == model.Id;
            model.IsPrimaryExchangeRateCurrency = store.PrimaryExchangeRateCurrencyId == model.Id;

            return model;
        }

        private bool IsAttachedToStore(Currency currency, IList<Store> stores, bool force)
        {
            var attachedStore = stores.FirstOrDefault(x => x.PrimaryStoreCurrencyId == currency.Id || x.PrimaryExchangeRateCurrencyId == currency.Id);

            if (attachedStore != null)
            {
                if (force || (!force && !currency.Published))
                {
                    NotifyError(T("Admin.Configuration.Currencies.DeleteOrPublishStoreConflict", attachedStore.Name));
                    return true;
                }

                // Must store limitations include the store where the currency is attached as primary or exchange rate currency?
                //if (currency.LimitedToStores)
                //{
                //	if (selectedStoreIds == null)
                //		selectedStoreIds = _storeMappingService.GetStoreMappingsFor("Currency", currency.Id).Select(x => x.StoreId).ToArray();

                //	if (!selectedStoreIds.Contains(attachedStore.Id))
                //	{
                //		NotifyError(T("Admin.Configuration.Currencies.StoreLimitationConflict", attachedStore.Name));
                //		return true;
                //	}
                //}
            }
            return false;
        }

        #endregion

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        [Permission(Permissions.Configuration.Currency.Read)]
        public IActionResult List(bool liveRates = false)
        {
            var model = new CurrencyListModel 
            { 
                DisplayLiveRates = liveRates,
                AutoUpdateEnabled = _currencySettings.AutoUpdateEnabled
            };
            
            ViewBag.ExchangeRateProviders = new List<SelectListItem>();

            foreach (var erp in _currencyService.LoadAllExchangeRateProviders())
            {
                ViewBag.ExchangeRateProviders.Add(new SelectListItem
                {
                    Text = _moduleManager.GetLocalizedFriendlyName(erp.Metadata),
                    Value = erp.Metadata.SystemName,
                    Selected = erp.Metadata.SystemName.Equals(_currencySettings.ActiveExchangeRateProviderSystemName, StringComparison.InvariantCultureIgnoreCase)
                });
            }

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Currency.Update)]
        public async Task<IActionResult> List(CurrencyListModel model)
        {
            _currencySettings.ActiveExchangeRateProviderSystemName = model.ExchangeRateProvider;
            _currencySettings.AutoUpdateEnabled = model.AutoUpdateEnabled;

            await _services.Settings.ApplySettingAsync(_currencySettings, x => x.ActiveExchangeRateProviderSystemName);
            await _services.Settings.ApplySettingAsync(_currencySettings, x => x.AutoUpdateEnabled);
            await _db.SaveChangesAsync();

            return RedirectToAction("List", "Currency");
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Currency.Read)]
        public async Task<IActionResult> CurrencyList(GridCommand command)
        {
            var currencies = await _db.Currencies
                .AsNoTracking()
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var currencyModels = await currencies
                .SelectAsync(async x =>
                {
                    var model = await CreateCurrencyListModelAsync(x);
                    model.EditUrl = Url.Action(nameof(Edit), "Currency", new { id = x.Id });
                    return model;
                })
                .AsyncToList();

            var gridModel = new GridModel<CurrencyModel>
            {
                Rows = currencyModels,
                Total = await currencies.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Update)]
        public async Task<IActionResult> Update(CurrencyModel model)
        {
            var success = false;
            var currency = await _db.Currencies.FindByIdAsync(model.Id);

            if (currency != null)
            {
                await MapperFactory.MapAsync(model, currency);
                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Currency.Read)]
        public async Task<IActionResult> LiveRateList()
        {
            var language = _services.WorkContext.WorkingLanguage;
            var rates = new List<ExchangeRate>();
            var ratesCount = 0;
            var allCurrenciesByIsoCode = (await _db.Currencies
                .AsNoTracking()
                .ToListAsync())
                .ToDictionarySafe(x => x.CurrencyCode.EmptyNull().ToUpper(), x => x);

            try
            {
                var primaryExchangeCurrency = _services.StoreContext.CurrentStore.PrimaryExchangeRateCurrency;
                if (primaryExchangeCurrency == null)
                {
                    throw new SmartException(T("Admin.System.Warnings.ExchangeCurrency.NotSet"));
                }

                rates = (await _currencyService.GetCurrencyLiveRatesAsync(primaryExchangeCurrency.CurrencyCode)).ToList();

                // Get localized name of currencies.
                var currencyNames = allCurrenciesByIsoCode.ToDictionarySafe(
                    x => x.Key,
                    x => x.Value.GetLocalized(y => y.Name, language, true, false).Value
                );

                // Fallback to english name where no localized currency name exists.
                foreach (var info in CultureInfo.GetCultures(CultureTypes.AllCultures).Where(x => !x.IsNeutralCulture))
                {
                    try
                    {
                        var region = new RegionInfo(info.LCID);

                        if (!currencyNames.ContainsKey(region.ISOCurrencySymbol))
                        {
                            currencyNames.Add(region.ISOCurrencySymbol, region.CurrencyEnglishName);
                        }
                    }
                    catch 
                    { 
                    }
                }

                // Provide rate with currency name and whether it is available in store.
                rates.Each(x =>
                {
                    x.IsStoreCurrency = allCurrenciesByIsoCode.ContainsKey(x.CurrencyCode);

                    if (x.Name.IsEmpty() && currencyNames.ContainsKey(x.CurrencyCode))
                    {
                        x.Name = currencyNames[x.CurrencyCode];
                    }
                });

                rates = rates.OrderBy(x => !x.IsStoreCurrency).ThenBy(x => x.Name).ToList();
            }
            catch (Exception ex)
            {
                NotifyError(ex, false);
            }

            var gridModel = new GridModel<ExchangeRate>
            {
                Rows = rates,
                Total = ratesCount
            };

            return Json(gridModel);
        }

        // AJAX
        [Permission(Permissions.Configuration.Currency.Update)]
        public async Task<IActionResult> ApplyRate(string currencyCode, decimal rate)
        {
            var currency = await _db.Currencies.FirstOrDefaultAsync(x => x.CurrencyCode == currencyCode);
            var success = false;
            var returnMessage = string.Empty;

            if (currency != null)
            {
                currency.Rate = rate;

                await _db.SaveChangesAsync();

                success = true;
                returnMessage = T("Admin.Common.TaskSuccessfullyProcessed").Value;
            }
            else
            {
                returnMessage = T("Admin.Configuration.Currencies.ApplyRate.Error").Value;
            }

            return new JsonResult(new { success, returnMessage });
        }

        [Permission(Permissions.Configuration.Currency.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new CurrencyModel();
            AddLocales(model.Locales);
            await PrepareCurrencyModelAsync(model, null, false);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Configuration.Currency.Create)]
        public async Task<IActionResult> Create(CurrencyModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var currency = await MapperFactory.MapAsync<CurrencyModel, Currency>(model);
                _db.Currencies.Add(currency);
                await _db.SaveChangesAsync();

                await UpdateLocalesAsync(currency, model);
                await SaveStoreMappingsAsync(currency, model.SelectedStoreIds);
                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.Configuration.Currencies.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = currency.Id }) : RedirectToAction("List");
            }

            await PrepareCurrencyModelAsync(model, null, true);

            return View(model);
        }

        [Permission(Permissions.Configuration.Currency.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var currency = await _db.Currencies.FindByIdAsync(id, false);
            if (currency == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<Currency, CurrencyModel>(currency);
            model.CreatedOn = _dateTimeHelper.ConvertToUserTime(currency.CreatedOnUtc, DateTimeKind.Utc);

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Name = currency.GetLocalized(x => x.Name, languageId, false, false);
            });

            foreach (var ending in model.DomainEndings.SplitSafe(","))
            {
                var item = model.AvailableDomainEndings.FirstOrDefault(x => x.Value.EqualsNoCase(ending));
                if (item == null)
                {
                    model.AvailableDomainEndings.Add(new SelectListItem { Text = ending, Value = ending, Selected = true });
                }
                else
                {
                    item.Selected = true;
                }
            }

            model.DomainEndingsArray = model.DomainEndings.SplitSafe(",").ToArray();

            await PrepareCurrencyModelAsync(model, currency, false);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.Configuration.Currency.Update)]
        public async Task<IActionResult> Edit(CurrencyModel model, bool continueEditing)
        {
            var currency = await _db.Currencies.FindByIdAsync(model.Id);
            if (currency == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await MapperFactory.MapAsync(model, currency);
                currency.DomainEndings = string.Join(",", model.DomainEndingsArray ?? new string[0]);

                if (!IsAttachedToStore(currency, _services.StoreContext.GetAllStores().ToList(), false))
                {
                    await UpdateLocalesAsync(currency, model);
                    await SaveStoreMappingsAsync(currency, model.SelectedStoreIds);
                    await _db.SaveChangesAsync();

                    NotifySuccess(T("Admin.Configuration.Currencies.Updated"));
                    return continueEditing ? RedirectToAction("Edit", new { id = currency.Id }) : RedirectToAction("List");
                }
            }

            model.CreatedOn = _dateTimeHelper.ConvertToUserTime(currency.CreatedOnUtc, DateTimeKind.Utc);

            await PrepareCurrencyModelAsync(model, currency, true);

            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [Permission(Permissions.Configuration.Currency.Delete)]
        public async Task<IActionResult> Delete(CountryModel model)
        {
            // TODO: (mh) (core) Why CounntryModel? And why not just pass id?
            var currency = await _db.Currencies.FindByIdAsync(model.Id);
            if (currency == null)
            {
                return NotFound();
            }

            try
            {
                if (!IsAttachedToStore(currency, _services.StoreContext.GetAllStores().ToList(), true))
                {
                    _db.Currencies.Remove(currency);
                    await _db.SaveChangesAsync();

                    NotifySuccess(T("Admin.Configuration.Currencies.Deleted"));
                    return RedirectToAction("List");
                }
                else
                {
                    NotifyError(T("Admin.Configuration.Currencies.CannotDeleteAssociated"));
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction("Edit", new { id = currency.Id });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Currency.Delete)]
        public async Task<IActionResult> DeleteSelection(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var currencies = await _db.Currencies.GetManyAsync(ids, true);
                var triedToDeleteAssociated = false;

                foreach (var currency in currencies)
                {
                    if (!IsAttachedToStore(currency, _services.StoreContext.GetAllStores().ToList(), true))
                    {
                        _db.Currencies.Remove(currency);
                    }
                    else
                    {
                        triedToDeleteAssociated = true;
                        NotifyError(T("Admin.Configuration.Currencies.CannotDeleteAssociated"));
                    }
                }

                numDeleted = await _db.SaveChangesAsync();

                success = !triedToDeleteAssociated || numDeleted != 0;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        // AJAX
        public IActionResult GetCustomFormattingExample(int currencyId, string customFormat)
        {
            var example = string.Empty;
            var error = string.Empty;

            if (customFormat.HasValue())
            {
                try
                {
                    var currency = _db.Currencies.FindById(currencyId, false);
                    var clone = currency.Clone();
                    clone.Id = 0;
                    clone.CustomFormatting = customFormat;

                    var money = new Money(1234.45M, clone);
                    example = money.ToString();
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
            }

            return new JsonResult(new { example, error });
        }
    }
}
