namespace Smartstore.Core.Platform.AI.Prompting
{
    /// <summary>
    /// Represents a text generation model with table of content (TOC) properties.
    /// </summary>
    public partial interface ITableOfContentsGenerationPrompt
    {
        /// <summary>
        /// Gets or sets a value defining whether a table of contents should be added to the generated text.
        /// </summary>
        bool AddTableOfContents { get; set; }

        /// <summary>
        /// Gets or sets a value defining the title of the table of contents.
        /// </summary>
        string TableOfContentsTitle { get; set; }

        /// <summary>
        /// Gets or sets a value defining the tag that should be used for the title of the table of contents.
        /// </summary>
        string TableOfContentsTitleTag { get; set; }
    }
}
