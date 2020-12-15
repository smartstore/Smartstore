using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;
using Smartstore.Data.Caching;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Common.Services
{
    public partial class CurrencyService : ICurrencyService
    {
        private readonly SmartDbContext _db;
        private readonly IStoreMappingService _storeMappingService;
        private readonly CurrencySettings _currencySettings;
        private readonly IProviderManager _providerManager;  
        private readonly IStoreContext _storeContext;

        public CurrencyService(
            SmartDbContext db,
            IStoreMappingService storeMappingService,
            CurrencySettings currencySettings,
            IProviderManager providerManager,
            IStoreContext storeContext)
        {
            _db = db;
            _storeMappingService = storeMappingService;
            _currencySettings = currencySettings;
            _providerManager = providerManager;
            _storeContext = storeContext;
        }

        public virtual async Task<IList<ExchangeRate>> GetCurrencyLiveRatesAsync(string exchangeRateCurrencyCode)
        {
            var exchangeRateProvider = LoadActiveExchangeRateProvider();
            if (exchangeRateProvider != null)
            {
                return await exchangeRateProvider.Value.GetCurrencyLiveRatesAsync(exchangeRateCurrencyCode);
            }

            return new List<ExchangeRate>();
        }

        public virtual async Task<IList<Currency>> GetCurrenciesAsync(bool showHidden = false, int storeId = 0)
        {
            var query = _db.Currencies
                .AsNoTracking()
                .AsCaching();

            if (!showHidden)
                query = query.Where(c => c.Published);

            query = query.OrderBy(c => c.DisplayOrder);

            if (storeId > 0)
            {
                query = query
                    .Where(c => _storeMappingService.AuthorizeAsync(c, storeId).Await());
            }

            return await query.ToListAsync();
        }

        public virtual decimal ConvertCurrency(decimal amount, decimal exchangeRate)
        {
            if (amount != decimal.Zero && exchangeRate != decimal.Zero)
                return amount * exchangeRate;

            return decimal.Zero;
        }

        public virtual decimal ConvertCurrency(decimal amount, Currency sourceCurrency, Currency targetCurrency, Store store = null)
        {
            decimal result = amount;
            if (sourceCurrency.Id == targetCurrency.Id)
                return result;

            if (result != decimal.Zero && sourceCurrency.Id != targetCurrency.Id)
            {
                result = ConvertToPrimaryExchangeRateCurrency(result, sourceCurrency, store);
                result = ConvertFromPrimaryExchangeRateCurrency(result, targetCurrency, store);
            }

            return result;
        }

        public virtual decimal ConvertToPrimaryExchangeRateCurrency(decimal amount, Currency sourceCurrency, Store store = null)
        {
            decimal result = amount;
            var primaryExchangeRateCurrency = store == null ? _storeContext.CurrentStore.PrimaryExchangeRateCurrency : store.PrimaryExchangeRateCurrency;

            if (result != decimal.Zero && sourceCurrency.Id != primaryExchangeRateCurrency.Id)
            {
                decimal exchangeRate = sourceCurrency.Rate;
                if (exchangeRate == decimal.Zero)
                    throw new SmartException(string.Format("Exchange rate not found for currency [{0}]", sourceCurrency.Name));

                result /= exchangeRate;
            }
            return result;
        }

        public virtual decimal ConvertFromPrimaryExchangeRateCurrency(decimal amount, Currency targetCurrency, Store store = null)
        {
            decimal result = amount;
            var primaryExchangeRateCurrency = store == null ? _storeContext.CurrentStore.PrimaryExchangeRateCurrency : store.PrimaryExchangeRateCurrency;

            if (result != decimal.Zero && targetCurrency.Id != primaryExchangeRateCurrency.Id)
            {
                decimal exchangeRate = targetCurrency.Rate;
                if (exchangeRate == decimal.Zero)
                    throw new SmartException(string.Format("Exchange rate not found for currency [{0}]", targetCurrency.Name));

                result *= exchangeRate;
            }
            return result;
        }

        public virtual decimal ConvertToPrimaryStoreCurrency(decimal amount, Currency sourceCurrency, Store store = null)
        {
            decimal result = amount;
            var primaryStoreCurrency = store == null ? _storeContext.CurrentStore.PrimaryStoreCurrency : store.PrimaryStoreCurrency;

            if (result != decimal.Zero && sourceCurrency.Id != primaryStoreCurrency.Id)
            {
                decimal exchangeRate = sourceCurrency.Rate;
                if (exchangeRate == decimal.Zero)
                    throw new SmartException(string.Format("Exchange rate not found for currency [{0}]", sourceCurrency.Name));

                result /= exchangeRate;
            }
            return result;
        }

        public virtual decimal ConvertFromPrimaryStoreCurrency(decimal amount, Currency targetCurrency, Store store = null)
        {
            var primaryStoreCurrency = store == null ? _storeContext.CurrentStore.PrimaryStoreCurrency : store.PrimaryStoreCurrency;
            return ConvertCurrency(amount, primaryStoreCurrency, targetCurrency, store);
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
    }
}