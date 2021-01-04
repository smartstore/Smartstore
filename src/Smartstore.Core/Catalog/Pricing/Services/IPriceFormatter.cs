using System.Threading.Tasks;
using Smartstore.Core.Common;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Price formatter interface.
    /// </summary>
    public partial interface IPriceFormatter
    {
        /// <summary>
        /// Formats the price
        /// </summary>
        /// <param name="price">Price</param>
        /// <returns>Price</returns>
        Task<string> FormatPriceAsync(decimal price);

        /// <summary>
        /// Formats the price.
        /// </summary>
        /// <param name="price">Price.</param>
        /// <param name="showCurrency">A value indicating whether to show a currency.</param>
        /// <param name="targetCurrency">Target currency.</param>
        /// <returns>Formatted price.</returns>
        Task<string> FormatPriceAsync(decimal price, bool showCurrency, Currency targetCurrency);

        /// <summary>
        /// Formats the price.
        /// </summary>
        /// <param name="price">Price.</param>
        /// <param name="showCurrency">A value indicating whether to show a currency.</param>
        /// <param name="showTax">A value indicating whether to show tax suffix.</param>
        /// <returns>Formatted price.</returns>
        Task<string> FormatPriceAsync(decimal price, bool showCurrency, bool showTax);

        /// <summary>
        /// Formats the price.
        /// </summary>
        /// <param name="price">Price.</param>
        /// <param name="showCurrency">A value indicating whether to show a currency.</param>
        /// <param name="currencyCode">Currency code.</param>
        /// <param name="showTax">A value indicating whether to show tax suffix.</param>
        /// <param name="language">Language entity.</param>
        /// <returns>Formatted price.</returns>
        Task<string> FormatPriceAsync(decimal price, bool showCurrency, string currencyCode, bool showTax, Language language);

        /// <summary>
        /// Formats the price.
        /// </summary>
        /// <param name="price">Price.</param>
        /// <param name="showCurrency">A value indicating whether to show a currency.</param>
        /// <param name="currencyCode">Currency code.</param>
        /// <param name="language">Language entity.</param>
        /// <param name="priceIncludesTax">A value indicating whether price includes tax.</param>
        /// <returns>Formatted price.</returns>
        Task<string> FormatPriceAsync(decimal price, bool showCurrency, string currencyCode, Language language, bool priceIncludesTax);

        /// <summary>
        /// Formats the price.
        /// </summary>
        /// <param name="price">Price.</param>
        /// <param name="showCurrency">A value indicating whether to show a currency.</param>
        /// <param name="currencyCode">Currency code.</param>
        /// <param name="language">Language entity.</param>
        /// <param name="priceIncludesTax">A value indicating whether price includes tax.</param>
        /// <param name="showTax">A value indicating whether to show the tax suffix.</param>
        /// <returns>Formatted price.</returns>
        Task<string> FormatPriceAsync(decimal price, bool showCurrency, string currencyCode, Language language, bool priceIncludesTax, bool showTax);

        /// <summary>
        /// Formats the price.
        /// </summary>
        /// <param name="price">Price.</param>
        /// <param name="showCurrency">A value indicating whether to show a currency.</param>
        /// <param name="targetCurrency">Target currency.</param>
        /// <param name="language">Language entity.</param>
        /// <param name="priceIncludesTax">A value indicating whether price includes tax.</param>
        /// <returns>Formatted price.</returns>
        Task<string> FormatPriceAsync(decimal price, bool showCurrency, Currency targetCurrency, Language language, bool priceIncludesTax);

        /// <summary>
        /// Formats the price.
        /// </summary>
        /// <param name="price">Price.</param>
        /// <param name="showCurrency">A value indicating whether to show a currency.</param>
        /// <param name="targetCurrency">Target currency.</param>
        /// <param name="language">Language entity.</param>
        /// <param name="priceIncludesTax">A value indicating whether price includes tax.</param>
        /// <param name="showTax">A value indicating whether to show the tax suffix.</param>
        /// <returns>Formatted price.</returns>
        Task<string> FormatPriceAsync(decimal price, bool showCurrency, Currency targetCurrency, Language language, bool priceIncludesTax, bool showTax);


        /// <summary>
        /// Formats the shipping price.
        /// </summary>
        /// <param name="price">Price.</param>
        /// <param name="showCurrency">A value indicating whether to show a currency.</param>
        /// <returns>Formatted shipping price.</returns>
        Task<string> FormatShippingPriceAsync(decimal price, bool showCurrency);

        /// <summary>
        /// Formats the shipping price.
        /// </summary>
        /// <param name="price">Price.</param>
        /// <param name="showCurrency">A value indicating whether to show a currency.</param>
        /// <param name="targetCurrency">Target currency.</param>
        /// <param name="language">Language entity.</param>
        /// <param name="priceIncludesTax">A value indicating whether price includes tax.</param>
        /// <returns>Formatted shipping price.</returns>
        Task<string> FormatShippingPriceAsync(decimal price, bool showCurrency, Currency targetCurrency, Language language, bool priceIncludesTax);

        /// <summary>
        /// Formats the shipping price.
        /// </summary>
        /// <param name="price">Price.</param>
        /// <param name="showCurrency">A value indicating whether to show a currency.</param>
        /// <param name="currencyCode">Currency code.</param>
        /// <param name="language">Language entity.</param>
        /// <param name="priceIncludesTax">A value indicating whether price includes tax.</param>
        /// <param name="showTax">A value indicating whether to show the tax suffix.</param>
        /// <returns>Formatted shipping price.</returns>
        Task<string> FormatShippingPriceAsync(decimal price, bool showCurrency, string currencyCode, Language language, bool priceIncludesTax, bool showTax);


        /// <summary>
        /// Formats the payment method additional fee.
        /// </summary>
        /// <param name="price">Price.</param>
        /// <param name="showCurrency">A value indicating whether to show a currency.</param>
        /// <returns>Formatted payment method additional fee.</returns>
        Task<string> FormatPaymentMethodAdditionalFeeAsync(decimal price, bool showCurrency);

        /// <summary>
        /// Formats the payment method additional fee.
        /// </summary>
        /// <param name="price">Price.</param>
        /// <param name="showCurrency">A value indicating whether to show a currency.</param>
        /// <param name="targetCurrency">Target currency.</param>
        /// <param name="language">Language entity.</param>
        /// <param name="priceIncludesTax">A value indicating whether price includes tax.</param>
        /// <returns>Formatted payment method additional fee.</returns>
        Task<string> FormatPaymentMethodAdditionalFeeAsync(decimal price, bool showCurrency, Currency targetCurrency, Language language, bool priceIncludesTax);

        /// <summary>
        /// Formats the payment method additional fee.
        /// </summary>
        /// <param name="price">Price.</param>
        /// <param name="showCurrency">A value indicating whether to show a currency.</param>
        /// <param name="currencyCode">Currency code.</param>
        /// <param name="language">Language entity.</param>
        /// <param name="priceIncludesTax">A value indicating whether price includes tax.</param>
        /// <param name="showTax">A value indicating whether to show the tax suffix.</param>
        /// <returns>Formatted payment method additional fee.</returns>
        Task<string> FormatPaymentMethodAdditionalFeeAsync(decimal price, bool showCurrency, string currencyCode, Language language, bool priceIncludesTax, bool showTax);

        /// <summary>
        /// Formats a tax rate.
        /// </summary>
        /// <param name="taxRate">Tax rate.</param>
        /// <returns>Formatted tax rate.</returns>
        string FormatTaxRate(decimal taxRate);
    }
}
