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

        // TODO: (mg) Unfortunately, CurrentImagePath is not sufficient due to the regenerate button (prompt to image association).
        // The image path associated with the prompt must be sent with each request.
        //public string? CurrentImagePath { get; set; }
    }
}
