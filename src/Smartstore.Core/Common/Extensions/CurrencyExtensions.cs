using Smartstore.Core.Common;

namespace Smartstore
{
    public static class CurrencyExtensions
    {
        /// <summary>
        /// Round value up or down to the nearest multiple of denomination (cash rounding) if activated for currency.
        /// </summary>
        /// <param name="currency">Currency. Rounding must be activated for this currency.</param>
        /// <param name="value">Value to round.</param>
        /// <param name="toNearestRounding">Amount by which was rounded.</param>
        /// <returns>Rounded amount.</returns>
        /// <example>"Schweizer Rappenrundung" of 16.23 -> returned value is 16.25 and toNearestRounding is 0.02.</example>
        public static Money RoundToNearest(this Currency currency, Money value, out Money toNearestRounding)
        {
            Guard.NotNull(currency, nameof(currency));

            var newValue = currency.RoundToNearest(value.Amount, out var tmpToNearestRounding);

            toNearestRounding = new Money(tmpToNearestRounding, currency);

            return new Money(newValue, currency);
        }

        /// <summary>
        /// Round value up or down to the nearest multiple of denomination (cash rounding) if activated for currency.
        /// </summary>
        /// <param name="value">Value to round.</param>
        /// <param name="currency">Currency. Rounding must be activated for this currency.</param>
        /// <param name="toNearestRounding">Amount by which was rounded.</param>
        /// <returns>Rounded value</returns>
        /// <example>"Schweizer Rappenrundung" of 16.23 -> returned value is 16.25 and toNearestRounding is 0.02.</example>
        public static decimal RoundToNearest(this Currency currency, decimal value, out decimal toNearestRounding)
        {
            Guard.NotNull(currency, nameof(currency));

            var oldValue = value;

            switch (currency.RoundOrderTotalRule)
            {
                case CurrencyRoundingRule.RoundMidpointUp:
                    value = value.RoundToNearest(currency.RoundOrderTotalDenominator, MidpointRounding.AwayFromZero);
                    break;
                case CurrencyRoundingRule.AlwaysRoundDown:
                    value = value.RoundToNearest(currency.RoundOrderTotalDenominator, false);
                    break;
                case CurrencyRoundingRule.AlwaysRoundUp:
                    value = value.RoundToNearest(currency.RoundOrderTotalDenominator, true);
                    break;
                case CurrencyRoundingRule.RoundMidpointDown:
                default:
                    value = value.RoundToNearest(currency.RoundOrderTotalDenominator, MidpointRounding.ToEven);
                    break;
            }

            toNearestRounding = value - decimal.Round(oldValue, currency.RoundNumDecimals);

            return value;
        }

        /// <summary>
        /// Rounds a value if rounding is enabled for the currency.
        /// </summary>
        /// <param name="value">Value to round.</param>
        /// <param name="currency">Currency</param>
        /// <returns>Rounded value.</returns>
        public static decimal RoundIfEnabledFor(this Currency currency, decimal value)
        {
            Guard.NotNull(currency, nameof(currency));

            if (currency.RoundOrderItemsEnabled)
            {
                return Math.Round(value, currency.RoundNumDecimals);
            }

            return value;
        }

        /// <summary>
        /// Checks if a currency was configured for the domain ending.
        /// </summary>
        /// <param name="domain">Domain to check.</param>
        public static bool HasDomainEnding(this Currency currency, string domain)
        {
            if (currency == null || domain.IsEmpty() || currency.DomainEndings.IsEmpty())
                return false;

            var endings = currency.DomainEndings.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            return endings.Any(x => domain.EndsWith(x, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Returns the currency configured for the domain ending.
        /// </summary>
        /// <param name="domain">Domain to check.</param>
        public static Currency GetByDomainEnding(this IEnumerable<Currency> currencies, string domain)
        {
            if (currencies == null || domain.IsEmpty())
                return null;

            return currencies.FirstOrDefault(x => x.Published && x.HasDomainEnding(domain));
        }
    }
}
