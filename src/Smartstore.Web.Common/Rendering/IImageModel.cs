#nullable enable

using System.Drawing;
using Smartstore.Core.Content.Media;

namespace Smartstore.Web.Rendering
{
    /// <summary>
    /// Represents a simple POCO, cacheable variant of an image media file.
    /// </summary>
    public partial interface IImageModel
    {
        MediaFile? File { get; }
        Size PixelSize { get; }

        public string? Alt { get; }
        public string? Title { get; }

        public string? Url { get; }
        public string? ThumbUrl { get; }
        public int? ThumbSize { get; }

        public bool NoFallback { get; }
        public string? Host { get; }
    }
}
