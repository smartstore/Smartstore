using Smartstore.Caching;
using Smartstore.Core.Content.Media;
using Smartstore.Web.Modelling;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.Models.Media
{
    public partial class ImageModel : EntityModelBase, IImageModel, ICacheEvents
    {
        public override int Id
        {
            get => File?.Id ?? base.Id;
            set => base.Id = value;
        }

        public MediaFileInfo File { get; set; }
        public int? ThumbSize { get; set; }
        public string Alt { get; set; }
        public string Title { get; set; }
        public bool NoFallback { get; set; }
        public string Host { get; set; }

        public bool HasImage()
        {
            return File != null || !NoFallback;
        }

        void ICacheEvents.OnCache()
        {
            MemoryCacheStore.TryDropLazyLoader(File?.File);
        }

        void ICacheEvents.OnRemoved(IMemoryCacheStore sender, CacheEntryRemovedReason reason)
        {
        }
    }
}
