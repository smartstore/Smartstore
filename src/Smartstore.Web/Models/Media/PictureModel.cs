using System;
using Smartstore.Core.Content.Media;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Media
{
    public partial class PictureModel : ModelBase
    {
        public MediaFileInfo File { get; set; }
        public int? ThumbSize { get; set; }
        public string Alt { get; set; }
        public string Title { get; set; }
        public bool NoFallback { get; set; }
        public string Host { get; set; }

        public bool HasPicture
        {
            get => File != null || !NoFallback;
        }
    }
}
