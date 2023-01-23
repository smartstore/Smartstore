using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Configuration;
using Smartstore.Core;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Configuration;
using Smartstore.Core.Localization;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.Shipping.Settings;
using SmartStore.DellyManLogistics.Client;
using SmartStore.DellyManLogistics.Models;

namespace Smartstore.Shipping
{
    [SystemName("SmartStore.DellyManLogistics")]
    [FriendlyName("Distance Rate Shipping", Description = "Distance Rate Shipping")]
    [Order(0)]
    public class DistanceRateProvider : IShippingRateComputationMethod, IConfigurable
    {
        private readonly IDellyManClient _dellyManClient;
        private readonly IShippingService _shippingService;
        private readonly ISettingService _settingService;
        private readonly DellyManLogisticsSettings _dellyManLogisticsSettings;
        private readonly ICommonServices _services;

        public DistanceRateProvider(IDellyManClient dellyManClient,
            IShippingService shippingService,
            ISettingService settingService,
            DellyManLogisticsSettings dellyManLogisticsSettings,
            ICommonServices services)
        {
            _dellyManClient = dellyManClient;
            _shippingService = shippingService;
            _settingService = settingService;
            _dellyManLogisticsSettings = dellyManLogisticsSettings;
            _services = services;
            T = NullLocalizer.Instance;
        }

        public static string SystemName => "SmartStore.DellyManLogistics";
        public Localizer T { get; set; }
        public ShippingRateComputationMethodType ShippingRateComputationMethodType => ShippingRateComputationMethodType.Realtime;

        public IShipmentTracker ShipmentTracker => null;

        public bool IsActive => true;

        public RouteInfo GetConfigurationRoute()
             => new("Configure", "DellyManLogistics", new { area = "Admin" });


        public Task<decimal?> GetFixedRateAsync(ShippingOptionRequest request) => Task.FromResult<decimal?>(1000m);

        public async Task<ShippingOptionResponse> GetShippingOptionsAsync(ShippingOptionRequest request)
        {
            Guard.NotNull(request, nameof(request));

            var response = new ShippingOptionResponse();

            if (request.Items == null || request.Items.Count == 0)
            {
                response.Errors.Add(T("Admin.System.Warnings.NoShipmentItems"));
                return response;
            }

            var shippingMethods = await _shippingService.GetAllShippingMethodsAsync(request.StoreId, true);
            var byGroundShipping = shippingMethods.FirstOrDefault(s => s.Name == "By Ground");
            //foreach (var shippingMethod in shippingMethods)
            //{
                var shippingOption = new ShippingOption
                {
                    ShippingMethodId = byGroundShipping.Id,
                    Name = byGroundShipping.GetLocalized(x => x.Name),
                    Description = byGroundShipping.GetLocalized(x => x.Description),
                    Rate = await GetRateAsync(request)
                };
                response.ShippingOptions.Add(shippingOption);
          //  }

            return response;
        }

        private async Task<decimal> GetRateAsync(ShippingOptionRequest getShippingOptionRequest)
        {
            var getQuoteRequestModel = new GetQuoteRequestModel
            {
                CustomerID = int.Parse(_dellyManLogisticsSettings.CustomerId),
                DeliveryAddress = new List<string> { getShippingOptionRequest.ShippingAddress.Address1 },
                InsuranceAmount = 0,
                IsInstantDelivery = 0,
                IsProductInsurance = 0,
                IsProductOrder = 0,
                PackageWeight = new List<int>(),
                PaymentMode = "pickup",
                PickupAddress = _dellyManLogisticsSettings.DefaultPickUpGoogleAddress,
                PickupRequestedDate = DateTime.Now.ToString("dd/MM/yyyy"),
                PickupRequestedTime = _dellyManLogisticsSettings.PickupRequestedTime,
                ProductAmount = new List<decimal> { },
                VehicleID = 1

            };

            var result = await _dellyManClient.GetQuotesAsync(getQuoteRequestModel);

            await result.EnsureSuccessStatusCodeAsync();
            var response = result.Content;

            if (response.ResponseCode == 100 && response.ResponseMessage.Equals("Success", StringComparison.InvariantCultureIgnoreCase))
            {
                if (response.Data != null && response.Data.Count > 0)
                {
                    var company = response.Data.FirstOrDefault();
                    if (string.IsNullOrEmpty(_dellyManLogisticsSettings.CompanyId) || company.CompanyID.ToString() != _dellyManLogisticsSettings.CompanyId)
                    {
                        _dellyManLogisticsSettings.CompanyId = company.CompanyID.ToString();
                      await  _services.SettingFactory.SaveSettingsAsync<DellyManLogisticsSettings>(_dellyManLogisticsSettings, _services.StoreContext.CurrentStore.Id);
                        // _settingService.Settings.SaveSetting<DellyManLogisticsSettings>(settings, _services.StoreContext.CurrentStore.Id);
                    }

                    return company.TotalPrice;
                }

                return _dellyManLogisticsSettings.DefaultDeliveryFee;
            }
            else
            {
                return _dellyManLogisticsSettings.DefaultDeliveryFee;
            }

        }

    }
}
