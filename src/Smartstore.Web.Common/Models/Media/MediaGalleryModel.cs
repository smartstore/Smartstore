using Smartstore.Core.Content.Media;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Media
{
    public partial class MediaGalleryModel : ModelBase
    {
        public IList<MediaFileInfo> Files { get; set; } = new List<MediaFileInfo>();
        public int GalleryStartIndex { get; set; }
        public int ThumbSize { get; set; } = MediaSettings.ThumbnailSizeXs;
        public int ImageSize { get; set; } = MediaSettings.ThumbnailSizeXl;
        public string FallbackUrl { get; set; }
        public string ThumbFallbackUrl { get; set; }

        public string ModelName { get; set; }
        public string DefaultAlt { get; set; }

        public bool BoxEnabled { get; set; }
        public bool ImageZoomEnabled { get; set; }
        public string ImageZoomType { get; set; }
    }
}
