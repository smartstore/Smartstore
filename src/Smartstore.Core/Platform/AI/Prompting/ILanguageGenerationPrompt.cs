namespace Smartstore.Core.Platform.AI.Prompting
{
    /// <summary>
    /// Represents a text generation model with language properties.
    /// </summary>
    public partial interface ILanguageGenerationPrompt
    {
        /// <summary>
        /// Gets or sets a value defining the id of the choosen language.
        /// </summary>
        int LanguageId { get; set; }
    }
}
