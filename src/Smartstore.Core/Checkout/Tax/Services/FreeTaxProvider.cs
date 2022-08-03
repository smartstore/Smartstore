using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Tax
{
    [SystemName("Tax.Free")]
    [FriendlyName("Free tax rate provider")]
    [Order(0)]
    internal class FreeTaxProvider : ITaxProvider
    {
        public Task<TaxRate> GetTaxRateAsync(TaxRateRequest request)
            => Task.FromResult(new TaxRate(0m, request.TaxCategoryId));
    }
}