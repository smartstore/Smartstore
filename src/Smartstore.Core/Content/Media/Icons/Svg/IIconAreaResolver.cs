#nullable enable

using Microsoft.AspNetCore.Http;

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Supplies the default icon area for the current web request.
/// </summary>
public interface IIconAreaResolver
{
    /// <summary>
    /// Resolves the current icon area, defaulting to public outside the Admin area.
    /// </summary>
    /// <param name="context">The current request context, or null outside a request.</param>
    string ResolveArea(HttpContext? context);
}
