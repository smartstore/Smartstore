using Smartstore.Core.Content.Media;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Common
{
    public partial class FaviconModel : ModelBase
    {
        public MediaFileInfo FavIcon { get; set; }
        public MediaFileInfo AppleTouchIcon { get; set; }
        public MediaFileInfo PngIcon { get; set; }
        public MediaFileInfo MsTileIcon { get; set; }
        public string MsTileColor { get; set; }
    }
}
