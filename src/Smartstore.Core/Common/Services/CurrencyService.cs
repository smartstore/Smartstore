using Smartstore.Caching;
using Smartstore.Core.Common.Configuration;
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
        // 0 = exchange rate currency code.
        // 1 = provider system name.
        const string LiveCurrencyRatesKey = "live.currency.rates:{0}-{1}";

        private readonly SmartDbContext _db;
        private readonly ICacheManager _cache;
        private readonly IProviderManager _providerManager;
        private readonly IWorkContext _workContext;
        private readonly CurrencySettings _currencySettings;
        private readonly ISettingFactory _settingFactory;
        private readonly IRoundingHelper _roundingHelper;

        private Currency _primaryCurrency;
        private Currency _primaryExchangeCurrency;

        public CurrencyService(
            SmartDbContext db,
            ICacheManager cache,
            IProviderManager providerManager,
            IWorkContext workContext,
            CurrencySettings currencySettings,
            ISettingFactory settingFactory,
            IRoundingHelper roundingHelper)
        {
            _db = db;
            _cache = cache;
            _providerManager = providerManager;
            _workContext = workContext;
            _currencySettings = currencySettings;
            _settingFactory = settingFactory;
            _roundingHelper = roundingHelper;
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

            Guard.NotNull(amount.Currency);
            return amount.ExchangeTo(_workContext.WorkingCurrency, PrimaryExchangeCurrency);
        }

        public virtual Money ConvertToWorkingCurrency(decimal amount)
        {
            return new Money(amount, PrimaryCurrency).ExchangeTo(_workContext.WorkingCurrency, PrimaryExchangeCurrency);
        }

        #endregion

        #region Exchange rate provider

        public virtual async Task<IList<ExchangeRate>> GetCurrencyLiveRatesAsync(bool force = false)
        {
            var exchangeRateCurrencyCode = PrimaryExchangeCurrency?.CurrencyCode;
            if (exchangeRateCurrencyCode.IsEmpty())
            {
                throw new InvalidOperationException(T("Admin.System.Warnings.ExchangeCurrency.NotSet"));
            }

            var key = LiveCurrencyRatesKey.FormatInvariant(exchangeRateCurrencyCode.ToLowerInvariant(), _currencySettings.ActiveExchangeRateProviderSystemName.EmptyNull());

            if (force)
            {
                await _cache.RemoveAsync(key);
            }

            // No need to delete the cache entry by pattern key. Let it expire naturally.
            var exchangeRates = await _cache.GetAsync(key, async o =>
            {
                o.ExpiresIn(TimeSpan.FromHours(24));

                var provider = LoadActiveExchangeRateProvider();
                if (provider != null)
                {
                    return await provider.Value.GetCurrencyLiveRatesAsync(exchangeRateCurrencyCode);
                }

                return new List<ExchangeRate>();
            });

            return exchangeRates;
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

        public virtual Money CreateMoney(
            decimal amount,
            object currencyCodeOrObj = null,
            bool displayCurrency = true, 
            bool roundIfEnabled = true)
        {
            Currency currency = null;

            if (currencyCodeOrObj is null)
            {
                currency = _workContext.WorkingCurrency;
            }
            else if (currencyCodeOrObj is string currencyCode)
            {
                Guard.NotEmpty(currencyCode);
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

            if (roundIfEnabled)
            {
                amount = _roundingHelper.RoundIfEnabledFor(amount, currency);
            }

            return new Money(amount, currency, !displayCurrency);
        }
    }
}