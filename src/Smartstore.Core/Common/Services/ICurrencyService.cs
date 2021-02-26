using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Common.Services
{
    /// <summary>
    /// Currency service interface.
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
		Task<List<Currency>> GetAllCurrenciesAsync(bool showHidden = false, int storeId = 0);


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
        Provider<IExchangeRateProvider> LoadActiveExchangeRateProvider();

        /// <summary>
        /// Load exchange rate provider by system name
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <returns>Found exchange rate provider</returns>
        Provider<IExchangeRateProvider> LoadExchangeRateProviderBySystemName(string systemName);

        /// <summary>
        /// Load all exchange rate providers
        /// </summary>
        /// <returns>Exchange rate providers</returns>
        IEnumerable<Provider<IExchangeRateProvider>> LoadAllExchangeRateProviders();

        /// <summary>
        /// Creates and returns a money struct, including tax infomation to format a price with tax suffix.
        /// </summary>
        /// <param name="price">Price.</param>
        /// <param name="displayCurrency">
        /// A value indicating whether to display the currency symbol/code.
        /// </param>
        /// <param name="currencyCodeOrObj">
        /// Target currency as string code (e.g. USD) or an actual <see cref="Currency"/> instance. If <c>null</c>,
        /// currency will be obtained via <see cref="IWorkContext.WorkingCurrency"/>.
        /// </param>
        /// <param name="language">
        /// Language for tax suffix. If <c>null</c>, language will be obtained via <see cref="IWorkContext.WorkingLanguage"/>.
        /// </param>
        /// <param name="priceIncludesTax">
        /// A value indicating whether given price includes tax already.
        /// If <c>null</c>, current setting will be obtained via <see cref="IWorkContext.TaxDisplayType"/>.
        /// </param>
        /// <param name="displayTax">
        /// A value indicating whether to display the tax suffix.
        /// If <c>null</c>, current setting will be obtained via <see cref="TaxSettings.DisplayTaxSuffix"/> and
        /// additionally via <see cref="TaxSettings.ShippingPriceIncludesTax"/> or <see cref="TaxSettings.PaymentMethodAdditionalFeeIncludesTax"/>
        /// according to <paramref name="target"/>.
        /// </param>
        /// <param name="target">
        /// The target object to format price for. This parameter affects how <paramref name="displayTax"/>
        /// will be auto-resolved if it is <c>null</c>.
        /// </param>
        /// <returns>Money.</returns>
        Money CreateMoney(
            decimal price,
            bool displayCurrency = true,
            object currencyCodeOrObj = null,
            Language language = null,
            bool? priceIncludesTax = null,
            bool? displayTax = null,
            PricingTarget target = PricingTarget.Product);
    }
}