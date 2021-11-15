using Smartstore.Core.Configuration;

namespace Smartstore.Shipping.Settings
{
    // INFO: (mh) (core) New convention: Settings --> Configuration
    public class ShippingByTotalSettings : ISettings
    {
        public bool LimitMethodsToCreated { get; set; }
        public bool CalculateTotalIncludingTax { get; set; } = true;
        public decimal SmallQuantityThreshold { get; set; }
        public decimal SmallQuantitySurcharge { get; set; }
    }
}
