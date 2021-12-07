namespace Smartstore.ShippingByWeight.Settings
{
    public class ShippingByWeightSettings : ISettings
    {
        /// <summary>
        /// TODO: (mh) (core) Add docs
        /// </summary>
        public bool LimitMethodsToCreated { get; set; }

        /// <summary>
        /// TODO: (mh) (core) Add docs
        /// </summary>
        public bool CalculatePerWeightUnit { get; set; }

        /// <summary>
        /// Specifies whether to include the weight of free shipping products in shipping calculation.
        /// </summary>
        public bool IncludeWeightOfFreeShippingProducts { get; set; } = true;
    }
}