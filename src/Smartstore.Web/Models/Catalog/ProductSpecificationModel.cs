using System;
using Smartstore.Core.Localization;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Catalog
{
    public partial class ProductSpecificationModel : ModelBase
    {
        public int SpecificationAttributeId { get; set; }
        public LocalizedValue<string> SpecificationAttributeName { get; set; }
        public LocalizedValue<string> SpecificationAttributeOption { get; set; }
    }
}
