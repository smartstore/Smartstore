using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Data.Hooks;
using Smartstore.Engine.Modularity;
using EState = Smartstore.Data.EntityState;

namespace Smartstore.Core.Common.Services
{
    [Important]
    public partial class CurrencyService : AsyncDbSaveHook<Currency>, ICurrencyService
    {
        private readonly SmartDbContext _db;
        private readonly ILocalizationService _localizationService;
        private readonly IProviderManager _providerManager;
        private readonly IWorkContext _workContext;
        private readonly CurrencySettings _currencySettings;
        private readonly TaxSettings _taxSettings;
        private readonly ISettingFactory _settingFactory;

        private Currency _primaryCurrency;
        private Currency _primaryExchangeCurrency;

        public CurrencyService(
            SmartDbContext db,
            ILocalizationService localizationService,
            IProviderManager providerManager,
            IWorkContext workContext,
            CurrencySettings currencySettings,
            TaxSettings taxSettings,
            ISettingFactory settingFactory)
        {
            _db = db;
            _localizationService = localizationService;
            _providerManager = providerManager;
            _workContext = workContext;
            _currencySettings = currencySettings;
            _taxSettings = taxSettings;
            _settingFactory = settingFactory;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        #region Hook

        private string _hookErrorMessage;

        public override async Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entry.Entity is Currency currency)
            {
                if (entry.InitialState == EState.Deleted)
                {
                    // Ensure that we do not delete the primary or exchange currency.
                    if (currency.Id == _currencySettings.PrimaryCurrencyId || currency.Id == _currencySettings.PrimaryExchangeCurrencyId)
                    {
                        _hookErrorMessage = currency.Id == _currencySettings.PrimaryCurrencyId
                            ? T("Admin.Configuration.Currencies.CannotDeletePrimaryCurrency", currency.Name.NaIfEmpty())
                            : T("Admin.Configuration.Currencies.CannotDeleteExchangeCurrency", currency.Name.NaIfEmpty());
                    }
                    else if (currency.Published && !await _db.Currencies.AnyAsync(x => x.Published && x.Id != currency.Id, cancelToken))
                    {
                        _hookErrorMessage = T("Admin.Configuration.Currencies.PublishedCurrencyRequired");
                    }
                }
                else if (entry.InitialState == EState.Modified)
                {
                    // Ensure that we have at least one published currency.
                    if (!currency.Published && !await _db.Currencies.AnyAsync(x => x.Published && x.Id != currency.Id, cancelToken))
                    {
                        _hookErrorMessage = T("Admin.Configuration.Currencies.PublishedCurrencyRequired");
                    }
                }

                if (_hookErrorMessage.HasValue())
                {
                    entry.ResetState();
                }
            }

            // We need to return HookResult.Ok instead of HookResult.Failed to be able to output an error notification.
            return HookResult.Ok;
        }

        public override Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_hookErrorMessage.HasValue())
            {
                var message = new string(_hookErrorMessage);
                _hookErrorMessage = null;

                throw new HookException(message);
            }

            return Task.CompletedTask;
        }

        #endregion

        public virtual Currency PrimaryCurrency
        {
            get => _primaryCurrency ??= GetPrimaryCurrency(false);
            set => _primaryCurrency = value;
        }

        public virtual Currency PrimaryExchangeCurrency
        {
            get => _primaryExchangeCurrency ??= GetPrimaryCurrency(true);
            set => _primaryExchangeCurrency = value;
        }

        private Currency GetPrimaryCurrency(bool forExchange)
        {
            var currencyId = forExchange ? _currencySettings.PrimaryExchangeCurrencyId : _currencySettings.PrimaryCurrencyId;
            var currency = _db.Currencies.FindById(currencyId, false);

            if (currency == null)
            {
                var allCurrencies = _db.Currencies.AsNoTracking().ToList();
                currency =
                    allCurrencies.FirstOrDefault(x => x.Published) ??
                    allCurrencies.FirstOrDefault() ??
                    throw new InvalidOperationException("Unable to load primary currency.");

                if (forExchange)
                {
                    _currencySettings.PrimaryExchangeCurrencyId = currency.Id;
                }
                else
                {
                    _currencySettings.PrimaryCurrencyId = currency.Id;
                }

                _settingFactory.SaveSettingsAsync(_currencySettings).Await();
            }

            return currency;
        }

        #region Currency conversion

        public virtual Money ConvertToWorkingCurrency(Money amount)
        {
            if (amount.Currency == _workContext.WorkingCurrency)
            {
                // Perf
                return amount;
            }

            Guard.NotNull(amount.Currency, nameof(amount.Currency));
            return amount.ExchangeTo(_workContext.WorkingCurrency, PrimaryExchangeCurrency);
        }

        public virtual Money ConvertToWorkingCurrency(decimal amount)
        {
            return new Money(amount, PrimaryCurrency).ExchangeTo(_workContext.WorkingCurrency, PrimaryExchangeCurrency);
        }

        #endregion

        #region Exchange rate provider

        public virtual Task<IList<ExchangeRate>> GetCurrencyLiveRatesAsync(string exchangeRateCurrencyCode)
        {
            var exchangeRateProvider = LoadActiveExchangeRateProvider();
            if (exchangeRateProvider != null)
            {
                return exchangeRateProvider.Value.GetCurrencyLiveRatesAsync(exchangeRateCurrencyCode);
            }
            else
            {
                return Task.FromResult<IList<ExchangeRate>>(new List<ExchangeRate>());
            }
        }

        public virtual Provider<IExchangeRateProvider> LoadActiveExchangeRateProvider()
        {
            return LoadExchangeRateProviderBySystemName(_currencySettings.ActiveExchangeRateProviderSystemName) ?? LoadAllExchangeRateProviders().FirstOrDefault();
        }

        public virtual Provider<IExchangeRateProvider> LoadExchangeRateProviderBySystemName(string systemName)
        {
            return _providerManager.GetProvider<IExchangeRateProvider>(systemName);
        }

        public virtual IEnumerable<Provider<IExchangeRateProvider>> LoadAllExchangeRateProviders()
        {
            return _providerManager.GetAllProviders<IExchangeRateProvider>();
        }

        #endregion

        public virtual Money CreateMoney(decimal price, bool displayCurrency = true, object currencyCodeOrObj = null)
        {
            Currency currency = null;

            if (currencyCodeOrObj is null)
            {
                currency = _workContext.WorkingCurrency;
            }
            else if (currencyCodeOrObj is string currencyCode)
            {
                Guard.NotEmpty(currencyCode, nameof(currencyCodeOrObj));
                currency =
                    (currencyCode == PrimaryCurrency.CurrencyCode ? PrimaryCurrency : null) ??
                    (currencyCode == PrimaryExchangeCurrency.CurrencyCode ? PrimaryExchangeCurrency : null) ??
                    _db.Currencies.FirstOrDefault(x => x.CurrencyCode == currencyCode) ??
                    new Currency { CurrencyCode = currencyCode };
            }
            else if (currencyCodeOrObj is Currency)
            {
                currency = (Currency)currencyCodeOrObj;
            }

            if (currency == null)
            {
                throw new ArgumentException("Currency parameter must either be a valid currency code as string or an actual currency entity instance.", nameof(currencyCodeOrObj));
            }

            return new Money(price, currency, !displayCurrency);
        }
    }
}