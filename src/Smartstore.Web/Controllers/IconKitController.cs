using Microsoft.AspNetCore.Authorization;
using Microsoft.Net.Http.Headers;
using Smartstore.Core.Content.Media.Icons;

namespace Smartstore.Web.Controllers;

/// <summary>
/// Serves public, revisioned SVG sprites independently of storefront or admin pages.
/// </summary>
/// <param name="kits">Resolves revisions and generates cached sprites on demand.</param>
[AllowAnonymous]
public class IconKitController(IIconKitService kits) : Controller
{
    /// <summary>
    /// Serves exactly one kit. Old revisions are never replaced with current content.
    /// </summary>
    /// <param name="kit">The configured kit name.</param>
    /// <param name="revision">The output revision supplied by the renderer.</param>
    [HttpGet("/icons/{kit}-{revision:length(24)}.svg")]
    [HttpHead("/icons/{kit}-{revision:length(24)}.svg")]
    public async Task<IActionResult> Sprite(string kit, string revision, CancellationToken cancelToken)
    {
        var path = await kits.GetSpriteFileAsync(kit, revision, cancelToken);
        if (path == null)
        {
            Response.Headers.CacheControl = "no-store";
            return NotFound();
        }

        Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        // ASP.NET Core handles conditional requests, HEAD and physical file transfer.
        // The body is never materialized as a managed string or byte array here.
        return new PhysicalFileResult(path, "image/svg+xml")
        {
            EntityTag = new EntityTagHeaderValue('"' + revision + '"')
        };
    }
}
