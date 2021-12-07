namespace Smartstore.Tax.Models
{
    [LocalizedDisplay("Plugins.Tax.CountryStateZip.Fields.")]
    public class ByRegionTaxRateModel : EntityModelBase
    {
        [LocalizedDisplay("*TaxCategory")]
        public int TaxCategoryId { get; set; }
        [LocalizedDisplay("*TaxCategory")]
        public string TaxCategoryName { get; set; }

        [LocalizedDisplay("*Country")]
        public int CountryId { get; set; }
        [LocalizedDisplay("*Country")]
        public string CountryName { get; set; }

        [LocalizedDisplay("*StateProvince")]
        public int StateProvinceId { get; set; }
        [LocalizedDisplay("*StateProvince")]
        public string StateProvinceName { get; set; }

        [LocalizedDisplay("*Zip")]
        public string Zip { get; set; }

        [LocalizedDisplay("*Percentage")]
        public decimal Percentage { get; set; }
    }
}
