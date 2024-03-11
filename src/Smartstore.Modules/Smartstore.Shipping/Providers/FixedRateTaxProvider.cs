using System.Collections.Generic;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Localization;
using Smartstore.Http;

namespace Smartstore.Shipping
{
    [SystemName("Shipping.FixedRate")]
    [FriendlyName("Fixed Rate Shipping")]
    [Order(0)]
    internal class FixedRateProvider : IShippingRateComputationMethod, IConfigurable
    {
        private readonly IShippingService _shippingService;
        private readonly ISettingService _settingService;

        public FixedRateProvider(IShippingService shippingService, ISettingService settingService)
        {
            _shippingService = shippingService;
            _settingService = settingService;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        private decimal GetRate(int shippingMethodId)
        {
            var key = $"ShippingRateComputationMethod.FixedRate.Rate.ShippingMethodId{shippingMethodId}";
            var rate = _settingService.GetSettingByKey<decimal>(key);
            return rate;
        }

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "FixedRate", new { area = "Admin" });

        public async Task<decimal?> GetFixedRateAsync(ShippingOptionRequest request)
        {
            Guard.NotNull(request);

            var rates = new List<decimal>();
            var shippingMethods = await _shippingService.GetAllShippingMethodsAsync(request.StoreId, request.MatchRules);

            foreach (var shippingMethod in shippingMethods)
            {
                var rate = GetRate(shippingMethod.Id);
                if (!rates.Contains(rate))
                {
                    rates.Add(rate);
                }
            }

            // Return default rate if all of them are equal.
            if (rates.Count == 1)
            {
                return rates[0];
            }

            return null;
        }

        public async Task<ShippingOptionResponse> GetShippingOptionsAsync(ShippingOptionRequest request)
        {
            Guard.NotNull(request);

            var response = new ShippingOptionResponse();

            if (request.Items.IsNullOrEmpty())
            {
                response.Errors.Add(T("Admin.System.Warnings.NoShipmentItems"));
                return response;
            }

            var shippingMethods = await _shippingService.GetAllShippingMethodsAsync(request.StoreId, request.MatchRules);
            foreach (var shippingMethod in shippingMethods)
            {
                var shippingOption = new ShippingOption
                {
                    ShippingMethodId = shippingMethod.Id,
                    Name = shippingMethod.GetLocalized(x => x.Name),
                    Description = shippingMethod.GetLocalized(x => x.Description),
                    Rate = GetRate(shippingMethod.Id)
                };
                response.ShippingOptions.Add(shippingOption);
            }

            return response;
        }

        /// <summary>
        /// Gets a shipping rate computation method type
        /// </summary>
        public ShippingRateComputationMethodType ShippingRateComputationMethodType
            => ShippingRateComputationMethodType.Offline;

        /// <summary>
        /// Gets a shipment tracker
        /// </summary>
        public IShipmentTracker ShipmentTracker =>
                //uncomment the line below to return a general shipment tracker (finds an appropriate tracker by tracking number)
                //return new GeneralShipmentTracker(EngineContext.Current.Resolve<ITypeFinder>());
                null;
    }
}
