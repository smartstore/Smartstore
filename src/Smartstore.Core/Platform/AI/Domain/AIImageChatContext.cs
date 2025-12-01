#nullable enable

using Smartstore.Imaging;

namespace Smartstore.Core.AI
{
    /// <summary>
    /// Represents information for AI image chat operations.
    /// </summary>
    public class AIImageChatContext
    {
        /// <summary>
        /// The identifier(s) of the source files used to generate an AI image.
        /// </summary>
        public required int[] SourceFileIds { get; init; }

        public ImageOrientation Orientation { get; init; }

        public ImageAspectRatio? AspectRatio { get; init; }
        public AIImageResolution? Resolution { get; init; }
        public AIImageOutputFormat? OutputFormat { get; init; }
    }
}
