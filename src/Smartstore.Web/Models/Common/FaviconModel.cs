namespace Smartstore.Web.Models.Common
{
    public partial class FaviconModel : ModelBase
    {
        public string FavIconUrl { get; set; }
        public List<Favicon> PngIcons { get; set; } = new();
        public List<Favicon> AppleTouchIcons { get; set; } = new();
        public string MsTileIconUrl { get; set; }
        public string MsTileColor { get; set; }

        public class Favicon
        {
            /// <summary>
            /// Represents value for link attribute sizes (e.g. 16x16).
            /// </summary>
            public string Size { get; set; }

            /// <summary>
            /// Represents complete icon URL.
            /// </summary>
            public string Url { get; set; }
        }
    }
}
