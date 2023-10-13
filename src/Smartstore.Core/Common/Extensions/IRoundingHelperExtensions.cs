using Smartstore.Core;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;

namespace Smartstore
{
    public static partial class IRoundingHelperExtensions
    {
        /// <summary>
        /// Rounds <see cref="Money.Amount"/> of <paramref name="amount"/> by using its <see cref="Money.Currency"/>.
        /// </summary>
        /// <param name="amount">Amount to round.</param>
        /// <returns>Rounded amount.</returns>
        public static decimal Round(this IRoundingHelper helper, Money amount)
        {
            Guard.NotNull(helper);

            return helper.Round(amount.Amount, amount.Currency);
        }

        /// <summary>
        /// Rounds <see cref="Money.Amount"/> of <paramref name="amount"/> to the smallest currency unit (e.g. cents) by using its <see cref="Money.Currency"/>.
        /// </summary>
        /// <param name="amount">Amount to round.</param>
        /// <returns>Rounded amount.</returns>
        public static int ToSmallestCurrencyUnit(this IRoundingHelper helper, Money amount)
        {
            Guard.NotNull(helper);

            return helper.ToSmallestCurrencyUnit(amount.Amount, amount.Currency);
        }

        /// <summary>
        /// Rounds <paramref name="amount"/> to the smallest currency unit (e.g. cents).
        /// </summary>
        /// <param name="amount">Amount to round.</param>
        /// <param name="currency">
        /// Rounds <paramref name="amount"/> using <see cref="Currency.RoundNumDecimals"/> and <see cref="Currency.MidpointRounding"/>.
        /// Cannot be <c>null</c>.
        /// </param>
        /// <returns>Rounded amount.</returns>
        public static int ToSmallestCurrencyUnit(this IRoundingHelper helper, decimal amount, Currency currency)
        {
            Guard.NotNull(helper);
            Guard.NotNull(currency);

            var factor = (int)Math.Pow(10, currency.RoundNumDecimals);

            return Convert.ToInt32(helper.Round(amount * factor, 0, currency.MidpointRounding));
        }

        /// <summary>
        /// Round value up or down to the nearest multiple of denomination (cash rounding) if activated for currency.
        /// </summary>
        /// <param name="amount">Amount to round.</param>
        /// <param name="currency">
        /// Currency. <see cref="Currency.RoundOrderTotalEnabled"/> must be activated for this currency.
        /// If <c>null</c>, currency will be obtained via <see cref="IWorkContext.WorkingCurrency"/>.
        /// </param>
        /// <param name="toNearestRounding">Amount by which was rounded.</param>
        /// <returns>Rounded amount.</returns>
        /// <example>"Schweizer Rappenrundung" of 16.23 -> returned value is 16.25 and toNearestRounding is 0.02.</example>
        /// <remarks>Usually this method is used to round the order total.</remarks>
        public static Money ToNearest(this IRoundingHelper helper, Money amount, Currency currency, out Money toNearestRounding)
        {
            Guard.NotNull(helper);
            Guard.NotNull(currency);

            var roundedAmount = helper.ToNearest(amount.Amount, out var tmpToNearestRounding, currency);
            toNearestRounding = new(tmpToNearestRounding, currency);

            return new(roundedAmount, currency);
        }
    }
}
