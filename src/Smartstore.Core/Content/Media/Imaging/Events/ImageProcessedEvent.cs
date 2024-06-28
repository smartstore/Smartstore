namespace Smartstore.Core.Content.Media.Imaging
{
    /// <summary>
    /// Published after processing finishes.
    /// </summary>
    public class ImageProcessedEvent(ProcessImageQuery query, ProcessImageResult result)
    {
        public ProcessImageQuery Query { get; private set; } = query;
        public ProcessImageResult Result { get; private set; } = result;
    }
}
