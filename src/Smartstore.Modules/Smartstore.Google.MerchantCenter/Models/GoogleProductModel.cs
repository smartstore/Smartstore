using Smartstore.Core.Catalog.Products;

namespace Smartstore.Google.MerchantCenter.Models
{
    [CustomModelPart]
    [LocalizedDisplay("Plugins.Feed.Froogle.")]
    public class GoogleProductModel : EntityModelBase
    {
        [LocalizedDisplay("Admin.Catalog.Products.Fields.ID")]
        public int ProductId { get; set; }

        [LocalizedDisplay("*Products.ProductName")]
        public string Name { get; set; }

        [LocalizedDisplay("Admin.Catalog.Products.Fields.Sku")]
        public string Sku { get; set; }

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
        public string GenderLocalized { get; set; }

        [UIHint("AgeGroup")]
        [LocalizedDisplay("*AgeGroup")]
        public string AgeGroup { get; set; }
        public string AgeGroupLocalized { get; set; }

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
        public string IsBundleLocalized { get; set; }

        [LocalizedDisplay("*IsAdult")]
        public bool? IsAdult { get; set; }
        public string IsAdultLocalized { get; set; }

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
    }
}
