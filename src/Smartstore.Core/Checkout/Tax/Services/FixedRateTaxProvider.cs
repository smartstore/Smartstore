using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Smartstore.Core.Configuration;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Tax
{
    // TODO: (mg) (core) move FixedRateTaxProvider to tax module and complete it when IConfigurable is available.
    [SystemName("Tax.FixedRate")]
    [FriendlyName("Fixed tax rate provider")]
    [Display(Order = 5)]
    internal class FixedRateTaxProvider : ITaxProvider/*, IConfigurable*/
    {
        private readonly ISettingService _settingService;

        public FixedRateTaxProvider(ISettingService settingService)
        {
            _settingService = settingService;
        }

        public Task<TaxRate> GetTaxRateAsync(TaxRateRequest request)
        {
            var rate = _settingService.GetSettingByKey<decimal>($"Tax.TaxProvider.FixedRate.TaxCategoryId{request.TaxCategoryId}");

            return Task.FromResult(new TaxRate(rate, request.TaxCategoryId));
        }
    }
}
