namespace Smartstore.Core.Platform.AI.Prompting
{
    /// <summary>
    /// Represents an image generation prompt.
    /// </summary>
    public interface IImageGenerationPrompt
    {
        string Prompt { get; }
        string EntityName { get; }
        string TargetProperty { get; }

        string Medium { get; }
        string Environment { get; }
        string Lighting { get; }
        string Color { get; }
        string Mood { get; }
        string Composition { get; }

        public AIImageFormat Format { get; }
        public ImageCreationStyle Style { get; }
    }
}
