using Microsoft.AspNetCore.Html;

namespace Smartstore.Web.Rendering;

/// <summary>
/// Contains extensions for rendering entity picker models.
/// </summary>
public static class EntityPickerModelExtensions
{
    /// <summary>
    /// Highlights all occurrences of the entity picker search term in a title while HTML-encoding the title.
    /// </summary>
    /// <param name="model">The entity picker model.</param>
    /// <param name="title">The title to render.</param>
    /// <returns>The encoded title with optional highlight markup.</returns>
    public static IHtmlContent HighlightTitle(this EntityPickerModel model, string title)
    {
        Guard.NotNull(model);

        var result = new HtmlContentBuilder();

        if (!model.HighlightSearchTerm || model.SearchTerm.IsEmpty() || title.IsEmpty())
        {
            return result.Append(title);
        }

        var startIndex = 0;

        while (startIndex < title.Length)
        {
            var matchIndex = title.IndexOf(model.SearchTerm, startIndex, StringComparison.OrdinalIgnoreCase);

            if (matchIndex < 0)
            {
                result.Append(title[startIndex..]);
                break;
            }

            result.Append(title[startIndex..matchIndex])
                .AppendHtml("<span class=\"highlight\">")
                .Append(title.Substring(matchIndex, model.SearchTerm.Length))
                .AppendHtml("</span>");

            startIndex = matchIndex + model.SearchTerm.Length;
        }

        return result;
    }
}