using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Localization;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Common.Services
{
    /// <summary>
    /// Currency service interface.
    /// </summary>
    public partial interface ICurrencyService
    {
        /// <summary>
        /// Gets the primary currency (in which all money amounts are entered in backend).
        /// </summary>
        /// <remarks>The setter is for testing purposes only.</remarks>
        Currency PrimaryCurrency { get; set; }

        /// <summary>
        /// Gets the primary exchange currency which is used to calculate money conversions.
        /// </summary>
        /// <remarks>The setter is for testing purposes only.</remarks>
        Currency PrimaryExchangeCurrency { get; }

        /// <summary>
        /// Exchanges given <see cref="Money"/> amount to <see cref="IWorkContext.WorkingCurrency"/>,
        /// using <see cref="PrimaryExchangeCurrency"/> as exchange rate currency.
        /// </summary>
        /// <param name="amount">The source amount to exchange</param>
        /// <returns>The exchanged amount.</returns>
        Money ConvertToWorkingCurrency(Money amount);

        /// <summary>
        /// Exchanges given money amount (which is assumed to be in <see cref="PrimaryCurrency"/>) to <see cref="IWorkContext.WorkingCurrency"/>,
        /// using <see cref="PrimaryExchangeCurrency"/> as exchange rate currency.
        /// </summary>
        /// <param name="amount">The source amount to exchange (should be in <see cref="PrimaryCurrency"/>).</param>
        /// <returns>The exchanged amount.</returns>
        Money ConvertToWorkingCurrency(decimal amount);

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