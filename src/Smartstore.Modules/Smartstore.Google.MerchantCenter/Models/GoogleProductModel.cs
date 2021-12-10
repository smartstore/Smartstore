using Smartstore.Core.Catalog.Products;

namespace Smartstore.Google.MerchantCenter.Models
{
    [CustomModelPart]
    [LocalizedDisplay("Plugins.Feed.Froogle.")]
    public class GoogleProductModel : EntityModelBase
    {
        public int ProductId { get; set; }

        [LocalizedDisplay("*Products.ProductName")]
        public string Name { get; set; }

        public string SKU { get; set; }
        public int ProductTypeId { get; set; }
        public ProductType ProductType => (ProductType)ProductTypeId;
        public string ProductTypeName { get; set; }
        public string ProductTypeLabelHint
        {
            get
            {
                return ProductType switch
                {
                    ProductType.SimpleProduct => "secondary d-none",
                    ProductType.GroupedProduct => "success",
                    ProductType.BundledProduct => "info",
                    _ => string.Empty,
                };
            }
        }

        [UIHint("Taxonomy")]
        [LocalizedDisplay("*Products.GoogleCategory")]
        public string Taxonomy { get; set; }

        [UIHint("Gender")]
        [LocalizedDisplay("*Gender")]
        public string Gender { get; set; }

        [UIHint("AgeGroup")]
        [LocalizedDisplay("*AgeGroup")]
        public string AgeGroup { get; set; }

        [LocalizedDisplay("*Color")]
        public string Color { get; set; }

        [LocalizedDisplay("*Size")]
        public string Size { get; set; }

        [LocalizedDisplay("*Material")]
        public string Material { get; set; }

        [LocalizedDisplay("*Pattern")]
        public string Pattern { get; set; }

        [LocalizedDisplay("Common.Export")]
        public bool Export { get; set; }

        [LocalizedDisplay("*Multipack")]
        public int Multipack { get; set; }

        [LocalizedDisplay("*IsBundle")]
        public bool? IsBundle { get; set; }

        [LocalizedDisplay("*IsAdult")]
        public bool? IsAdult { get; set; }

        [UIHint("EnergyEfficiencyClass")]
        [LocalizedDisplay("*EnergyEfficiencyClass")]
        public string EnergyEfficiencyClass { get; set; }

        [LocalizedDisplay("*CustomLabel0")]
        public string CustomLabel0 { get; set; }

        [LocalizedDisplay("*CustomLabel1")]
        public string CustomLabel1 { get; set; }

        [LocalizedDisplay("*CustomLabel2")]
        public string CustomLabel2 { get; set; }

        [LocalizedDisplay("*CustomLabel3")]
        public string CustomLabel3 { get; set; }

        [LocalizedDisplay("*CustomLabel4")]
        public string CustomLabel4 { get; set; }

        public string GenderLocalize { get; set; }
        public string AgeGroupLocalize { get; set; }
        public string Export2Localize { get; set; }
        public string IsBundleLocalize { get; set; }
        public string IsAdultLocalize { get; set; }
    }
}
