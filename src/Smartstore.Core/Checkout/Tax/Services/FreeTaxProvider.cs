using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Tax
{
    [SystemName("Tax.Free")]
    [FriendlyName("Free tax rate provider")]
    [Display(Order = 0)]
    public class FreeTaxProvider : ITaxProvider
    {
        public Task<CalculateTaxResult> GetTaxRateAsync(CalculateTaxRequest calculateTaxRequest)
        {
            return Task.FromResult(new CalculateTaxResult());
        }
    }
}