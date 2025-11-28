using Smartstore.Imaging;

namespace Smartstore.Core.AI.Prompting
{
    /// <summary>
    /// Represents a model for AI image generation.
    /// </summary>
    public interface IAIImageModel
    {
        /// <summary>
        /// Specifies the name of the entity.
        /// </summary>
        string EntityName { get; }

        string Prompt { get; }
        string TargetProperty { get; }

        /// <summary>
        /// The name of the AI model.
        /// </summary>
        /// <example>dall-e-3</example>
        string ModelName { get; }

        /// <summary>
        /// Specifies an image style.
        /// </summary>
        string Style { get; }

        #region Prompt engineering

        /// <summary>
        /// E.g. photo, painting, illustration
        /// </summary>
        string Medium { get; }

        /// <summary>
        /// E.g. shop, indoors, outdoors, living room, kitchen, city, forest, beach, pedestal, etc
        /// </summary>
        string Environment { get; }

        /// <summary>
        /// soft, ambient, overcast, neon, studio lights, etc
        /// </summary>
        string Lighting { get; }

        /// <summary>
        /// vibrant, muted, bright, monochromatic, colorful, black and white, pastel
        /// </summary>
        string Color { get; }

        /// <summary>
        /// Sedate, calm, raucous, energetic
        /// Gemütlich, hektisch, entspannend, geheimnisvoll, nostalgisch, futuristisch.
        /// </summary>
        string Mood { get; }

        /// <summary>
        /// Portrait, headshot, closeup, birds-eye view ???
        /// </summary>
        string Composition { get; }

        #endregion

        #region Image editing

        /// <summary>
        /// Gets the IDs of the source files used for image editing.
        /// </summary>
        int[] SourceFileIds { get; }

        #endregion

        #region Image response/output

        /// <summary>
        /// Specifies the image orientation.
        /// </summary>
        ImageOrientation Orientation { get; }

        /// <summary>
        /// Gets the aspect ratio of the image, if specified.
        /// </summary>
        ImageAspectRatio? AspectRatio { get; }

        /// <summary>
        /// Gets the resolution setting used for generating the AI image, if specified.
        /// </summary>
        AIImageResolution? Resolution { get; }

        /// <summary>
        /// Gets the format to use for image output.
        /// </summary>
        /// <remarks>If not set, the default output format will be used. The selected format may affect
        /// image quality, file size, and compatibility with downstream consumers.</remarks>
        AIImageOutputFormat? OutputFormat { get; }

        #endregion
    }
}
