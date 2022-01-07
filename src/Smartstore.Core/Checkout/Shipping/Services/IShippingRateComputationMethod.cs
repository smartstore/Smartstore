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
        /// Gets fixed shipping rate (if the shipping rate computation method allows it and the shipping rate can be calculated before checkout).
        /// </summary>
        /// <param name="request">Get shipping options request.</param>
        /// <remarks>The returned currency is ignored.</remarks>
        /// <returns>Fixed shipping rate. Or <c>null</c> if there is no fixed shipping rate.</returns>
        Task<decimal?> GetFixedRateAsync(ShippingOptionRequest request);

        /// <summary>
        /// Gets a shipment tracker
        /// </summary>
        IShipmentTracker ShipmentTracker { get; }

        /// <summary>
        /// Gets a value indicating whether the shipping rate computation method is active and should be offered to customers
        /// </summary>
        bool IsActive { get; }
    }
}