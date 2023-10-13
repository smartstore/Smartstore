using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;

namespace Smartstore
{
    public static partial class ICurrencyServiceExtensions
    {
        #region Currency conversion

        /// <summary>
        /// Exchanges given <see cref="Money"/> amount to <see cref="ICurrencyService.PrimaryCurrency"/>.
        /// </summary>
        /// <param name="amount">The source amount to exchange</param>
        /// <returns>The exchanged amount.</returns>
        public static Money ConvertToPrimaryCurrency(this ICurrencyService service, Money amount)
        {
            if (amount.Currency == service.PrimaryCurrency)
            {
                // Perf
                return amount;
            }

            Guard.NotNull(amount.Currency);
            return amount.ExchangeTo(service.PrimaryCurrency, service.PrimaryExchangeCurrency);
        }

        /// <summary>
        /// Exchanges given money amount (which is assumed to be in <see cref="ICurrencyService.PrimaryCurrency"/>) to <paramref name="toCurrency"/>,
        /// using <see cref="ICurrencyService.PrimaryExchangeCurrency"/> as exchange rate currency.
        /// </summary>
        /// <param name="amount">The source amount to exchange (should be in <see cref="ICurrencyService.PrimaryCurrency"/>).</param>
        /// <returns>The exchanged amount in <paramref name="toCurrency"/>.</returns>
        public static Money ConvertFromPrimaryCurrency(this ICurrencyService service, decimal amount, Currency toCurrency)
        {
            Guard.NotNull(toCurrency);
            return new Money(amount, service.PrimaryCurrency).ExchangeTo(toCurrency, service.PrimaryExchangeCurrency);
        }

        /// <summary>
        /// Exchanges given <see cref="Money"/> amount to <see cref="ICurrencyService.PrimaryExchangeCurrency"/>.
        /// </summary>
        /// <param name="amount">The source amount to exchange</param>
        /// <returns>The exchanged amount.</returns>
        public static Money ConvertToExchangeRateCurrency(this ICurrencyService service, Money amount)
        {
            if (amount.Currency == service.PrimaryExchangeCurrency)
            {
                // Perf
                return amount;
            }

            Guard.NotNull(amount.Currency);
            return amount.ExchangeTo(service.PrimaryExchangeCurrency);
        }

        /// <summary>
        /// Exchanges given money amount (which is assumed to be in <see cref="ICurrencyService.PrimaryCurrency"/>) by using <paramref name="exchangeRate"/>.
        /// Typically used when converting money amounts of orders at the rate that was applied at the time the order was placed.
        /// </summary>
        /// <param name="amount">The source amount to exchange (should be in <see cref="ICurrencyService.PrimaryCurrency"/>).</param>
        /// <param name="exchangeRate">The currency exchange rate, e.g. <see cref="Order.CurrencyRate"/>.</param>
        /// <param name="currencyCodeOrObj">Target currency as string code (e.g. <see cref="Order.CustomerCurrencyCode"/>) or an actual <see cref="Currency"/> instance.</param>
        /// <param name="displayCurrency">A value indicating whether to display the currency symbol/code.</param>
        /// <returns>The exchanged amount.</returns>
        public static Money ConvertToExchangeRate(this ICurrencyService service,
            decimal amount,
            decimal exchangeRate,
            object currencyCodeOrObj = null,
            bool displayCurrency = true)
        {
            return service.CreateMoney(amount, currencyCodeOrObj, displayCurrency, false).Exchange(exchangeRate);
        }

        /// <summary>
        /// Exchanges given <see cref="Money"/> amount to <paramref name="targetCurrency"/>,
        /// using <see cref="ICurrencyService.PrimaryExchangeCurrency"/> as exchange rate currency.
        /// </summary>
        /// <param name="amount">The source amount to exchange.</param>
        /// <param name="targetCurrency">The target currency to exchange amount to.</param>
        /// <returns>The exchanged amount.</returns>
        public static Money ConvertToCurrency(this ICurrencyService service, Money amount, Currency targetCurrency)
        {
            if (amount.Currency == targetCurrency)
            {
                // Perf
                return amount;
            }

            Guard.NotNull(amount.Currency);
            Guard.NotNull(targetCurrency);

            return amount.ExchangeTo(targetCurrency, service.PrimaryExchangeCurrency);
        }

        #endregion
    }
}
