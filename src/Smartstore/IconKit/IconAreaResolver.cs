#nullable enable

using Microsoft.AspNetCore.Http;

namespace Smartstore.IconKit;

public class IconAreaResolver : IIconAreaResolver
{
    /// <inheritdoc />
    public virtual string ResolveArea(HttpContext? context)
        => context?.Request.IsAdminArea() == true
            ? "admin" : "public";
}
