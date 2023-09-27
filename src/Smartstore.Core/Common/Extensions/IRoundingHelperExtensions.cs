using System.Globalization;
using Smartstore.Core;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;

namespace Smartstore
{
    public static partial class IRoundingHelperExtensions
    {
        /// <summary>
        /// Rounds <paramref name="amount"/>.
        /// </summary>
        /// <param name="amount">Amount to round.</param>
        /// <param name="currency">Currency whose properties are to be used for rounding. Cannot be <c>null</c>.</param>
        /// <returns>Rounded amount.</returns>
        public static decimal Round(this IRoundingHelper helper, decimal amount, Currency currency)
        {
            Guard.NotNull(helper);
            Guard.NotNull(currency);

            return helper.Round(amount, currency.RoundNumDecimals, currency.MidpointRounding);
        }

        /// <summary>
        /// Rounds <paramref name="amount"/> to the smallest currency unit, e.g. cents.
        /// </summary>
        /// <param name="amount">Amount to round.</param>
        /// <param name="currency">Currency whose properties are to be used for rounding. Cannot be <c>null</c>.</param>
        /// <returns>Rounded amount.</returns>
        public static int ToSmallestCurrencyUnit(this IRoundingHelper helper, decimal amount, Currency currency)
        {
            Guard.NotNull(helper);
            Guard.NotNull(currency);

            return Convert.ToInt32(helper.Round(amount * 100, 0, currency.MidpointRounding));
        }

        /// <summary>
        /// Creates the culture invariant string representation of the rounded <paramref name="amount"/>.
        /// </summary>
        /// <param name="amount">Amount to round.</param>
        /// <param name="currency">
        /// Rounds <paramref name="amount"/> using <see cref="Currency.RoundNumDecimals"/> and <see cref="Currency.MidpointRounding"/>.
        /// If <c>null</c>, currency will be obtained via <see cref="IWorkContext.WorkingCurrency"/>.
        /// </param>
        /// <returns>The formatted rounded amount.</returns>
        public static string ToString(this IRoundingHelper helper, decimal amount, Currency currency)
        {
            Guard.NotNull(helper);
            Guard.NotNull(currency);

            return helper.Round(amount, currency.RoundNumDecimals, currency.MidpointRounding).ToString("0.00", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Round value up or down to the nearest multiple of denomination (cash rounding) if activated for currency.
        /// </summary>
        /// <param name="amount">Amount to round.</param>
        /// <param name="currency">Currency whose properties are to be used for rounding. Cannot be <c>null</c>.</param>
        /// <param name="toNearestRounding">Amount by which was rounded.</param>
        /// <returns>Rounded amount.</returns>
        /// <example>"Schweizer Rappenrundung" of 16.23 -> returned value is 16.25 and toNearestRounding is 0.02.</example>
        /// <remarks>Usually this method is used to round the order total.</remarks>
        public static Money RoundToNearest(this IRoundingHelper helper, Money amount, Currency currency, out Money toNearestRounding)
        {
            Guard.NotNull(helper);
            Guard.NotNull(currency);

            var roundedAmount = helper.ToNearest(amount.Amount, out var tmpToNearestRounding, currency);
            toNearestRounding = new(tmpToNearestRounding, currency);

            return new(roundedAmount, currency);
        }
    }
}
