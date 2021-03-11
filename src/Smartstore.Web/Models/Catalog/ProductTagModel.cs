using System;
using Smartstore.Core.Localization;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Catalog
{
    public partial class ProductTagModel : EntityModelBase
    {
        public LocalizedValue<string> Name { get; set; }
        public string Slug { get; set; }
        public int ProductCount { get; set; }
    }
}
