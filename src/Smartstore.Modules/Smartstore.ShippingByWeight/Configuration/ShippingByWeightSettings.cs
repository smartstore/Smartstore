namespace Smartstore.ShippingByWeight.Settings
{
    public class ShippingByWeightSettings : ISettings
    {
        /// <summary>
        /// Defines whether there will be a fallback to free shipping if no shipping rate record matches are found.
        /// </summary>
        public bool LimitMethodsToCreated { get; set; }

        /// <summary>
        /// Defines whether the shipping fee will be multiplied with total weight of the order.
        /// </summary>
        public bool CalculatePerWeightUnit { get; set; }

        /// <summary>
        /// Specifies whether to include the weight of free shipping products in shipping calculation.
        /// </summary>
        public bool IncludeWeightOfFreeShippingProducts { get; set; } = true;
    }
}