using System.Threading.Tasks;
using Smartstore.Core.Checkout.Tax;

namespace Smartstore.Core.Tests.Tax
{
    public class FixedRateTestTaxProvider : ITaxProvider
    {
        public Task<TaxRate> GetTaxRateAsync(TaxRateRequest request)
        {
            return Task.FromResult(new TaxRate(10, request.TaxCategoryId));
        }
    }
}