using System;
using Smartstore.Core.Localization;
using Smartstore.Web.Modelling;
using Smartstore.Web.Models.Media;

namespace Smartstore.Web.Models.Catalog
{
    public partial class CategorySummaryModel : EntityModelBase
    {
        public LocalizedValue<string> Name { get; set; }
        public string Url { get; set; }
        public PictureModel PictureModel { get; set; } = new PictureModel();

        // TODO: Badges
    }
}
