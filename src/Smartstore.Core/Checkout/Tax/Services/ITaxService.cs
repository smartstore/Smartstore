using System.Collections.Generic;
using System.Threading.Tasks;
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
        /// Gets tax rate for product async.
        /// </summary>
        /// <param name="product">Product to get tax rate. Can be <c>null</c>.</param>
        /// <param name="taxCategoryId">Tax category identifier. Is <see cref="Product.TaxCategoryId"/> if <c>null</c> and product <c>is not null</c>.</param>
        /// <param name="customer">Customer to get tax rate. Is <see cref="IWorkContext.CurrentCustomer"/> if <c>null</c>.</param>
        /// <returns>Tax rate.</returns>
        Task<decimal> GetTaxRateAsync(Product product, int? taxCategoryId = null, Customer customer = null);

        /// <summary>
        /// Gets the calculated price of the product for the currency async.
        /// </summary>
        /// <param name="product">Product to get tax rate. Can be <c>null</c>.</param>
        /// <param name="price">Products base unit price.</param>
        /// <param name="includingTax">
        /// Whether calculated price should include tax. 
        /// Is the equality check result of <see cref="IWorkContext.TaxDisplayType"/> to <see cref="TaxDisplayType.IncludingTax"/> if <c>null</c>.
        /// </param>
        /// <param name="priceIncludesTax">Whether price already includes tax. Is <see cref="TaxSettings.PricesIncludeTax"/> if <c>null</c>.</param>
        /// <param name="taxCategoryId">Tax category identifier. Is <see cref="Product.TaxCategoryId"/> if <c>null</c>.</param>
        /// <param name="customer">Customer to get tax rate. Is <see cref="IWorkContext.CurrentCustomer"/> if <c>null</c>.</param>
        /// <param name="currency">Currency used for price calculation. Is <see cref="IWorkContext.WorkingCurrency"/> if <c>null</c>.</param>
        /// <returns>Price converted to desired currency as <see cref="Money"/> and tax rate.</returns>
        Task<(Money price, decimal taxRate)> GetProductPriceAsync(
            Product product,
            Money price,
            bool? includingTax = null,
            bool? priceIncludesTax = null,
            int? taxCategoryId = null,            
            Customer customer = null,
            Currency currency = null);

        /// <summary>
        /// Gets the calculated shipping price for <see cref="IWorkContext.WorkingCurrency"/> async.
        /// </summary>
        /// <remarks>
        /// Returns price directly if <see cref="TaxSettings.ShippingIsTaxable"/> is <c>false</c>.</remarks>
        /// <param name="price">Shipping price.</param>
        /// <param name="includingTax">
        /// Whether calculated price should include tax. 
        /// Is the equality check result of <see cref="IWorkContext.TaxDisplayType"/> to <see cref="TaxDisplayType.IncludingTax"/> if <c>null</c>.
        /// </param>
        /// <param name="customer">Customer to get tax rate. Is <see cref="IWorkContext.CurrentCustomer"/> if <c>null</c>.</param>
        /// <param name="taxCategoryId">Tax category identifier. Is <see cref="TaxSettings.ShippingTaxClassId"/> if <c>null</c>.</param>
        /// <returns>Calculated shipping price converted to <see cref="IWorkContext.WorkingCurrency"/> and tax rate.</returns>
        Task<(Money price, decimal taxRate)> GetShippingPriceAsync(
            Money price, 
            bool? includingTax = null, 
            Customer customer = null, 
            int? taxCategoryId = null);

        /// <summary>
        /// Gets the calculated additional payment method fee for <see cref="IWorkContext.WorkingCurrency"/> async.
        /// </summary>
        /// <remarks>
        /// Returns price directly if <see cref="TaxSettings.PaymentMethodAdditionalFeeIsTaxable"/> is <c>false</c>.</remarks>
        /// <param name="price">Additional payment method fee.</param>
        /// <param name="includingTax">
        /// Whether calculated price should include tax. 
        /// Is the equality check result of <see cref="IWorkContext.TaxDisplayType"/> to <see cref="TaxDisplayType.IncludingTax"/> if <c>null</c>.
        /// </param>
        /// <param name="customer">Customer to get tax rate. Is <see cref="IWorkContext.CurrentCustomer"/> if <c>null</c>.</param>
        /// <param name="taxCategoryId">Tax category identifier. Is <see cref="TaxSettings.PaymentMethodAdditionalFeeTaxClassId"/> if <c>null</c>.</param>
        /// <returns>Calculated additional payment method fee converted to <see cref="IWorkContext.WorkingCurrency"/> and tax rate.</returns>
        Task<(Money price, decimal taxRate)> GetPaymentMethodAdditionalFeeAsync(
            Money price, 
            bool? includingTax = null,
            int? taxCategoryId = null, 
            Customer customer = null);

        /// <summary>
        /// Gets the calculated checkout attribute value price for <see cref="IWorkContext.WorkingCurrency"/> async.
        /// </summary>
        /// <remarks>
        /// Returns <see cref="CheckoutAttributeValue.PriceAdjustment"/> directly if <see cref="CheckoutAttribute.IsTaxExempt"/> is <c>false</c>.</remarks>
        /// <param name="attributeValue">Checkout attribute value for price calculation.</param>
        /// <param name="includingTax">
        /// Whether calculated price should include tax. 
        /// Is the equality check result of <see cref="IWorkContext.TaxDisplayType"/> to <see cref="TaxDisplayType.IncludingTax"/> if <c>null</c>.
        /// </param>
        /// <param name="customer">Customer to get tax rate. Is <see cref="IWorkContext.CurrentCustomer"/> if <c>null</c>.</param>
        /// <returns>Calculated checkout attribute value price converted to <see cref="IWorkContext.WorkingCurrency"/>.</returns>
        Task<(Money price, decimal taxRate)> GetCheckoutAttributePriceAsync(
            CheckoutAttributeValue attributeValue, 
            Customer customer = null, 
            Currency currency = null, 
            bool? includingTax = null);

        /// <summary>
        /// Gets VAT Number status.
        /// </summary>
        /// <param name="fullVatNumber">Two letter ISO code of country with VAT number (e.g. GB 111 1111 111).</param>
        /// <param name="name">Name (if received).</param>
        /// <param name="address">Address (if received).</param>
        /// <returns>VAT Number status.</returns>
        (VatNumberStatus status, string name, string address) GetVatNumberStatus(string fullVatNumber);

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
        /// Checks whether the customer has a VAT exemption.
        /// </summary>
        /// <param name="customer">Customer for VAT exemption.</param>
        /// <param name="address">Address to check. Gets tax address of customer if <c>null</c>.</param>
        /// <returns>
        /// <c>True</c> if customer is VAT exempted, <c>False</c> if customer is not VAT exempted.
        /// </returns>
        Task<bool> IsVatExemptAsync(Customer customer, Address address = null);
    }
}