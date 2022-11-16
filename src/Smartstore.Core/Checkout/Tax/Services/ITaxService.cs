using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
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
        ///     Gets a tax formatting pattern that can be applied to
        ///     <see cref="Money"/> values by calling <see cref="Money.WithPostFormat(string)"/>.
        /// </summary>
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
        ///     The target object to format price for. This parameter affects how <paramref name="displayTaxSuffix"/>
        ///     will be auto-resolved if it is <c>null</c>.
        /// </param>
        /// <param name="language">
        ///     Language for tax suffix. If <c>null</c>, language will be obtained via <see cref="IWorkContext.WorkingLanguage"/>.
        /// </param>
        /// <returns>Money</returns>
        string GetTaxFormat(
            bool? displayTaxSuffix = null,
            bool? priceIncludesTax = null,
            PricingTarget target = PricingTarget.Product,
            Language language = null);

        /// <summary>
        /// Checks the validity of a VAT number using an online service like VIES service of the EU.
        /// </summary>
        /// <param name="fullVatNumber">The full VAT number including the two letter country ISO code (e.g. DE 111 1111 111).</param>
        /// <returns>Result of the VAT check including current status.</returns>
        Task<VatCheckResult> GetVatNumberStatusAsync(string fullVatNumber);

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