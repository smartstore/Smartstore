namespace Smartstore.Tax.Models
{
    [LocalizedDisplay("Plugins.Tax.FixedRate.Fields.")]
    public class FixedTaxRateModel
    {
        public int TaxCategoryId { get; set; }

        [LocalizedDisplay("*TaxCategoryName")]
        public string TaxCategoryName { get; set; }

        [LocalizedDisplay("*Rate")]
        public decimal Rate { get; set; }
    }
}
