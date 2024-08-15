#nullable enable

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Platform.AI;
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
        /// <returns>
        /// The icon button inclusive dropdown to choose the target property to be translated.
        /// <c>null</c> if there is no active <see cref="IAIProvider"/>.
        /// </returns>
        TagBuilder? GenerateTranslationTool(ILocalizedModel model);

        /// <summary>
        /// Creates the icon button to open the simple text creation dialog.
        /// </summary>
        /// <param name="attributes">The attributes of the <see cref="TagHelper"/>.</param>
        /// <param name="hasContent">
        /// Indicates whether the target property already has content.
        /// If it has, we can offer options like: summarize, optimize etc.
        /// </param>
        /// <returns>
        /// The icon button inclusive dropdown to choose a rewrite command from.
        /// <c>null</c> if there is no active <see cref="IAIProvider"/>.
        /// </returns>
        TagBuilder? GenerateTextCreationTool(AttributeDictionary? attributes, bool hasContent);

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
        /// Creates the icon button to open the rich text creation dialog.
        /// </summary>
        /// <param name="attributes">The attributes of the <see cref="TagHelper"/>.</param>
        /// <returns>
        /// The icon button to open the rich text creation dialog.
        /// <c>null</c> if there is no active <see cref="IAIProvider"/>.
        /// </returns>
        TagBuilder? GenerateRichTextTool(AttributeDictionary? attributes);
    }
}
