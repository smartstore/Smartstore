namespace Smartstore.Core.AI.Prompting
{
    /// <summary>
    /// Represents a model for text generation with keyword properties.
    /// </summary>
    public partial interface IAIKeywordModel
    {
        /// <summary>
        /// Gets or sets a value defining the keywords to use while generating the text.
        /// </summary>
        string Keywords { get; set; }

        /// <summary>
        /// Gets or sets a value defining the keywords to avoid while generating the text.
        /// </summary>
        string KeywordsToAvoid { get; set; }

        /// <summary>
        /// Gets or sets a value defining whether to make the keywords bold in the generated text.
        /// </summary>
        bool MakeKeywordsBold { get; set; }
    }
}
