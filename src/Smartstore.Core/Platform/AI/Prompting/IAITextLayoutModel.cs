namespace Smartstore.Core.AI.Prompting
{
    /// <summary>
    /// Represents a text generation model with structure/layout properties.
    /// </summary>
    public partial interface IAITextLayoutModel
    {
        /// <summary>
        /// Gets or sets a value defining whether to include an intro.
        /// </summary>
        bool IncludeIntro { get; set; }

        /// <summary>
        /// Gets or sets a value defining the main heading tag.
        /// </summary>
        string MainHeadingTag { get; set; }

        /// <summary>
        /// Gets or sets a value defining the paragraph count.
        /// </summary>
        int ParagraphCount { get; set; }

        /// <summary>
        /// Gets or sets a value defining the paragraph heading tag.
        /// </summary>
        string ParagraphHeadingTag { get; set; }

        /// <summary>
        /// Gets or sets a value defining the paragraph word count.
        /// </summary>
        int ParagraphWordCount { get; set; }

        /// <summary>
        /// Gets or sets a value defining whether to include a conclusion.
        /// </summary>
        bool IncludeConclusion { get; set; }
    }
}
