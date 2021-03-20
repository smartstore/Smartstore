using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Tax
{
    /// <summary>
    /// Tax service interface.
    /// </summary>
    public partial interface ITaxService
    {
        #region OBSOLETE

        // TODO: (mg) (core) Remove the shitty methods in this region once ITaxCalculator is incorporated evrywhere.

        /// <summary>
        /// Calculates the product price in the primary currency.
        /// </summary>
        /// <param name="product">Product. Can be <c>null</c>.</param>
        /// <param name="price">Product price.</param>
        /// <param name="includingTax">A value indicating whether the calculated price should include tax. Obtained from <see cref="IWorkContext.TaxDisplayType"/> if <c>null</c>.</param>
        /// <param name="priceIncludesTax">A value indicating whether price already includes tax. Obtained from <see cref="TaxSettings.PricesIncludeTax"/> if <c>null</c>.</param>
        /// <param name="taxCategoryId">Tax category identifier. Obtained from <see cref="Product.TaxCategoryId"/> if <c>null</c>.</param>
        /// <param name="customer">Customer. Obtained from <see cref="IWorkContext.CurrentCustomer"/> if <c>null</c>.</param>
        /// <returns>Product price in the primary currency and tax rate.</returns>
        Task<(Money Price, decimal TaxRate)> GetProductPriceAsync(
            Product product,
            Money price,
            bool? includingTax = null,
            bool? priceIncludesTax = null,
            int? taxCategoryId = null,
            Customer customer = null);

        /// <summary>
        /// Gets the shipping price in the primary currency.
        /// </summary>
        /// <param name="price">Shipping price.</param>
        /// <param name="includingTax">A value indicating whether calculated price should include tax. Obtained from <see cref="IWorkContext.TaxDisplayType"/> if <c>null</c>.</param>
        /// <param name="taxCategoryId">Tax category identifier. Obtained from <see cref="Product.TaxCategoryId"/> if <c>null</c>.</param>
        /// <param name="customer">Customer. Obtained from <see cref="IWorkContext.CurrentCustomer"/> if <c>null</c>.</param>
        /// <returns>Shipping price in the primary currency and tax rate.</returns>
        Task<(Money Price, decimal TaxRate)> GetShippingPriceAsync(
            Money price,
            bool? includingTax = null,
            int? taxCategoryId = null,
            Customer customer = null);

        /// <summary>
        /// Gets additional payment method fee in the primary currency.
        /// </summary>
        /// <param name="price">Additional payment method fee.</param>
        /// <param name="includingTax">A value indicating whether calculated price should include tax. Obtained from <see cref="IWorkContext.TaxDisplayType"/> if <c>null</c>.</param>
        /// <param name="taxCategoryId">Tax category identifier. Is <see cref="TaxSettings.PaymentMethodAdditionalFeeTaxClassId"/> if <c>null</c>.</param>
        /// <param name="customer">Customer. Obtained from <see cref="IWorkContext.CurrentCustomer"/> if <c>null</c>.</param>
        /// <returns>Additional payment method fee in the primary currency and tax rate.</returns>
        Task<(Money Price, decimal TaxRate)> GetPaymentMethodFeeAsync(
            Money price,
            bool? includingTax = null,
            int? taxCategoryId = null,
            Customer customer = null);

        /// <summary>
        /// Gets the checkout attribute value price in the primary currency.
        /// </summary>
        /// <param name="attributeValue">Checkout attribute value.</param>
        /// <param name="includingTax">A value indicating whether calculated price should include tax. Obtained from <see cref="IWorkContext.TaxDisplayType"/> if <c>null</c>.</param>
        /// <param name="customer">Customer. Obtained from <see cref="IWorkContext.CurrentCustomer"/> if <c>null</c>.</param>
        /// <returns>Checkout attribute value price in the primary currency and tax rate.</returns>
        Task<(Money Price, decimal TaxRate)> GetCheckoutAttributePriceAsync(
            CheckoutAttributeValue attributeValue,
            bool? includingTax = null,
            Customer customer = null);

        #endregion

        /// <summary>
        /// Loads active tax provider.
        /// </summary>
        /// <returns>
        /// Active tax provider or <c>null</c> if none was found.
        /// </returns>
        Provider<ITaxProvider> LoadActiveTaxProvider();

        /// <summary>
        /// Loads tax provider by system name.
        /// </summary>
        /// <param name="systemName">Tax providers system name.</param>
        /// <returns>
        /// Tax provider or <c>null</c> if none was found by system name.
        /// </returns>
        Provider<ITaxProvider> LoadTaxProviderBySystemName(string systemName);

        /// <summary>
        /// Loads all tax providers.
        /// </summary>
        /// <returns>
        /// All tax providers or <c>null</c> if none were found.
        /// </returns>
        IEnumerable<Provider<ITaxProvider>> LoadAllTaxProviders();

        /// <summary>
        /// Formats a tax rate.
        /// </summary>
        /// <param name="taxRate">Tax rate to format.</param>
        /// <returns>Formatted tax rate.</returns>
        string FormatTaxRate(decimal taxRate);

        /// <summary>
        /// Gets tax rate for given product.
        /// </summary>
        /// <param name="product">Product. Can be <c>null</c>.</param>
        /// <param name="taxCategoryId">Tax category identifier. Obtained from <see cref="Product.TaxCategoryId"/> if <c>null</c>.</param>
        /// <param name="customer">Customer. Obtained from <see cref="IWorkContext.CurrentCustomer"/> if <c>null</c>.</param>
        /// <returns>Tax rate.</returns>
        Task<TaxRate> GetTaxRateAsync(Product product, int? taxCategoryId = null, Customer customer = null);

        /// <summary>
        /// Gets VAT Number status.
        /// </summary>
        /// <param name="fullVatNumber">Two letter ISO code of country with VAT number (e.g. GB 111 1111 111).</param>
        /// <param name="name">Name (if received).</param>
        /// <param name="address">Address (if received).</param>
        /// <returns>VAT Number status.</returns>
        Task<(VatNumberStatus status, string name, string address)> GetVatNumberStatusAsync(string fullVatNumber);

        /// <summary>
        /// Checks whether the product is tax exempt for the customer.
        /// </summary>
        /// <param name="product">Product for tax exemption.</param>
        /// <param name="customer">Customer to check.</param>
        /// <remarks>
        /// Either product or customer can be <c>null</c>.
        /// </remarks>
        /// <returns>
        /// <c>True</c> if the product is tax exempted, <c>False</c> if otherwise.
        /// </returns>
        Task<bool> IsTaxExemptAsync(Product product, Customer customer);

        /// <summary>
        /// Checks whether the customer has a EU VAT exemption (the European Union value added tax).
        /// </summary>
        /// <param name="customer">Customer for VAT exemption.</param>
        /// <param name="address">Address to check. Gets tax address of customer if <c>null</c>.</param>
        /// <returns>
        /// <c>True</c> if customer is VAT exempted, <c>False</c> if customer is not VAT exempted.
        /// </returns>
        Task<bool> IsVatExemptAsync(Customer customer, Address address = null);
    }
}