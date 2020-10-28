using System;
using Smartstore.Core.Common;

namespace Smartstore
{
    public static class CurrencyExtensions
    {
        /// <summary>
        /// Round decimal up or down to the nearest multiple of denomination if activated for currency
        /// </summary>
        /// <param name="value">Value to round</param>
        /// <param name="currency">Currency. Rounding must be activated for this currency.</param>
        /// <param name="roundingAmount">The rounding amount</param>
        /// <returns>Rounded value</returns>
        public static decimal RoundToNearest(this Currency currency, decimal value, out decimal roundingAmount)
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

            roundingAmount = value - Math.Round(oldValue, 2);

            return value;
        }

        /// <summary>
        /// Rounds a value if rounding is enabled for the currency
        /// </summary>
        /// <param name="value">Value to round</param>
        /// <param name="currency">Currency</param>
        /// <returns>Rounded value</returns>
        public static decimal RoundIfEnabledFor(this Currency currency, decimal value)
        {
            Guard.NotNull(currency, nameof(currency));

            if (currency.RoundOrderItemsEnabled)
            {
                return Math.Round(value, currency.RoundNumDecimals);
            }

            return value;
        }
    }
}
