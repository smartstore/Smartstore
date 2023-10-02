using System.Runtime.CompilerServices;
using Smartstore.Core.Checkout.Tax;
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

        public virtual decimal Round(decimal amount, Currency currency = null)
        {
            currency ??= _workContext.WorkingCurrency;
            return Round(amount, currency.RoundNumDecimals, currency.MidpointRounding);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual decimal Round(decimal amount, int decimals, MidpointRounding midpointRounding = MidpointRounding.ToEven)
        {
            return decimal.Round(amount, decimals, midpointRounding);
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

            return Round(amount, currency.RoundNumDecimals, currency.MidpointRounding);
        }

        public virtual decimal ToNearest(decimal amount, out decimal toNearestRounding, Currency currency = null)
        {
            currency ??= _workContext.WorkingCurrency;

            var oldValue = amount;

            switch (currency.RoundOrderTotalRule)
            {
                case CurrencyRoundingRule.RoundMidpointUp:
                    amount = ToNearest(amount, currency, MidpointRounding.AwayFromZero, null);
                    break;
                case CurrencyRoundingRule.AlwaysRoundDown:
                    amount = ToNearest(amount, currency, null, false);
                    break;
                case CurrencyRoundingRule.AlwaysRoundUp:
                    amount = ToNearest(amount, currency, null, true);
                    break;
                case CurrencyRoundingRule.RoundMidpointDown:
                default:
                    amount = ToNearest(amount, currency, MidpointRounding.ToEven, null);
                    break;
            }

            toNearestRounding = amount - Round(oldValue, currency.RoundNumDecimals, currency.MidpointRounding);

            return amount;
        }

        protected virtual decimal ToNearest(decimal amount, Currency currency, MidpointRounding? midpointRounding, bool? roundUp)
        {
            if (currency.RoundOrderTotalDenominator != decimal.Zero)
            {
                if (midpointRounding.HasValue)
                {
                    return decimal.Round(amount / currency.RoundOrderTotalDenominator, 0, midpointRounding.Value) * currency.RoundOrderTotalDenominator;
                }
                else if (roundUp.HasValue)
                {
                    var roundedAmountBase = roundUp.Value
                        ? decimal.Ceiling(amount / currency.RoundOrderTotalDenominator)
                        : decimal.Floor(amount / currency.RoundOrderTotalDenominator);

                    return Round(roundedAmountBase, 0, currency.MidpointRounding) * currency.RoundOrderTotalDenominator;
                }
            }

            return amount;
        }
    }
}
