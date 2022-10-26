using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;

namespace Smartstore.Core
{
    /// <summary>
    /// Work context source interface
    /// </summary>
    public interface IWorkContextSource
    {
        /// <summary>
        /// Resolves the current customer (uncached).
        /// </summary>
        /// <returns>
        /// A value tuple where first item is the resolved current or impersonated customer and 
        /// second item is the impersonator (if any) or null.
        /// </returns>
        Task<(Customer, Customer)> ResolveCurrentCustomerAsync();

        /// <summary>
        /// Resolves the working language for the given customer (uncached).
        /// </summary>
        Task<Language> ResolveWorkingLanguageAsync(Customer customer);

        /// <summary>
        /// Resolves the working currency for the given customer (uncached).
        /// </summary>
        Task<Currency> ResolveWorkingCurrencyAsync(Customer customer, bool forAdminArea);

        /// <summary>
        /// Resolves the tax display type for the given customer and store id (uncached).
        /// </summary>
        Task<TaxDisplayType> ResolveTaxDisplayTypeAsync(Customer customer, int storeId);

        /// <summary>
        /// Saves given customer attribute to database.
        /// </summary>
        /// <param name="customer">The customer to save an attribute for.</param>
        /// <param name="name">The name of a generic attribute, e.g.: CurrencyId, Language Id etc.</param>
        /// <param name="value">The value of the generic attribute.</param>
        /// <param name="async">Whether to perform the save operation asynchronously.</param>
        Task SaveCustomerAttribute(Customer customer, string name, int? value, bool async);
    }
}