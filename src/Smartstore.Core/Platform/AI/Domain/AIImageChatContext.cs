#nullable enable

using System.Text.Json.Serialization;
using Smartstore.Imaging;
using Smartstore.Json.Converters;

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

        [JsonConverter(typeof(TypeConverterJsonConverter<ImageAspectRatio>))]
        public ImageAspectRatio? AspectRatio { get; init; }

        [JsonConverter(typeof(TypeConverterJsonConverter<AIImageResolution>))]
        public AIImageResolution? Resolution { get; init; }

        [JsonConverter(typeof(TypeConverterJsonConverter<AIImageOutputFormat>))]
        public AIImageOutputFormat? OutputFormat { get; init; }
    }
}
