namespace Smartstore.Core.Platform.AI.Prompting
{
    /// <summary>
    /// Represents a text generation model with link properties.
    /// </summary>
    public partial interface ILinkGenerationPrompt
    {
        /// <summary>
        /// Gets or sets a value defining the id of the choosen language.
        /// </summary>
        string AnchorText { get; set; }

        /// <summary>
        /// Gets or sets a value defining the id of the choosen language.
        /// </summary>
        string AnchorLink { get; set; }

        /// <summary>
        /// Gets or sets a value defining the id of the choosen language.
        /// </summary>
        bool AddCallToAction { get; set; }

        /// <summary>
        /// Gets or sets a value defining the id of the choosen language.
        /// </summary>
        string CallToActionText { get; set; }
    }
}
