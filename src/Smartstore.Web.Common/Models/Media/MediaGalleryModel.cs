using System.ComponentModel;
using Smartstore.Core.Content.Media;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Media
{
    public partial class MediaGalleryModel : ModelBase
    {
        [DefaultValue("[]")]
        public IList<MediaFileInfo> Files { get; set; } = [];
        public int GalleryStartIndex { get; set; }

        [DefaultValue(MediaSettings.ThumbnailSizeXs)]
        public int ThumbSize { get; set; } = MediaSettings.ThumbnailSizeXs;

        [DefaultValue(MediaSettings.ThumbnailSizeXl)]
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
