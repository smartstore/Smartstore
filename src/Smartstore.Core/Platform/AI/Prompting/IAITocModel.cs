namespace Smartstore.Core.AI.Prompting
{
    /// <summary>
    /// Represents a text generation model with table of content (TOC) properties.
    /// </summary>
    public partial interface IAITocModel
    {
        /// <summary>
        /// Gets or sets a value defining whether a table of contents should be added to the generated text.
        /// </summary>
        bool AddToc { get; set; }

        /// <summary>
        /// Gets or sets a value defining the title of the table of contents.
        /// </summary>
        string TocTitle { get; set; }

        /// <summary>
        /// Gets or sets a value defining the tag that should be used for the title of the table of contents.
        /// </summary>
        string TocTitleTag { get; set; }
    }
}
