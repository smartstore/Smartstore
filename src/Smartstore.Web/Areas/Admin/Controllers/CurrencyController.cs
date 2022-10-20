using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Common;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class CurrencyController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly ModuleManager _moduleManager;
        private readonly IPaymentService _paymentService;
        private readonly CurrencySettings _currencySettings;

        public CurrencyController(
            SmartDbContext db,
            ILocalizedEntityService localizedEntityService,
            IStoreMappingService storeMappingService,
            ModuleManager moduleManager,
            IPaymentService paymentService,
            CurrencySettings currencySettings)
        {
            _db = db;
            _localizedEntityService = localizedEntityService;
            _storeMappingService = storeMappingService;
            _moduleManager = moduleManager;
            _paymentService = paymentService;
            _currencySettings = currencySettings;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
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

            foreach (var erp in Services.CurrencyService.LoadAllExchangeRateProviders())
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

            await Services.Settings.ApplySettingAsync(_currencySettings, x => x.ActiveExchangeRateProviderSystemName);
            await Services.Settings.ApplySettingAsync(_currencySettings, x => x.AutoUpdateEnabled);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(List), "Currency");
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Currency.Read)]
        public async Task<IActionResult> CurrencyList(GridCommand command)
        {
            var currencies = await _db.Currencies
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var mapper = MapperFactory.GetMapper<Currency, CurrencyModel>();
            var currencyModels = await currencies
                .SelectAwait(async x =>
                {
                    var model = await mapper.MapAsync(x);
                    model.IsPrimaryCurrency = model.Id == _currencySettings.PrimaryCurrencyId;
                    model.IsPrimaryExchangeCurrency = model.Id == _currencySettings.PrimaryExchangeCurrencyId;
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
        [Permission(Permissions.Configuration.Currency.Update)]
        public async Task<IActionResult> CurrencyUpdate(CurrencyModel model)
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
        [Permission(Permissions.Configuration.Currency.Delete)]
        public async Task<IActionResult> CurrencyDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                try
                {
                    var currencies = await _db.Currencies.GetManyAsync(ids, true);
                    _db.Currencies.RemoveRange(currencies);

                    numDeleted = await _db.SaveChangesAsync();
                    success = true;
                }
                catch (Exception ex)
                {
                    NotifyError(ex);
                }
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Currency.Read)]
        public async Task<IActionResult> LiveRateList()
        {
            var language = Services.WorkContext.WorkingLanguage;
            var rates = new List<ExchangeRate>();
            var ratesCount = 0;
            var allCurrenciesByIsoCode = (await _db.Currencies
                .AsNoTracking()
                .ToListAsync())
                .ToDictionarySafe(x => x.CurrencyCode.EmptyNull().ToUpper(), x => x);

            try
            {
                var primaryExchangeCurrency = Services.CurrencyService.PrimaryExchangeCurrency;
                if (primaryExchangeCurrency == null)
                {
                    throw new InvalidOperationException(T("Admin.System.Warnings.ExchangeCurrency.NotSet"));
                }

                rates = (await Services.CurrencyService.GetCurrencyLiveRatesAsync(primaryExchangeCurrency.CurrencyCode)).ToList();

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
        [Permission(Permissions.Configuration.Currency.EditExchangeRate)]
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

        // AJAX
        [HttpPost]
        [Permission(Permissions.Configuration.Currency.Update)]
        public async Task<IActionResult> SetPrimaryCurrency(int id, bool forExchange)
        {
            var currency = await _db.Currencies.FindByIdAsync(id, false);
            if (currency == null)
            {
                return NotFound();
            }

            if (forExchange)
            {
                _currencySettings.PrimaryExchangeCurrencyId = currency.Id;
            }
            else
            {
                _currencySettings.PrimaryCurrencyId = currency.Id;
            }

            await Services.SettingFactory.SaveSettingsAsync(_currencySettings);

            return new JsonResult(new { success = true });
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
                await _storeMappingService.ApplyStoreMappingsAsync(currency, model.SelectedStoreIds);
                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.Configuration.Currencies.Added"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), new { id = currency.Id })
                    : RedirectToAction(nameof(List));
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

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Name = currency.GetLocalized(x => x.Name, languageId, false, false);
            });

            await PrepareCurrencyModelAsync(model, currency, false);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
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
                try
                {
                    await MapperFactory.MapAsync(model, currency);

                    currency.DomainEndings = string.Join(",", model.DomainEndingsArray ?? Array.Empty<string>());

                    await UpdateLocalesAsync(currency, model);
                    await _storeMappingService.ApplyStoreMappingsAsync(currency, model.SelectedStoreIds);
                    await _db.SaveChangesAsync();

                    NotifySuccess(T("Admin.Configuration.Currencies.Updated"));

                    return continueEditing
                        ? RedirectToAction(nameof(Edit), new { id = currency.Id })
                        : RedirectToAction(nameof(List));
                }
                catch (Exception ex)
                {
                    NotifyError(ex);
                }
            }

            await PrepareCurrencyModelAsync(model, currency, true);

            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [Permission(Permissions.Configuration.Currency.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var currency = await _db.Currencies.FindByIdAsync(id);
            if (currency == null)
            {
                return NotFound();
            }

            try
            {
                _db.Currencies.Remove(currency);
                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.Configuration.Currencies.Deleted"));

                return RedirectToAction(nameof(List));
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction(nameof(Edit), new { id = currency.Id });
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
                    var clone = currency?.Clone() ?? new Currency();
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

        #region Utilities

        private async Task UpdateLocalesAsync(Currency currency, CurrencyModel model)
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
                model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(currency.CreatedOnUtc, DateTimeKind.Utc);
                model.IsPrimaryCurrency = currency.Id == _currencySettings.PrimaryCurrencyId;
                model.IsPrimaryExchangeCurrency = currency.Id == _currencySettings.PrimaryExchangeCurrencyId;
            }

            if (!excludeProperties)
            {
                model.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(currency);

                foreach (var ending in model.DomainEndings.SplitSafe(','))
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

                model.DomainEndingsArray = model.DomainEndings.SplitSafe(',').ToArray();
            }
        }

        #endregion
    }
}
