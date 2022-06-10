namespace Smartstore.ShippingByWeight.Models
{
    [LocalizedDisplay("Plugins.Shipping.ByWeight.Fields.")]
    public class ByWeightListModel : ModelBase
    {
        [LocalizedDisplay("*LimitMethodsToCreated")]
        public bool LimitMethodsToCreated { get; set; }

        [LocalizedDisplay("*CalculatePerWeightUnit")]
        public bool CalculatePerWeightUnit { get; set; }

        [LocalizedDisplay("*IncludeWeightOfFreeShippingProducts")]
        public bool IncludeWeightOfFreeShippingProducts { get; set; }
    }
}
