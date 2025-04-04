#nullable enable

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.AI;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Rendering
{
    /// <summary>
    /// Represents a generator for creating the HTML for AI dialog openers.
    /// </summary>
    public partial interface IAIToolHtmlGenerator : IViewContextAware
    {
        /// <summary>
        /// Creates the button to open the translation dialog.
        /// </summary>
        /// <param name="model">The localized model to be translated.</param>
        /// <param name="localizedEditorName">The unique name of the localized editor.</param>
        /// <returns>
        /// The icon button with a drop-down list to select the target property to be translated.
        /// <c>null</c> if there is no active <see cref="IAIProvider"/>.
        /// </returns>
        TagBuilder? GenerateTranslationTool(ILocalizedModel model, string localizedEditorName);

        /// <summary>
        /// Creates the icon button and the commands dropdown menu to open the simple text creation dialog.
        /// </summary>
        /// <param name="attributes">The attributes of the <see cref="TagHelper"/>.</param>
        /// <param name="enabled">A value indicating whether to initially enable the command dropdown items (e.g. optimize, change-tone, etc.).</param>
        /// <returns>
        /// The icon button inclusive dropdown to choose a rewrite command from.
        /// <c>null</c> if there is no active <see cref="IAIProvider"/>.
        /// </returns>
        TagBuilder? GenerateTextCreationTool(AttributeDictionary? attributes, bool enabled = true);

        /// <summary>
        /// Creates the icon button and the commands dropdown menu to open the rich text creation dialog.
        /// </summary>
        /// <param name="attributes">The attributes of the <see cref="TagHelper"/>.</param>
        /// <param name="enabled">A value indicating whether to initially enable the command dropdown items (e.g. optimize, change-tone, etc.).</param>
        /// <returns>
        /// The icon button to open the rich text creation dialog.
        /// <c>null</c> if there is no active <see cref="IAIProvider"/>.
        /// </returns>
        TagBuilder? GenerateRichTextTool(AttributeDictionary? attributes, bool enabled = true);

        /// <summary>
        /// Creates the icon button to open the suggestion dialog.
        /// </summary>
        /// <param name="attributes">The attributes of the <see cref="TagHelper"/>.</param>
        /// <returns>
        /// The icon button to open the suggestion dialog.
        /// <c>null</c> if there is no active <see cref="IAIProvider"/>.
        /// </returns>
        TagBuilder? GenerateSuggestionTool(AttributeDictionary? attributes);

        /// <summary>
        /// Creates the icon button to open the image creation dialog.
        /// </summary>
        /// <param name="attributes">The attributes of the <see cref="TagHelper"/>.</param>
        /// <returns>
        /// The icon button to open the image creation dialog.
        /// <c>null</c> if there is no active <see cref="IAIProvider"/>.
        /// </returns>
        TagBuilder? GenerateImageCreationTool(AttributeDictionary? attributes);

        /// <summary>
        /// Generates the text optimizer dropdown items for the text optimizer dropdown menu.
        /// </summary>
        /// <param name="forChatDialog">Whether the dropdown is rendered within the chat dialog.</param>
        /// <param name="enabled">A value indicating whether to initially enable the command dropdown items.</param>
        /// <param name="forHtmlEditor">A value indicating whether to enable HTML editor specific commands such as 'continue'.</param>
        /// <returns>The HTML content.</returns>
        IHtmlContent GenerateOptimizeCommands(bool forChatDialog, bool enabled = true, bool forHtmlEditor = false);

        /// <summary>
        /// Gets the URL of the dialog.
        /// </summary>
        /// <param name="topic">The <see cref="AIChatTopic"/> of the dialog.</param>
        string GetDialogUrl(AIChatTopic topic);
    }
}
