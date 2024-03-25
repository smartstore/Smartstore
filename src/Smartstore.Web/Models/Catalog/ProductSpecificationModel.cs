using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Catalog
{
    public partial class ProductSpecificationModel : ModelBase
    {
        public int SpecificationAttributeId { get; set; }
        public LocalizedValue<string> SpecificationAttributeName { get; set; }
        public LocalizedValue<string> SpecificationAttributeOption { get; set; }
        public bool Essential { get; set; }
        public int DisplayOrder { get; set; }
    }
}
