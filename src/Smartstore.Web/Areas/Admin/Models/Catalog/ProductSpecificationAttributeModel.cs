namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Products.SpecificationAttributes.Fields.")]
    public class ProductSpecificationAttributeModel : EntityModelBase
    {
        public int SpecificationAttributeId { get; set; }
        public int SpecificationAttributeOptionId { get; set; }
        public string SpecificationAttributeOptionsUrl { get; set; }

        [LocalizedDisplay("*SpecificationAttribute")]
        public string SpecificationAttributeName { get; set; }

        [LocalizedDisplay("*SpecificationAttributeOption")]
        public string SpecificationAttributeOptionName { get; set; }

        [LocalizedDisplay("*AllowFiltering")]
        public bool? AllowFiltering { get; set; }

        [LocalizedDisplay("*ShowOnProductPage")]
        public bool? ShowOnProductPage { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("Admin.Catalog.Attributes.SpecificationAttributes.Fields.Essential")]
        public bool Essential { get; set; }
    }
}
