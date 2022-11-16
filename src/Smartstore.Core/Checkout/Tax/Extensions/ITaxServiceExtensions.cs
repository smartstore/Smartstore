using Smartstore.Core;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Localization;

namespace Smartstore
{
    public static class ITaxServiceExtensions
    {
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
        ///     additionally via <see cref="TaxSettings.ShippingPriceIncludesTax"/> or <see cref="TaxSettings.PaymentMethodAdditionalFeeIncludesTax"/>.
        /// </param>
        /// <param name="priceIncludesTax">
        ///     A value indicating whether given price includes tax already.
        ///     If <c>null</c>, current setting will be obtained via <see cref="IWorkContext.TaxDisplayType"/>.
        /// </param>
        /// <param name="language">
        ///     Language for tax suffix. If <c>null</c>, language will be obtained via <see cref="IWorkContext.WorkingLanguage"/>.
        /// </param>
        /// <returns>Money</returns>
        public static Money ApplyTaxFormat(this ITaxService taxService,
            Money source,
            bool? displayTaxSuffix = null,
            bool? priceIncludesTax = null,
            Language language = null)
        {
            if (source == 0)
                return source;

            var format = taxService.GetTaxFormat(displayTaxSuffix, priceIncludesTax, PricingTarget.Product, language);
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
        ///     additionally via <see cref="TaxSettings.ShippingPriceIncludesTax"/> or <see cref="TaxSettings.PaymentMethodAdditionalFeeIncludesTax"/>.
        /// </param>
        /// <param name="priceIncludesTax">
        ///     A value indicating whether given price includes tax already.
        ///     If <c>null</c>, current setting will be obtained via <see cref="IWorkContext.TaxDisplayType"/>.
        /// </param>
        /// <param name="language">
        ///     Language for tax suffix. If <c>null</c>, language will be obtained via <see cref="IWorkContext.WorkingLanguage"/>.
        /// </param>
        /// <returns>Money</returns>
        public static Money ApplyShippingChargeTaxFormat(this ITaxService taxService,
            Money source,
            bool? displayTaxSuffix = null,
            bool? priceIncludesTax = null,
            Language language = null)
        {
            if (source == 0)
                return source;

            var format = taxService.GetTaxFormat(displayTaxSuffix, priceIncludesTax, PricingTarget.ShippingCharge, language);
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
        ///     additionally via <see cref="TaxSettings.ShippingPriceIncludesTax"/> or <see cref="TaxSettings.PaymentMethodAdditionalFeeIncludesTax"/>.
        /// </param>
        /// <param name="priceIncludesTax">
        ///     A value indicating whether given price includes tax already.
        ///     If <c>null</c>, current setting will be obtained via <see cref="IWorkContext.TaxDisplayType"/>.
        /// </param>
        /// <param name="language">
        ///     Language for tax suffix. If <c>null</c>, language will be obtained via <see cref="IWorkContext.WorkingLanguage"/>.
        /// </param>
        /// <returns>Money</returns>
        public static Money ApplyPaymentFeeTaxFormat(this ITaxService taxService,
            Money source,
            bool? displayTaxSuffix = null,
            bool? priceIncludesTax = null,
            Language language = null)
        {
            if (source == 0)
                return source;

            var format = taxService.GetTaxFormat(displayTaxSuffix, priceIncludesTax, PricingTarget.PaymentFee, language);
            return source.WithPostFormat(format);
        }
    }
}
