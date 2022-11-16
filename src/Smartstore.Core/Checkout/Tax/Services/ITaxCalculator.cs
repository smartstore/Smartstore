using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Tax
{
    /// <summary>
    /// Tax calculator interface
    /// </summary>
    public interface ITaxCalculator
    {
        /// <summary>
        /// Calculates tax for a given gross price (price including tax).
        /// </summary>
        /// <param name="grossPrice">Gross price</param>
        /// <param name="rate">Tax rate</param>
        /// <param name="inclusive">A value indicating whether the result price should be gross (inclusing tax).</param>
        /// <param name="currency">Optional currency for result price rounding. If <c>null</c>, result will be unrounded.</param>
        Tax CalculateTaxFromGross(decimal grossPrice, TaxRate rate, bool inclusive, Currency currency = null);

        /// <summary>
        /// Calculates tax for a given net price (price excluding tax).
        /// </summary>
        /// <param name="netPrice">Net price</param>
        /// <param name="rate">Tax rate</param>
        /// <param name="inclusive">A value indicating whether the result price should be gross (inclusing tax).</param>
        /// <param name="currency">Optional currency for result price rounding. If <c>null</c>, result will be unrounded.</param>
        Tax CalculateTaxFromNet(decimal netPrice, TaxRate rate, bool inclusive, Currency currency = null);

        /// <summary>
        /// Calculates product tax.
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="price">Product price.</param>
        /// <param name="inclusive">A value indicating whether the calculated price should include tax. Obtained from <see cref="IWorkContext.TaxDisplayType"/> if <c>null</c>.</param>
        /// <param name="customer">Customer. Obtained from <see cref="IWorkContext.CurrentCustomer"/> if <c>null</c>.</param>
        /// <param name="currency">Optional currency instance for cash rounding.</param>
        /// <returns>A <see cref="Tax"/> structure containing the calculation result.</returns>
        Task<Tax> CalculateProductTaxAsync(
            Product product,
            decimal price,
            bool? inclusive = null,
            Customer customer = null,
            Currency currency = null);

        /// <summary>
        /// Calculates tax for a checkout attribute value.
        /// </summary>
        /// <param name="attributeValue">Checkout attribute value.</param>
        /// <param name="inclusive">A value indicating whether the calculated price should include tax. Obtained from <see cref="IWorkContext.TaxDisplayType"/> if <c>null</c>.</param>
        /// <param name="customer">Customer. Obtained from <see cref="IWorkContext.CurrentCustomer"/> if <c>null</c>.</param>
        /// <param name="currency">Optional currency instance for cash rounding.</param>
        /// <returns>A <see cref="Tax"/> structure containing the calculation result.</returns>
        Task<Tax> CalculateCheckoutAttributeTaxAsync(
            CheckoutAttributeValue attributeValue,
            bool? inclusive = null,
            Customer customer = null,
            Currency currency = null);

        /// <summary>
        /// Calculates shipping charge tax.
        /// </summary>
        /// <param name="price">The shipping charge.</param>
        /// <param name="inclusive">A value indicating whether the calculated price should include tax. Obtained from <see cref="TaxSettings.ShippingPriceIncludesTax"/> if <c>null</c>.</param>
        /// <param name="taxCategoryId">Tax category identifier. If <c>null</c>, <see cref="TaxSettings.ShippingTaxClassId"/> will be used.</param>
        /// <param name="customer">Customer. Obtained from <see cref="IWorkContext.CurrentCustomer"/> if <c>null</c>.</param>
        /// <param name="currency">Optional currency instance for cash rounding.</param>
        /// <returns>A <see cref="Tax"/> structure containing the calculation result.</returns>
        Task<Tax> CalculateShippingTaxAsync(
            decimal price,
            bool? inclusive = null,
            int? taxCategoryId = null,
            Customer customer = null,
            Currency currency = null);

        /// <summary>
        /// Calculates payment fee tax.
        /// </summary>
        /// <param name="price">The payment fee.</param>
        /// <param name="inclusive">A value indicating whether the calculated price should include tax. Obtained from <see cref="TaxSettings.PaymentMethodAdditionalFeeIncludesTax"/> if <c>null</c>.</param>
        /// <param name="customer">Customer. Obtained from <see cref="IWorkContext.CurrentCustomer"/> if <c>null</c>.</param>
        /// <param name="taxCategoryId">Tax category identifier. If <c>null</c>, <see cref="TaxSettings.PaymentMethodAdditionalFeeTaxClassId"/> will be used.</param>
        /// <param name="currency">Optional currency instance for cash rounding.</param>
        /// <returns>A <see cref="Tax"/> structure containing the calculation result.</returns>
        Task<Tax> CalculatePaymentFeeTaxAsync(
            decimal price,
            bool? inclusive = null,
            int? taxCategoryId = null,
            Customer customer = null,
            Currency currency = null);
    }
}
