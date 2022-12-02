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

            var allTaxRates = await _db.TaxRates()
                .AsNoTracking()
                .OrderBy(x => x.CountryId)
                .ThenBy(x => x.StateProvinceId)
                .ThenBy(x => x.Zip)
                .ThenBy(x => x.TaxCategoryId)
                .ToListAsync();

            var matchedByCountry = allTaxRates
                .Where(x => x.CountryId == request.Address.CountryId && x.TaxCategoryId == request.TaxCategoryId)
                .ToList();

            var matchedByStateProvince = matchedByCountry
                .Where(x => x.StateProvinceId == request.Address.StateProvinceId)
                .ToList();

            if (matchedByStateProvince.Count == 0)
            {
                matchedByStateProvince.AddRange(matchedByCountry.Where(x => x.StateProvinceId == 0));
            }

            var matchedByZip = matchedByStateProvince
                .Where(x => x.Zip == request.Address.ZipPostalCode)
                .ToList();

            if (matchedByZip.Count == 0 || !request.Address.ZipPostalCode.HasValue())
            {
                matchedByZip.AddRange(matchedByStateProvince.Where(x => string.IsNullOrEmpty(x.Zip)));
            }

            if (matchedByZip.Any())
            {
                rate = matchedByZip[0].Percentage;
            }

            return new TaxRate(rate, request.TaxCategoryId);
        }
    }
}
