using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Tax
{
    /// <summary>
    /// Provides an interface for creating tax providers
    /// </summary>
    public partial interface ITaxProvider : IProvider
    {
        /// <summary>
        /// Gets tax rate
        /// </summary>
        Task<TaxRate> GetTaxRateAsync(TaxRateRequest request);
    }
}