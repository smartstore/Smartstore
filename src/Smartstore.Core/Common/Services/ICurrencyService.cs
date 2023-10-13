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

        #region Currency conversion

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

        #endregion

        #region Exchange rate provider

        /// <summary>
        /// Gets live exchange rates for the <see cref="PrimaryExchangeCurrency"/>.
        /// </summary>
        /// <param name="force">
        /// <c>true</c> to get the live rates from <see cref="IExchangeRateProvider"/>.
        /// <c>false</c> to load the currency rates from cache.</param> Cache duration is 24 hours. 
        /// <returns>Currency exchange rates.</returns>
        Task<IList<ExchangeRate>> GetCurrencyLiveRatesAsync(bool force = false);

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

        #endregion

        /// <summary>
        /// Creates and returns a <see cref="Money"/> struct.
        /// </summary>
        /// <param name="amount">The money amount.</param>
        /// <param name="currencyCodeOrObj">
        /// Target currency as string code (e.g. USD) or an actual <see cref="Currency"/> instance.
        /// If <c>null</c>, currency will be obtained via <see cref="IWorkContext.WorkingCurrency"/>.
        /// </param>
        /// <param name="displayCurrency">A value indicating whether to display the currency symbol/code.</param>
        /// <param name="roundIfEnabled">
        /// A value indicating whether to rounds <paramref name="amount"/> if rounding is enabled for the currency specified by <paramref name="currencyCodeOrObj"/>.
        /// </param>
        /// <returns>Money</returns>
        Money CreateMoney(decimal amount, object currencyCodeOrObj = null, bool displayCurrency = true, bool roundIfEnabled = true);
    }
}