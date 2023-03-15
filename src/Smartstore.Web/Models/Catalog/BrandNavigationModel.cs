using Smartstore.Caching;
using Smartstore.Core.Localization;
using Smartstore.Web.Models.Media;

namespace Smartstore.Web.Models.Catalog
{
    public partial class BrandNavigationModel : ModelBase, ICacheEvents
    {
        public List<BrandBriefInfoModel> Brands { get; set; } = new();
        public bool DisplayAllBrandsLink { get; set; }
        public bool DisplayBrands { get; set; }
        public bool DisplayImages { get; set; }
        public bool HideBrandDefaultPictures { get; set; }
        public int BrandThumbImageSize { get; set; }

        void ICacheEvents.OnCache()
        {
            MemoryCacheStore.TryDropLazyLoader(Brands);
        }

        void ICacheEvents.OnRemoved(IMemoryCacheStore sender, CacheEntryRemovedReason reason)
        {
        }
    }

    public partial class BrandBriefInfoModel : EntityModelBase, ICacheEvents
    {
        public LocalizedValue<string> Name { get; set; }
        public string SeName { get; set; }
        public int DisplayOrder { get; set; }
        public ImageModel Image { get; set; }

        void ICacheEvents.OnCache()
        {
            MemoryCacheStore.TryDropLazyLoader(Image);
        }

        void ICacheEvents.OnRemoved(IMemoryCacheStore sender, CacheEntryRemovedReason reason)
        {
        }
    }
}
