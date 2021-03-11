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
        /// Exchanges given <see cref="Money"/> amount to <see cref="Store.PrimaryStoreCurrency"/>
        /// of passed <paramref name="store"/>, or of <see cref="IStoreContext.CurrentStore"/> if <paramref name="store"/> is <c>null</c>.
        /// </summary>
        /// <param name="amount">The source amount to exchange</param>
        /// <param name="store">Store instance or <c>null</c> to auto-resolve current store.</param>
        /// <returns>The exchanged amount.</returns>
        Money ConvertToPrimaryCurrency(Money amount, Store store = null);

        /// <summary>
        /// Exchanges given <see cref="Money"/> amount to <see cref="Store.PrimaryExchangeRateCurrency"/>
        /// of passed <paramref name="store"/>, or of <see cref="IStoreContext.CurrentStore"/> if <paramref name="store"/> is <c>null</c>.
        /// </summary>
        /// <param name="amount">The source amount to exchange</param>
        /// <param name="store">Store instance or <c>null</c> to auto-resolve current store.</param>
        /// <returns>The exchanged amount.</returns>
        Money ConvertToExchangeRateCurrency(Money amount, Store store = null);

        /// <summary>
        /// Exchanges given <see cref="Money"/> amount to <see cref="IWorkContext.WorkingCurrency"/>,
        /// using <see cref="Store.PrimaryExchangeRateCurrency"/> as exchange rate currency.
        /// </summary>
        /// <param name="amount">The source amount to exchange</param>
        /// <param name="store">Store instance or <c>null</c> to auto-resolve current store's <see cref="Store.PrimaryExchangeRateCurrency"/>.</param>
        /// <returns>The exchanged amount.</returns>
        Money ConvertToWorkingCurrency(Money amount, Store store = null);

        /// <summary>
        /// Exchanges given money amount (which is assumed to be in <see cref="Store.PrimaryStoreCurrency"/>) to <see cref="IWorkContext.WorkingCurrency"/>,
        /// using <see cref="Store.PrimaryExchangeRateCurrency"/> as exchange rate currency.
        /// </summary>
        /// <param name="amount">The source amount to exchange (should be in <see cref="Store.PrimaryStoreCurrency"/>).</param>
        /// <param name="store">Store instance or <c>null</c> to auto-resolve current store's <see cref="Store.PrimaryExchangeRateCurrency"/>.</param>
        /// <returns>The exchanged amount.</returns>
        Money ConvertToWorkingCurrency(decimal amount, Store store = null);

        /// <summary>
        /// Exchanges given <see cref="Money"/> amount to <paramref name="targetCurrency"/>,
        /// using <see cref="Store.PrimaryExchangeRateCurrency"/> as exchange rate currency.
        /// </summary>
        /// <param name="amount">The source amount to exchange.</param>
        /// <param name="targetCurrency">The target currency to exchange amount to.</param>
        /// <param name="store">Store instance or <c>null</c> to auto-resolve current store's <see cref="Store.PrimaryExchangeRateCurrency"/>.</param>
        /// <returns>The exchanged amount.</returns>
        Money ConvertToCurrency(Money amount, Currency targetCurrency, Store store = null);

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
        ///     Creates and returns a <see cref="Money"/> struct.
        /// </summary>
        /// <param name="price">
        ///     The money amount.
        /// </param>
        /// <param name="displayCurrency">
        ///     A value indicating whether to display the currency symbol/code.
        /// </param>
        /// <param name="currencyCodeOrObj">
        ///     Target currency as string code (e.g. USD) or an actual <see cref="Currency"/> instance. If <c>null</c>,
        ///     currency will be obtained via <see cref="IWorkContext.WorkingCurrency"/>.
        /// </param>
        /// <returns>Money</returns>
        Money CreateMoney(decimal price, bool displayCurrency = true, object currencyCodeOrObj = null);

        /// <summary>
        ///     Gets a tax formatting pattern that can be applied to
        ///     <see cref="Money"/> values by calling <see cref="Money.WithPostFormat(string)"/>.
        /// </summary>
        /// <param name="displayTaxSuffix">
        ///     A value indicating whether to display the tax suffix.
        ///     If <c>null</c>, current setting will be obtained via <see cref="TaxSettings.DisplayTaxSuffix"/> and
        ///     additionally via <see cref="TaxSettings.ShippingPriceIncludesTax"/> or <see cref="TaxSettings.PaymentMethodAdditionalFeeIncludesTax"/>
        ///     according to <paramref name="target"/>.
        /// </param>
        /// <param name="priceIncludesTax">
        ///     A value indicating whether given price includes tax already.
        ///     If <c>null</c>, current setting will be obtained via <see cref="IWorkContext.TaxDisplayType"/>.
        /// </param>
        /// <param name="target">
        ///     The target object to format price for. This parameter affects how <paramref name="displayTax"/>
        ///     will be auto-resolved if it is <c>null</c>.
        /// </param>
        /// <param name="language">
        ///     Language for tax suffix. If <c>null</c>, language will be obtained via <see cref="IWorkContext.WorkingLanguage"/>.
        /// </param>
        /// <returns>Money</returns>
        string GetTaxFormat(
            bool? displayTaxSuffix = null,
            bool? priceIncludesTax = null,
            PricingTarget target = PricingTarget.Product,
            Language language = null);
    }
}