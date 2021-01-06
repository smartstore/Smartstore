using Smartstore.Core.Common;
using Smartstore.Core.Localization;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Price formatter interface.
    /// </summary>
    public partial interface IPriceFormatter
    {
        /// <summary>
        /// Formats the price.
        /// </summary>
        /// <param name="price">Price.</param>
        /// <param name="displayCurrency">
        /// A value indicating whether to display the currency symbol/code.
        /// </param>
        /// <param name="currencyCodeOrObj">
        /// Target currency as string code (e.g. USD) or an actual <see cref="Currency"/> instance. If <c>null</c>,
        /// currency will be obtained via <see cref="IWorkContext.WorkingCurrency"/>.
        /// </param>
        /// <param name="language">
        /// Language for tax suffix. If <c>null</c>, language will be obtained via <see cref="IWorkContext.WorkingLanguage"/>.
        /// </param>
        /// <param name="priceIncludesTax">
        /// A value indicating whether given price includes tax already.
        /// If <c>null</c>, current setting will be obtained via <see cref="IWorkContext.TaxDisplayType"/>.
        /// </param>
        /// <param name="displayTax">
        /// A value indicating whether to display the tax suffix.
        /// If <c>null</c>, current setting will be obtained via <see cref="TaxSettings.DisplayTaxSuffix"/> and
        /// additionally via <see cref="TaxSettings.ShippingPriceIncludesTax"/> or <see cref="TaxSettings.PaymentMethodAdditionalFeeIncludesTax"/>
        /// according to <paramref name="target"/>.
        /// </param>
        /// <param name="target">
        /// The target object to format price for. This parameter affects how <paramref name="displayTax"/>
        /// will be auto-resolved if it is <c>null</c>.
        /// </param>
        /// <returns>Formatted price.</returns>
        string FormatPrice(
            decimal price,
            bool displayCurrency = true,
            object currencyCodeOrObj = null,
            Language language = null,
            bool? priceIncludesTax = null,
            bool? displayTax = null,
            PricingTarget target = PricingTarget.Product);

        /// <summary>
        /// Gets the base price info for a product.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <param name="productPrice">The calculated product price.</param>
        /// <param name="currency">Target currency.</param>
        /// <returns>The base price info.</returns>
        string GetBasePriceInfo(Product product, decimal productPrice, Currency currency);

        /// <summary>
        /// Formats a tax rate.
        /// </summary>
        /// <param name="taxRate">Tax rate to format.</param>
        /// <returns>Formatted tax rate.</returns>
        string FormatTaxRate(decimal taxRate);
    }
}