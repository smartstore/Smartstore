using Smartstore.Imaging;

namespace Smartstore.Core.Content.Media.Imaging
{
    /// <summary>
    /// Published before processing begins, but after the source has been loaded.
    /// </summary>
    public class ImageProcessingEvent(ProcessImageQuery query, IImage image)
    {
        public ProcessImageQuery Query { get; private set; } = query;
        public IImage Image { get; private set; } = image;
    }
}
