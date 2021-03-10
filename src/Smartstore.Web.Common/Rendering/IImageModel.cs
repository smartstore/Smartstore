using Smartstore.Core.Content.Media;

namespace Smartstore.Web.Rendering
{
    /// <summary>
    /// Image model contract.
    /// </summary>
    public partial interface IImageModel
    {
        MediaFileInfo File { get; }
        public int? ThumbSize { get; }
        public string Alt { get; }
        public string Title { get; }
        public bool NoFallback { get; }
        public string Host { get; }
    }
}
