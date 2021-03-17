using System.Collections.Generic;
using Smartstore.Core.Content.Media;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Common
{
    public partial class FaviconModel : ModelBase
    {
        public string FavIconThumbnail { get; set; }
        public List<FaviconThumbnail> PngIconThumbnails { get; set; } = new();
        public List<FaviconThumbnail> AppleTouchIconThumbnails { get; set; } = new();
        public string MsTileIconThumbnail { get; set; }
        public string MsTileColor { get; set; }
    }

    public class FaviconThumbnail
    {
        /// <summary>
        /// Represents value for link attribute sizes (e.g. 16x16).
        /// </summary>
        public string Size { get; set; }

        /// <summary>
        /// Represents complete thumbnail URL.
        /// </summary>
        public string Url { get; set; }
    }
}
