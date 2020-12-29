using System.ComponentModel.DataAnnotations;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Tax.Services
{
    [SystemName("Tax.Free")]
    [FriendlyName("Free tax rate provider")]
    [Display(Order = 0)]
    public class FreeTaxProvider : ITaxProvider
    {
        public CalculateTaxResult GetTaxRate(CalculateTaxRequest calculateTaxRequest)
        {
            return new CalculateTaxResult()
            {
                TaxRate = decimal.Zero
            };
        }
    }
}