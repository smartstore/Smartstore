using System.Runtime.CompilerServices;
using Smartstore.Core.Common.Configuration;

namespace Smartstore.Core.Common.Services
{
    // TODO: (mg) check where extension methods with Money parameter are required. See IRoundingHelperExtensions.
    public partial class RoundingHelper : IRoundingHelper
    {
        private readonly IWorkContext _workContext;
        private readonly CurrencySettings _currencySettings;

        public RoundingHelper(IWorkContext workContext, CurrencySettings currencySettings)
        {
            _workContext = workContext;
            _currencySettings = currencySettings;
        }

        // TODO: (mg) only RoundingHelper should round currency values! Otherwise rounding difference possible. Rounding in structs like Money or Tax is buried too deep.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual decimal Round(decimal amount, Currency currency = null)
        {
            currency ??= _workContext.WorkingCurrency;

            return decimal.Round(amount, currency.RoundNumDecimals, _currencySettings.MidpointRounding);
        }

        public virtual decimal Round(decimal amount, CartRoundingItem item, Currency currency = null)
        {
            currency ??= _workContext.WorkingCurrency;

            switch (currency.RoundCartRule ?? _currencySettings.RoundCartRule)
            {
                // TODO: (mg) new options...

                case CartRoundingRule.AlwaysRound:
                    return Round(amount, currency);

                case CartRoundingRule.NeverRound:
                default:
                    return amount;
            }
        }

        public virtual decimal RoundToNearest(decimal amount, out decimal toNearestRounding, Currency currency = null)
        {
            currency ??= _workContext.WorkingCurrency;

            var oldValue = amount;

            switch (currency.RoundOrderTotalRule)
            {
                case CurrencyRoundingRule.RoundMidpointUp:
                    amount = amount.RoundToNearest(currency.RoundOrderTotalDenominator, MidpointRounding.AwayFromZero);
                    break;
                case CurrencyRoundingRule.AlwaysRoundDown:
                    amount = amount.RoundToNearest(currency.RoundOrderTotalDenominator, false);
                    break;
                case CurrencyRoundingRule.AlwaysRoundUp:
                    amount = amount.RoundToNearest(currency.RoundOrderTotalDenominator, true);
                    break;
                case CurrencyRoundingRule.RoundMidpointDown:
                default:
                    amount = amount.RoundToNearest(currency.RoundOrderTotalDenominator, MidpointRounding.ToEven);
                    break;
            }

            toNearestRounding = amount - decimal.Round(oldValue, currency.RoundNumDecimals);

            return amount;
        }
    }
}
