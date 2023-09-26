using System.Globalization;
using System.Runtime.CompilerServices;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Configuration;

namespace Smartstore.Core.Common.Services
{
    // TODO: (mg) lots of refactoring required. Now only RoundingHelper should round currency values (otherwise rounding difference possible).
    // TODO: (mg) refactor decimal numeric extension methods.
    public partial class RoundingHelper : IRoundingHelper
    {
        private readonly IWorkContext _workContext;
        private readonly CurrencySettings _currencySettings;

        public RoundingHelper(IWorkContext workContext, CurrencySettings currencySettings)
        {
            _workContext = workContext;
            _currencySettings = currencySettings;
        }

        public virtual decimal Round(decimal amount, Currency currency = null)
        {
            currency ??= _workContext.WorkingCurrency;

            if (amount == decimal.Zero ||
                !(currency.RoundOrderItemsEnabled ?? _currencySettings.RoundOrderItemsEnabled) ||
                (_workContext.TaxDisplayType == TaxDisplayType.ExcludingTax && !(currency.RoundNetPrices ?? _currencySettings.RoundNetPrices)))
            {
                return amount;
            }

            return decimal.Round(amount, currency.RoundNumDecimals, _currencySettings.MidpointRounding);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual int ToSmallestCurrencyUnit(decimal amount)
        {
            return Convert.ToInt32(decimal.Round(amount * 100, 0, _currencySettings.MidpointRounding));
        }

        public virtual decimal ToNearest(decimal amount, out decimal toNearestRounding, Currency currency = null)
        {
            currency ??= _workContext.WorkingCurrency;

            var oldValue = amount;

            switch (currency.RoundOrderTotalRule)
            {
                case CurrencyRoundingRule.RoundMidpointUp:
                    amount = ToNearest(amount, currency.RoundOrderTotalDenominator, MidpointRounding.AwayFromZero);
                    break;
                case CurrencyRoundingRule.AlwaysRoundDown:
                    amount = ToNearest(amount, currency.RoundOrderTotalDenominator, false);
                    break;
                case CurrencyRoundingRule.AlwaysRoundUp:
                    amount = ToNearest(amount, currency.RoundOrderTotalDenominator, true);
                    break;
                case CurrencyRoundingRule.RoundMidpointDown:
                default:
                    amount = ToNearest(amount, currency.RoundOrderTotalDenominator, MidpointRounding.ToEven);
                    break;
            }

            toNearestRounding = amount - decimal.Round(oldValue, currency.RoundNumDecimals, _currencySettings.MidpointRounding);

            return amount;
        }

        protected virtual decimal ToNearest(decimal amount, decimal denomination, MidpointRounding midpoint)
        {
            if (denomination == decimal.Zero)
            {
                return amount;
            }

            return decimal.Round(amount / denomination, midpoint) * denomination;
        }

        protected virtual decimal ToNearest(decimal amount, decimal denomination, bool roundUp)
        {
            if (denomination == decimal.Zero)
            {
                return amount;
            }

            var roundedValueBase = roundUp
                ? decimal.Ceiling(amount / denomination)
                : decimal.Floor(amount / denomination);

            return decimal.Round(roundedValueBase, _currencySettings.MidpointRounding) * denomination;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual string ToString(decimal amount, int decimals = 2)
        {
            return decimal.Round(amount, decimals, _currencySettings.MidpointRounding).ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
