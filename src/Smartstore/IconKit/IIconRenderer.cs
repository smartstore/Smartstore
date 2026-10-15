#nullable enable

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Html;

namespace Smartstore.IconKit;

/// <summary>
/// Renders resolved icons without exposing the inline or sprite choice to callers.
/// </summary>
public interface IIconRenderer
{
    /// <summary>
    /// Creates a complete SVG element, using the system placeholder for missing icons. Returns null if the placeholder is also unavailable.
    /// </summary>
    /// <param name="icon">The resolved icon identity, or null to render the missing-icon placeholder.</param>
    /// <param name="presentation">Presentation overrides; omitted values retain mapping defaults.</param>
    /// <param name="options">Rendering context and caller-owned DOM attributes.</param>
    Task<TagBuilder?> RenderAsync(IconInfo? icon, IconPresentation presentation = default, IconOptions? options = null, CancellationToken cancelToken = default);

    /// <summary>
    /// Resolves and renders a complete icon expression.
    /// </summary>
    /// <param name="expression">The icon identity and presentation.</param>
    /// <param name="options">Optional rendering context and HTML attributes.</param>
    /// <param name="cancelToken">The cancellation token.</param>
    Task<TagBuilder?> RenderAsync(IconExpression expression, IconOptions? options = null, CancellationToken cancelToken = default);

    /// <summary>
    /// Creates an HTML stack host for independently rendered icon layers.
    /// </summary>
    /// <param name="content">The already rendered icon children.</param>
    /// <param name="presentation">Stack presentation. StrokeScale applies to individual icons only.</param>
    /// <param name="options">Caller-owned DOM attributes.</param>
    TagBuilder RenderStack(IHtmlContent content, IconPresentation presentation = default, IconOptions? options = null);
}
