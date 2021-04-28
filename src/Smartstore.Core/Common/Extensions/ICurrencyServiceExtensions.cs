using System;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Localization;

namespace Smartstore
{
    public static class ICurrencyServiceExtensions
    {
        #region Conversion

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

            Guard.NotNull(amount.Currency, nameof(amount.Currency));
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
            Guard.NotNull(toCurrency, nameof(toCurrency));
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

            Guard.NotNull(amount.Currency, nameof(amount.Currency));
            return amount.ExchangeTo(service.PrimaryExchangeCurrency);
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

            Guard.NotNull(amount.Currency, nameof(amount.Currency));
            Guard.NotNull(targetCurrency, nameof(targetCurrency));

            return amount.ExchangeTo(targetCurrency, service.PrimaryExchangeCurrency);
        }

        #endregion

        #region Tax

        /// <summary>
        ///     Applies a tax formatting pattern to given <c>product</c> money <paramref name="source"/>,
        ///     e.g. "{0} *", "{0} incl. tax"
        /// </summary>
        /// <param name="source">
        ///     The source <see cref="Money"/> to apply formatting to.
        /// </param>
        /// <param name="displayTaxSuffix">
        ///     A value indicating whether to display the tax suffix.
        ///     If <c>null</c>, current setting will be obtained via <see cref="TaxSettings.DisplayTaxSuffix"/> and
        ///     additionally via <see cref="TaxSettings.ShippingPriceIncludesTax"/> or <see cref="TaxSettings.PaymentMethodAdditionalFeeIncludesTax"/>
        ///     according to <paramref name="target"/>.
        /// </param>
        /// <param name="priceIncludesTax">
        ///     A value indicating whether given price includes tax already.
        ///     If <c>null</c>, current setting will be obtained via <see cref="IWorkContext.TaxDisplayType"/>.
        /// </param>
        /// <param name="target">
        ///     The target object to format price for. This parameter affects how <paramref name="displayTax"/>
        ///     will be auto-resolved if it is <c>null</c>.
        /// </param>
        /// <param name="language">
        ///     Language for tax suffix. If <c>null</c>, language will be obtained via <see cref="IWorkContext.WorkingLanguage"/>.
        /// </param>
        /// <returns>Money</returns>
        public static Money ApplyTaxFormat(this ICurrencyService currencyService,
            Money source,
            bool? displayTaxSuffix = null,
            bool? priceIncludesTax = null,
            Language language = null)
        {
            if (source == 0)
                return source;

            var format = currencyService.GetTaxFormat(displayTaxSuffix, priceIncludesTax, PricingTarget.Product, language);
            return source.WithPostFormat(format);
        }

        /// <summary>
        ///     Applies a tax formatting pattern to given <c>shipping charge</c> money <paramref name="source"/>,
        ///     e.g. "{0} *", "{0} incl. tax"
        /// </summary>
        /// <param name="source">
        ///     The source <see cref="Money"/> to apply formatting to.
        /// </param>
        /// <param name="displayTaxSuffix">
        ///     A value indicating whether to display the tax suffix.
        ///     If <c>null</c>, current setting will be obtained via <see cref="TaxSettings.DisplayTaxSuffix"/> and
        ///     additionally via <see cref="TaxSettings.ShippingPriceIncludesTax"/> or <see cref="TaxSettings.PaymentMethodAdditionalFeeIncludesTax"/>
        ///     according to <paramref name="target"/>.
        /// </param>
        /// <param name="priceIncludesTax">
        ///     A value indicating whether given price includes tax already.
        ///     If <c>null</c>, current setting will be obtained via <see cref="IWorkContext.TaxDisplayType"/>.
        /// </param>
        /// <param name="target">
        ///     The target object to format price for. This parameter affects how <paramref name="displayTax"/>
        ///     will be auto-resolved if it is <c>null</c>.
        /// </param>
        /// <param name="language">
        ///     Language for tax suffix. If <c>null</c>, language will be obtained via <see cref="IWorkContext.WorkingLanguage"/>.
        /// </param>
        /// <returns>Money</returns>
        public static Money ApplyShippingChargeTaxFormat(this ICurrencyService currencyService,
            Money source,
            bool? displayTaxSuffix = null,
            bool? priceIncludesTax = null,
            Language language = null)
        {
            if (source == 0)
                return source;

            var format = currencyService.GetTaxFormat(displayTaxSuffix, priceIncludesTax, PricingTarget.ShippingCharge, language);
            return source.WithPostFormat(format);
        }

        /// <summary>
        ///     Applies a tax formatting pattern to given <c>payment fee</c> money <paramref name="source"/>,
        ///     e.g. "{0} *", "{0} incl. tax"
        /// </summary>
        /// <param name="source">
        ///     The source <see cref="Money"/> to apply formatting to.
        /// </param>
        /// <param name="displayTaxSuffix">
        ///     A value indicating whether to display the tax suffix.
        ///     If <c>null</c>, current setting will be obtained via <see cref="TaxSettings.DisplayTaxSuffix"/> and
        ///     additionally via <see cref="TaxSettings.ShippingPriceIncludesTax"/> or <see cref="TaxSettings.PaymentMethodAdditionalFeeIncludesTax"/>
        ///     according to <paramref name="target"/>.
        /// </param>
        /// <param name="priceIncludesTax">
        ///     A value indicating whether given price includes tax already.
        ///     If <c>null</c>, current setting will be obtained via <see cref="IWorkContext.TaxDisplayType"/>.
        /// </param>
        /// <param name="target">
        ///     The target object to format price for. This parameter affects how <paramref name="displayTax"/>
        ///     will be auto-resolved if it is <c>null</c>.
        /// </param>
        /// <param name="language">
        ///     Language for tax suffix. If <c>null</c>, language will be obtained via <see cref="IWorkContext.WorkingLanguage"/>.
        /// </param>
        /// <returns>Money</returns>
        public static Money ApplyPaymentFeeTaxFormat(this ICurrencyService currencyService,
            Money source,
            bool? displayTaxSuffix = null,
            bool? priceIncludesTax = null,
            Language language = null)
        {
            if (source == 0)
                return source;

            var format = currencyService.GetTaxFormat(displayTaxSuffix, priceIncludesTax, PricingTarget.PaymentFee, language);
            return source.WithPostFormat(format);
        }

        #endregion
    }
}
