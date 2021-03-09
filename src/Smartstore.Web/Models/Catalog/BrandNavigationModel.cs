using System;
using System.Collections.Generic;
using Smartstore.Core.Localization;
using Smartstore.Web.Modelling;
using Smartstore.Web.Models.Media;

namespace Smartstore.Web.Models.Catalog
{
    public partial class BrandNavigationModel : ModelBase
    {
        public List<BrandBriefInfoModel> Brands { get; set; } = new();
        public bool DisplayAllBrandsLink { get; set; }
        public bool DisplayBrands { get; set; }
        public bool DisplayImages { get; set; }
        public bool HideBrandDefaultPictures { get; set; }
        public int BrandThumbPictureSize { get; set; }
    }

    public partial class BrandBriefInfoModel : EntityModelBase
    {
        public LocalizedValue<string> Name { get; set; }
        public string SeName { get; set; }
        public int DisplayOrder { get; set; }
        public PictureModel Picture { get; set; }
    }
}
