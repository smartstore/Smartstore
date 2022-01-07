using Smartstore.Core.Localization;
using Smartstore.Web.Models.Media;

namespace Smartstore.Web.Models.Catalog
{
    public partial class BrandOverviewModel : EntityModelBase
    {
        public LocalizedValue<string> Name { get; set; }
        public LocalizedValue<string> Description { get; set; }
        public string SeName { get; set; }
        public ImageModel Image { get; set; }
    }
}
