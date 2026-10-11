#nullable enable

using Microsoft.AspNetCore.Http;

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Resolves the icon area using the existing request area detection.
/// </summary>
public class IconAreaResolver : IIconAreaResolver
{
    /// <inheritdoc />
    public virtual string ResolveArea(HttpContext? context)
        => context?.Request.IsAdminArea() == true
            ? "admin" : "public";
}
