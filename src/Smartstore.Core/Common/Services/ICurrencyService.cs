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
        /// Converts a currency amount.
        /// </summary>
        /// <param name="amount">Currency amount to be converted.</param>
        /// <param name="exchangeRate">Exchange rate.</param>
        /// <returns>Converted currency amount.</returns>
        Money ConvertCurrency(Money amount, decimal exchangeRate);

        /// <summary>
        /// Converts a currency amount.
        /// </summary>
        /// <param name="amount">Currency amount to be converted.</param>
        /// <param name="targetCurrency">The currency into which the conversion is made.</param>
        /// <param name="store">Store. If <c>null</c>, store will be obtained via <see cref="IStoreContext.CurrentStore"/>.</param>
        /// <returns>Converted currency amount where <see cref="Money.Currency"/> is <paramref name="targetCurrency"/>.</returns>
        Money ConvertCurrency(Money amount, Currency targetCurrency, Store store = null);

        /// <summary>
        /// Converts a currency amount into the exchange rate or primary currency of a store.
        /// </summary>
        /// <param name="toExchangeRateCurrency"><c>true</c> convert to exchange rate currency. <c>false</c> convert to primary store currency.</param>
        /// <param name="amount">Source currency and amount to be converted.</param>
        /// <param name="store">Store. If <c>null</c>, store will be obtained via <see cref="IStoreContext.CurrentStore"/>.</param>
        /// <returns>Converted currency amount where <see cref="Money.Currency"/> is the corresponding store currency.</returns>
        Money ConvertToStoreCurrency(bool toExchangeRateCurrency, Money amount, Store store = null);

        /// <summary>
        /// Converts a currency amount from the exchange rate or primary currency of a store.
        /// </summary>
        /// <param name="fromExchangeRateCurrency"><c>true</c> convert from exchange rate currency. <c>false</c> convert from primary store currency.</param>
        /// <param name="amount">Target currency and amount to be converted.</param>
        /// <param name="store">Store. If <c>null</c>, store will be obtained via <see cref="IStoreContext.CurrentStore"/>.</param>
        /// <returns>Converted currency amount where <see cref="Money.Currency"/> is the currency of <paramref name="amount"/>.</returns>
        Money ConvertFromStoreCurrency(bool fromExchangeRateCurrency, Money amount, Store store = null);

        /// <summary>
        /// Gets currency live rates
        /// </summary>
        /// <param name="exchangeRateCurrencyCode">Exchange rate currency code</param>
        /// <returns>Exchange rates</returns>
        Task<IList<ExchangeRate>> GetCurrencyLiveRatesAsync(string exchangeRateCurrencyCode);

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