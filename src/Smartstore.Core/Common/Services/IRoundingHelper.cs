using Smartstore.Core.Checkout.Tax;

namespace Smartstore.Core.Common.Services
{
    /// <summary>
    /// Represents rounding of currency amounts.
    /// </summary>
    public partial interface IRoundingHelper
    {
        /// <summary>Rounds <paramref name="amount"/>.</summary>
        /// <param name="amount">Amount to round.</param>
        /// <param name="currency">
        /// Rounds <paramref name="amount"/> using <see cref="Currency.RoundNumDecimals"/> and <see cref="Currency.MidpointRounding"/>.
        /// If <c>null</c>, currency will be obtained via <see cref="IWorkContext.WorkingCurrency"/>.
        /// </param>
        /// <returns>Rounded amount.</returns>
        decimal Round(decimal amount, Currency currency = null);

        /// <summary>
        /// Rounds <paramref name="amount"/>.
        /// </summary>
        /// <param name="amount">Amount to round.</param>
        /// <param name="decimals">Number of decimal places (precision).</param>
        /// <param name="midpointRounding">The rounding strategy of the midway between two currency amounts.</param>
        /// <returns>Rounded amount.</returns>
        decimal Round(decimal amount, int decimals, CurrencyMidpointRounding midpointRounding = CurrencyMidpointRounding.AwayFromZero);

        /// <summary>
        /// Gets a value indicating whether rounding during shopping cart calculation is enabled.
        /// </summary>
        /// <param name="currency">
        /// Currency. If <c>null</c>, currency will be obtained via <see cref="IWorkContext.WorkingCurrency"/>.
        /// </param>
        /// <param name="taxDisplayType">
        /// Tax display type. If <c>null</c>, type will be obtained via <see cref="IWorkContext.TaxDisplayType"/>.
        /// </param>
        /// <returns></returns>
        bool IsShoppingCartRoundingEnabled(Currency currency = null, TaxDisplayType? taxDisplayType = null);

        /// <summary>
        /// Rounds <paramref name="amount"/> if rounding during shopping cart calculation is enabled.
        /// </summary>
        /// <param name="amount">Amount to round.</param>
        /// <param name="currency">
        /// Rounds <paramref name="amount"/> using <see cref="Currency.RoundNumDecimals"/> and <see cref="Currency.MidpointRounding"/>.
        /// If <c>null</c>, currency will be obtained via <see cref="IWorkContext.WorkingCurrency"/>.
        /// </param>
        /// <param name="taxDisplayType">
        /// Tax display type. If <c>null</c>, type will be obtained via <see cref="IWorkContext.TaxDisplayType"/>.
        /// </param>
        /// <returns>Rounded amount if rounding is enabled for <paramref name="currency"/>, <paramref name="amount"/> otherwise.</returns>
        decimal RoundIfEnabledFor(decimal amount, Currency currency = null, TaxDisplayType? taxDisplayType = null);

        /// <summary>
        /// Round amount up or down to the nearest multiple of denomination (cash rounding) if activated for currency.
        /// </summary>
        /// <param name="amount">Amount to round.</param>
        /// <param name="toNearestRounding">Amount by which was rounded.</param>
        /// <param name="currency">
        /// Currency. <see cref="Currency.RoundOrderTotalEnabled"/> must be activated for this currency.
        /// If <c>null</c>, currency will be obtained via <see cref="IWorkContext.WorkingCurrency"/>.
        /// </param>
        /// <returns>Rounded amount.</returns>
        /// <example>"Schweizer Rappenrundung" of 16.23 -> returned value is 16.25 and toNearestRounding is 0.02.</example>
        /// <remarks>Usually this method is used to round the order total.</remarks>
        decimal ToNearest(decimal amount, out decimal toNearestRounding, Currency currency = null);
    }
}
