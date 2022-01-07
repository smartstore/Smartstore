using Smartstore.Core.Localization;
using Smartstore.Web.Models.Media;

namespace Smartstore.Web.Models.Catalog
{
    public partial class CategorySummaryModel : EntityModelBase
    {
        public LocalizedValue<string> Name { get; set; }
        public string Url { get; set; }
        public ImageModel Image { get; set; } = new ImageModel();

        // TODO: Badges
    }
}
