namespace Smartstore.Core.Platform.AI.Chat
{
    /// <summary>
    /// Interface to be implemented by all image generation prompts.
    /// </summary>
    public interface IImageGenerationPrompt
    {
        string EntityName { get; set; }
        string TargetProperty { get; set; }

        string Medium { get; set; }
        string Environment { get; set; }
        string Lighting { get; set; }
        string Color { get; set; }
        string Mood { get; set; }
        string Composition { get; set; }
    }
}
