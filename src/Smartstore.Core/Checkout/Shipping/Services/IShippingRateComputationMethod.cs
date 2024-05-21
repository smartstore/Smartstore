using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Shipping
{
    /// <summary>
    /// Provides an interface for shipping rate computation method
    /// </summary>
    public partial interface IShippingRateComputationMethod : IProvider, IUserEditable
    {
        /// <summary>
        /// Gets a shipping rate computation method type
        /// </summary>
        ShippingRateComputationMethodType ShippingRateComputationMethodType { get; }

        /// <summary>
        /// Gets available shipping options
        /// </summary>
        /// <param name="request">Get shipping options request</param>
        /// <returns>Get shipping options response</returns>
        Task<ShippingOptionResponse> GetShippingOptionsAsync(ShippingOptionRequest request);

        /// <summary>
        /// Gets a shipment tracker
        /// </summary>
        IShipmentTracker ShipmentTracker { get; }
    }
}