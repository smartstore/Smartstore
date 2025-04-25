namespace Smartstore.Shipping.Settings
{
    public class ShippingByTotalSettings : ISettings
    {
        public bool LimitMethodsToCreated { get; set; }
        public bool CalculateTotalIncludingTax { get; set; } = true;
        public decimal SmallQuantityThreshold { get; set; }
        public decimal SmallQuantitySurcharge { get; set; }
    }
}
