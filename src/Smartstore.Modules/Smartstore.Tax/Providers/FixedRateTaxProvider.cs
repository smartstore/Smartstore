using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Configuration;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.Tax
{
    [SystemName("Tax.FixedRate")]
    [FriendlyName("Fixed tax rate provider")]
    [Order(5)]
    internal class FixedRateTaxProvider : ITaxProvider, IConfigurable
    {
        private readonly ISettingService _settingService;

        public FixedRateTaxProvider(ISettingService settingService)
        {
            _settingService = settingService;
        }

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "TaxFixedRate", new { area = "Admin" });

        public Task<TaxRate> GetTaxRateAsync(TaxRateRequest request)
        {
            var rate = _settingService.GetSettingByKey<decimal>($"Tax.TaxProvider.FixedRate.TaxCategoryId{request.TaxCategoryId}");

            return Task.FromResult(new TaxRate(rate, request.TaxCategoryId));
        }
    }
}
