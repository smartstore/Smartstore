#nullable enable

namespace Smartstore.AI.Metadata
{
    /// <summary>
    /// Default model mapping used when no explicit selection is provided.
    /// </summary>
    public class AIModelDefaults
    {
        /// <summary>
        /// Default text model ID.
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// Default image model ID.
        /// </summary>
        public string? Image { get; set; }

        /// <summary>
        /// Default vision (image analysis) model ID.
        /// </summary>
        public string? Vision { get; set; }
    }
}
