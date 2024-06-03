namespace Smartstore.Core.Platform.AI.Prompting
{
    /// <summary>
    /// Represents a text generation model with the possibility to include images.
    /// </summary>
    public partial interface IIncludeImagesGenerationPrompt
    {
        /// <summary>
        /// Gets or sets a value defining whether images should be included in the generation.
        /// </summary>
        bool IncludeImages { get; set; }
    }
}
