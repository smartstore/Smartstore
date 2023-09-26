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

        public virtual decimal RoundIfEnabledFor(decimal amount, Currency currency = null, TaxDisplayType? taxDisplayType = null)
        {
            currency ??= _workContext.WorkingCurrency;
            taxDisplayType ??= _workContext.TaxDisplayType;

            if (amount == decimal.Zero || 
                !(currency.RoundOrderItemsEnabled ?? _currencySettings.RoundOrderItemsEnabled) ||
                (taxDisplayType == TaxDisplayType.ExcludingTax && !(currency.RoundNetPrices ?? _currencySettings.RoundNetPrices)))
            {
                return amount;
            }

            return Round(amount, currency.RoundNumDecimals);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual decimal Round(decimal amount, int decimals = 2)
        {
            return decimal.Round(amount, decimals, _currencySettings.MidpointRounding);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual int ToSmallestCurrencyUnit(decimal amount)
        {
            return Convert.ToInt32(Round(amount * 100, 0));
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

            toNearestRounding = amount - Round(oldValue, currency.RoundNumDecimals);

            return amount;
        }

        protected virtual decimal ToNearest(decimal amount, decimal denomination, MidpointRounding midpoint)
        {
            if (denomination == decimal.Zero)
            {
                return amount;
            }

            return decimal.Round(amount / denomination, 0, midpoint) * denomination;
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

            return Round(roundedValueBase, 0) * denomination;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual string ToString(decimal amount, int decimals = 2)
        {
            return Round(amount, decimals).ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
