using System;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Shipping;

namespace Smartstore.Core.Tests.Shipping
{
    public class FixedRateTestShippingRateComputationMethod : IShippingRateComputationMethod
    {
        /// <summary>
        ///  Gets available shipping options
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Represents a response of getting shipping rate options</returns>
        Task<ShippingOptionResponse> IShippingRateComputationMethod.GetShippingOptionsAsync(ShippingOptionRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("getShippingOptionRequest");

            var response = new ShippingOptionResponse();
            response.ShippingOptions.Add(new ShippingOption
            {
                ShippingMethodId = 1,
                Name = "Shipping option 1",
                Description = "",
                Rate = 10M
            });
            response.ShippingOptions.Add(new ShippingOption
            {
                ShippingMethodId = 2,
                Name = "Shipping option 2",
                Description = "",
                Rate = 10M
            });

            return Task.FromResult(response);
        }

        /// <summary>
        /// Gets fixed shipping rate (if shipping rate computation method allows it and the rate can be calculated before checkout).
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Fixed shipping rate; or null in case there's no fixed shipping rate</returns>
        public Task<decimal?> GetFixedRateAsync(ShippingOptionRequest getShippingOptionRequest)
        {
            if (getShippingOptionRequest == null)
                throw new ArgumentNullException("getShippingOptionRequest");

            return Task.FromResult((decimal?)10M);
        }

        /// <summary>
        /// Gets a shipping rate computation method type
        /// </summary>
        public ShippingRateComputationMethodType ShippingRateComputationMethodType => ShippingRateComputationMethodType.Offline;

        /// <summary>
        /// Gets a shipment tracker
        /// </summary>
        public IShipmentTracker ShipmentTracker => null;

        public bool IsActive => true;
    }
}