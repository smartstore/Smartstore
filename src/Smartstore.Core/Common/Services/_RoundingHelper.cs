using System.Runtime.CompilerServices;
using Smartstore.Core.Common.Configuration;

namespace Smartstore.Core.Common.Services
{
    public partial class RoundingHelper : IRoundingHelper
    {
        private readonly IWorkContext _workContext;
        private readonly CurrencySettings _currencySettings;

        public RoundingHelper(IWorkContext workContext, CurrencySettings currencySettings)
        {
            _workContext = workContext;
            _currencySettings = currencySettings;
        }

        // TODO: (mg) lots of refactoring required. Now only RoundingHelper should round currency values (otherwise rounding difference possible).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual decimal Round(decimal amount, Currency currency = null)
        {
            currency ??= _workContext.WorkingCurrency;

            return decimal.Round(amount, currency.RoundNumDecimals, _currencySettings.MidpointRounding);
        }

        public virtual decimal Round(decimal amount, CartRoundingItem item, bool isTax, Currency currency = null)
        {
            currency ??= _workContext.WorkingCurrency;

            switch (currency.RoundCartRule ?? _currencySettings.RoundCartRule)
            {
                case CartRoundingRule.NeverRound:
                default:
                    return amount;

                case CartRoundingRule.AlwaysRound:
                    return Round(amount, currency);

                // TODO: (mg) new options...
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
