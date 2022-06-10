namespace Smartstore.ShippingByWeight.Models
{
    [LocalizedDisplay("Plugins.Shipping.ByWeight.Fields.")]
    public class ByWeightModel : EntityModelBase
    {
        [LocalizedDisplay("*Store")]
        public int StoreId { get; set; }

        [LocalizedDisplay("*Store")]
        public string StoreName { get; set; }

        [LocalizedDisplay("*Country")]
        public int CountryId { get; set; }

        [LocalizedDisplay("*Country")]
        public string CountryName { get; set; }

        [LocalizedDisplay("*ShippingMethod")]
        public int ShippingMethodId { get; set; }

        [LocalizedDisplay("*ShippingMethod")]
        public string ShippingMethodName { get; set; }

        [LocalizedDisplay("*From")]
        public decimal From { get; set; }

        [LocalizedDisplay("*To")]
        public decimal To { get; set; }

        [LocalizedDisplay("*Zip")]
        public string Zip { get; set; }

        [LocalizedDisplay("*UsePercentage")]
        public bool UsePercentage { get; set; }

        [LocalizedDisplay("*ShippingChargePercentage")]
        public decimal ShippingChargePercentage { get; set; }

        [LocalizedDisplay("*ShippingChargeAmount")]
        public decimal ShippingChargeAmount { get; set; }

        [LocalizedDisplay("*SmallQuantitySurcharge")]
        public decimal SmallQuantitySurcharge { get; set; }

        [LocalizedDisplay("*SmallQuantityThreshold")]
        public decimal SmallQuantityThreshold { get; set; }
    }
}