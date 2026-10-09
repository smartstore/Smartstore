#nullable enable

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Html;

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Renders resolved icons without exposing the inline or sprite choice to callers.
/// </summary>
public interface IIconRenderer
{
    /// <summary>
    /// Creates a complete SVG element, or null if the source is no longer available.
    /// </summary>
    /// <param name="icon">The resolved icon identity.</param>
    /// <param name="options">Caller-owned DOM attributes and presentation options.</param>
    Task<TagBuilder?> RenderAsync(IconInfo icon, IconOptions? options = null, CancellationToken cancelToken = default);

    /// <summary>
    /// Creates an HTML stack host for independently rendered icon layers.
    /// </summary>
    /// <param name="content">The already rendered icon children.</param>
    /// <param name="options">Stack presentation and caller-owned DOM attributes. StrokeScale applies to individual icons only.</param>
    TagBuilder RenderStack(IHtmlContent content, IconOptions? options = null);
}
