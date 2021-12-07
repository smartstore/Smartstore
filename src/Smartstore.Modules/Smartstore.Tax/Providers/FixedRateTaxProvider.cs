using Smartstore.Core.Checkout.Tax;
using Smartstore.Http;

namespace Smartstore.Tax
{
    [SystemName("Tax.FixedRate")]
    [FriendlyName("Fixed Tax Rate")]
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

        public async Task<TaxRate> GetTaxRateAsync(TaxRateRequest request)
        {
            var rate = await _settingService.GetSettingByKeyAsync<decimal>($"Tax.TaxProvider.FixedRate.TaxCategoryId{request.TaxCategoryId}");
            return new TaxRate(rate, request.TaxCategoryId);
        }
    }
}
