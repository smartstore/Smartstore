#nullable enable

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
        public required int[] SourceFileIds { get; set; }

        /// <summary>
        /// The image format of the generated AI image.
        /// </summary>
        public AIImageFormat ImageFormat { get; set; }

        /// <summary>
        /// The temporary file path of the current AI-generated image.
        /// </summary>
        public string? CurrentImagePath { get; set; }
    }
}
