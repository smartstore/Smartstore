using Smartstore.Events;

namespace Smartstore.Core.Content.Media.Imaging
{
    /// <summary>
    /// Published after processing finishes.
    /// </summary>
    public class ImageProcessedEvent : IEventMessage
    {
        public ImageProcessedEvent(ProcessImageQuery query, ProcessImageResult result)
        {
            Query = query;
            Result = result;
        }

        public ProcessImageQuery Query { get; private set; }
        public ProcessImageResult Result { get; private set; }
    }
}
