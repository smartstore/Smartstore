using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Products.SpecificationAttributes.Fields.")]
    public class ProductSpecificationAttributeModel : EntityModelBase
    {
        public int SpecificationAttributeId { get; set; }
        public int SpecificationAttributeOptionId { get; set; }
        public string SpecificationAttributeOptionsUrl { get; set; }

        // TODO: (mh) (core) Remove after review.
        // INFO: (mh) (core) Obsolete > Option will be loaded via ajax now.
        //public List<SpecificationAttributeOption> SpecificationAttributeOptions { get; set; } = new();

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

        // TODO: (mh) (core) Remove after review. See comment above for infos.
        //public partial class SpecificationAttributeOption : EntityModelBase
        //{
        //    public int id { get; set; }
        //    public string name { get; set; }
        //    public string text { get; set; }
        //}
    }
}
