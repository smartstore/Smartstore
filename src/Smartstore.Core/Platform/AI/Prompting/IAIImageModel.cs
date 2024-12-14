namespace Smartstore.Core.AI.Prompting
{
    /// <summary>
    /// Represents a model for AI image generation.
    /// </summary>
    public interface IAIImageModel
    {
        /// <summary>
        /// Defines the name of the entity.
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
        /// e.g. photo, painting, illustration
        /// </summary>
        string Medium { get; }

        /// <summary>
        /// e.g. shop, indoors, outdoors, living room, kitchen, city, forest, beach, pedestal, etc
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

        /// <summary>
        /// Defines the image creation format.
        /// </summary>
        AIImageFormat Format { get; }

        /// <summary>
        /// Defines the image creation style directly passed to ChatGPT.
        /// </summary>
        /// <remarks>Currently only used by ChatGPT.</remarks>
        ImageCreationStyle Style { get; }
    }
}
