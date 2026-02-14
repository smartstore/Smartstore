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
    /// Specifies the location or context in which an AI command is rendered.
    /// </summary>
    public enum AICommandLocation
    {
        /// <summary>
        /// Command rendering location is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// The command is rendered in a simple text or textarea field.
        /// </summary>
        TextInput,

        /// <summary>
        /// The command is rendered in an HTML input control.
        /// </summary>
        HtmlInput,

        /// <summary>
        /// The command is rendered in the text chat dialog.
        /// </summary>
        ChatDialog
    }
    
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
        /// <param name="location">Specifies the location where the menu is rendered.</param>
        /// <param name="forHtml">Whether the text input is HTML formatted.</param>
        /// <param name="enabled">Whether to initially enable the command dropdown items.</param>
        /// <returns>The HTML content.</returns>
        IHtmlContent GenerateOptimizeCommands(AICommandLocation location, bool forHtml = false, bool enabled = true);

        /// <summary>
        /// Gets the URL of the dialog.
        /// </summary>
        /// <param name="topic">The <see cref="AIChatTopic"/> of the dialog.</param>
        string GetDialogUrl(AIChatTopic topic);
    }
}
