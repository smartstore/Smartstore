namespace Smartstore.Shipping.Models
{
    [LocalizedDisplay("Plugins.Shipping.FixedRateShipping.Fields.")]
    public class FixedRateModel
    {
        public int ShippingMethodId { get; set; }

        [LocalizedDisplay("*ShippingMethodName")]
        public string ShippingMethodName { get; set; }

        [LocalizedDisplay("*Rate")]
        public decimal Rate { get; set; }
    }
}
