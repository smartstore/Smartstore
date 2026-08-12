using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;

namespace Smartstore.Core;

/// <summary>
/// Work context
/// </summary>
public interface IWorkContext
{
    /// <summary>
    /// Initializes the work context by pre-resolving current customer
    /// working language, working currency and tax display type asynchronously.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Gets a value indicating whether the work context was initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets or sets the current customer. Only set this property to override the current customer, 
    /// which is resolved by the <c>WorkContextMiddleware</c> very early in the request pipeline.
    /// After setting this property, call <see cref="InitializeAsync"/> asynchronously
    /// to re-resolve the working language, working currency and tax display type.
    /// </summary>
    Customer CurrentCustomer { get; set; }

    /// <summary>
    /// Gets the original customer (in case the current in <see cref="CurrentCustomer"/> is impersonated)
    /// </summary>
    Customer CurrentImpersonator { get; }

    /// <summary>
    /// Get or set current user working language
    /// </summary>
    Language WorkingLanguage { get; set; }

    /// <summary>
    /// Get or set current user working currency
    /// </summary>
    Currency WorkingCurrency { get; set; }

    /// <summary>
    /// Get or set current tax display type
    /// </summary>
    TaxDisplayType TaxDisplayType { get; set; }

    /// <summary>
    /// Gets the tax display type for a given customer
    /// </summary>
    Task<TaxDisplayType> GetTaxDisplayTypeAsync(Customer customer, int storeId);

    /// <summary>
    /// Gets or sets a value indicating whether we're in admin area
    /// </summary>
		bool IsAdminArea { get; set; }
}