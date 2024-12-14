namespace Smartstore.Core.AI.Prompting
{
    /// <summary>
    /// Represents a model for text generation with language properties.
    /// </summary>
    public partial interface IAILanguageModel
    {
        /// <summary>
        /// Gets or sets a value defining the id of the choosen language.
        /// </summary>
        int LanguageId { get; set; }
    }
}
