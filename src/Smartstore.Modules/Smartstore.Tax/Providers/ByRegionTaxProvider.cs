using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Data;
using Smartstore.Http;

namespace Smartstore.Tax
{
    [SystemName("Tax.CountryStateZip")]
    [FriendlyName("Tax By Region")]
    [Order(10)]
    internal class ByRegionTaxProvider : ITaxProvider, IConfigurable
    {
        private readonly SmartDbContext _db;
        private readonly ISettingService _settingService;
        private readonly TaxSettings _taxSettings;

        public ByRegionTaxProvider(SmartDbContext db, ISettingService settingService, TaxSettings taxSettings)
        {
            _db = db;
            _settingService = settingService;
            _taxSettings = taxSettings;
        }

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "TaxByRegion", new { area = "Admin" });

        public async Task<TaxRate> GetTaxRateAsync(TaxRateRequest request)
        {
            var address = request.Address;

            if (_taxSettings.EuVatEnabled)
            {
                if (!(address?.Country?.SubjectToVat ?? false))
                {
                    // Fallback to fixed rate (merchant country VAT rate).
                    var fixedRate = _settingService.GetSettingByKey<decimal>($"Tax.TaxProvider.FixedRate.TaxCategoryId{request.TaxCategoryId}");

                    return new TaxRate(fixedRate, request.TaxCategoryId);
                }
            }

            decimal rate = 0;

            var taxRates = await _db.TaxRates()
                .AsNoTracking()
                .ApplyRegionFilter(
                    request.TaxCategoryId,
                    address?.Country?.Id,
                    address?.StateProvince?.Id,
                    address?.ZipPostalCode
                ).ToListAsync();

            if (taxRates.Any())
            {
                rate = taxRates[0].Percentage;
            }

            return new TaxRate(rate, request.TaxCategoryId);
        }
    }
}
