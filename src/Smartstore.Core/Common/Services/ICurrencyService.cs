using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Common;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Common.Services
{
    /// <summary>
    /// Currency service interface
    /// </summary>
    public partial interface ICurrencyService
    {
        /// <summary>
        /// Gets currency live rates
        /// </summary>
        /// <param name="exchangeRateCurrencyCode">Exchange rate currency code</param>
        /// <returns>Exchange rates</returns>
        Task<IList<ExchangeRate>> GetCurrencyLiveRatesAsync(string exchangeRateCurrencyCode);

        /// <summary>
        /// Gets all currencies and orders by <see cref="Currency.DisplayOrder"/>
        /// </summary>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
		/// <param name="storeId">Loads records only allowed in specified store. Pass 0 to load all records.</param>
		/// <returns>Currencies</returns>
		Task<IList<Currency>> GetCurrenciesAsync(bool showHidden = false, int storeId = 0);


        /// <summary>
        /// Converts currency
        /// </summary>
        /// <param name="amount">Amount</param>
        /// <param name="exchangeRate">Currency exchange rate</param>
        /// <returns>Converted value</returns>
        decimal ConvertCurrency(decimal amount, decimal exchangeRate);

        /// <summary>
        /// Converts currency
        /// </summary>
        /// <param name="amount">Amount</param>
        /// <param name="sourceCurrency">Source currency code</param>
        /// <param name="targetCurrency">Target currency code</param>
		/// <param name="store">Store to get the primary currencies from</param>
        /// <returns>Converted value</returns>
		decimal ConvertCurrency(decimal amount, Currency sourceCurrency, Currency targetCurrency, Store store = null);

        /// <summary>
        /// Converts to primary exchange rate currency 
        /// </summary>
        /// <param name="amount">Amount</param>
        /// <param name="sourceCurrencyCode">Source currency code</param>
		/// <param name="store">Store to get the primary exchange rate currency from</param>
        /// <returns>Converted value</returns>
		decimal ConvertToPrimaryExchangeRateCurrency(decimal amount, Currency sourceCurrencyCode, Store store = null);

        /// <summary>
        /// Converts from primary exchange rate currency
        /// </summary>
        /// <param name="amount">Amount</param>
        /// <param name="targetCurrency">Target currency code</param>
		/// <param name="store">Store to get the primary exchange rate currency from</param>
        /// <returns>Converted value</returns>
		decimal ConvertFromPrimaryExchangeRateCurrency(decimal amount, Currency targetCurrency, Store store = null);

        /// <summary>
        /// Converts to primary store currency 
        /// </summary>
        /// <param name="amount">Amount</param>
        /// <param name="sourceCurrency">Source currency code</param>
		/// <param name="store">Store to get the primary store currency from</param>
        /// <returns>Converted value</returns>
		decimal ConvertToPrimaryStoreCurrency(decimal amount, Currency sourceCurrency, Store store = null);

        /// <summary>
        /// Converts from primary store currency
        /// </summary>
        /// <param name="amount">Amount</param>
        /// <param name="targetCurrency">Target currency code</param>
		/// <param name="store">Store to get the primary store currency from</param>
        /// <returns>Converted value</returns>
		decimal ConvertFromPrimaryStoreCurrency(decimal amount, Currency targetCurrency, Store store = null);



        /// <summary>
        /// Load active exchange rate provider
        /// </summary>
        /// <returns>Active exchange rate provider</returns>
        // TODO: (MH) (core) Implement when provider is available
        //Provider<IExchangeRateProvider> LoadActiveExchangeRateProvider();

        /// <summary>
        /// Load exchange rate provider by system name
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <returns>Found exchange rate provider</returns>
        // TODO: (MH) (core) Implement when provider is available
        //Provider<IExchangeRateProvider> LoadExchangeRateProviderBySystemName(string systemName);

        /// <summary>
        /// Load all exchange rate providers
        /// </summary>
        /// <returns>Exchange rate providers</returns>
        // TODO: (MH) (core) Implement when provider is available
        //IEnumerable<Provider<IExchangeRateProvider>> LoadAllExchangeRateProviders();
    }
}