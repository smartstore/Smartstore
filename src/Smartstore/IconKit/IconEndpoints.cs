using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Smartstore.IconKit;

/// <summary>
/// Serves public, revisioned SVG sprites independently of storefront or admin pages.
/// </summary>
/// <param name="context">The current HTTP request.</param>
/// <param name="kits">Resolves revisions and generates cached sprites on demand.</param>
/// <param name="icons">Resolves requested icon addresses and modifiers.</param>
/// <param name="renderer">Creates kit-backed or inline SVG output.</param>
/// <param name="browser">Prepares picker sprites and searches available sources.</param>
internal class IconEndpoints(HttpContext context, IIconKitService kits, IIconService icons, IIconRenderer renderer, IIconBrowser browser)
{
    internal HttpResponse Response => context.Response;

    /// <summary>
    /// Serves exactly one kit. Old revisions are never replaced with current content.
    /// </summary>
    /// <param name="kit">The configured kit name.</param>
    /// <param name="revision">The output revision supplied by the renderer.</param>
    public async Task<IResult> Kit(string kit, string revision, CancellationToken cancelToken)
    {
        var path = await kits.GetSpriteFileAsync(kit, revision, cancelToken);
        if (path == null)
        {
            Response.Headers.CacheControl = "no-store";
            return Results.NotFound();
        }

        Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        // ASP.NET Core handles conditional requests, HEAD and physical file transfer.
        // The body is never materialized as a managed string or byte array here.
        return Results.File(path, "image/svg+xml", entityTag: new EntityTagHeaderValue('"' + revision + '"'));
    }

    /// <summary>
    /// Serves an immutable browser resolution manifest.
    /// </summary>
    /// <param name="revision">The content fingerprint embedded in the page.</param>
    public async Task<IResult> Manifest(string revision, CancellationToken cancelToken)
    {
        var path = await kits.GetManifestFileAsync(revision, cancelToken);
        if (path == null)
        {
            Response.Headers.CacheControl = "no-store";
            return Results.NotFound();
        }

        Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return Results.File(path, "application/json", entityTag: new EntityTagHeaderValue('"' + revision + '"'));
    }

    /// <summary>
    /// Returns one Select2 page after preparing the selected kit or full-variant sprite.
    /// </summary>
    /// <param name="kit">The optional selected kit.</param>
    /// <param name="lib">The selected library when browsing a variant.</param>
    /// <param name="variant">The selected library variant.</param>
    /// <param name="term">The optional search words.</param>
    /// <param name="page">The one-based page number.</param>
    public async Task<IResult> Browse(string kit, string lib, string variant, string term, int page = 1, CancellationToken cancelToken = default)
    {
        Response.Headers.CacheControl = "no-store";
        const int pageSize = 50;
        if (page < 1 || page > int.MaxValue / pageSize || term?.Length > 512)
        {
            return Results.BadRequest();
        }

        try
        {
            var result = await browser.SearchAsync(new IconSearchQuery
            {
                Library = lib, Variant = variant, Term = term, Skip = (page - 1) * pageSize, Take = pageSize
            }, kit, cancelToken);
            if (result == null)
            {
                return Results.NotFound();
            }

            return Results.Json(new
            {
                spriteUrl = result.SpriteUrl,
                external = result.Items.Select((item, i) => (item, i)).Where(x => x.item.IsExternal).Select(x => x.i).ToArray(),
                results = result.Items.Select(x => new
                {
                    id = x.Value, text = x.Name, address = x.Address, previewAddress = x.PreviewAddress,
                    library = x.LibraryKey, variant = x.VariantKey, inlineName = x.InlineName, supportsStrokeScaling = x.SupportsStrokeScaling, isDuotone = x.IsDuotone
                }),
                pagination = new { more = (long)page * pageSize < result.TotalCount },
                total = result.TotalCount
            });
        }
        catch (ArgumentException)
        {
            return Results.BadRequest();
        }
        catch (InvalidDataException)
        {
            return Results.BadRequest();
        }
    }

    /// <summary>
    /// Serves a complete variant sprite from the independent icon browser file cache.
    /// </summary>
    /// <param name="library">The registered library selector.</param>
    /// <param name="variant">The registered variant selector.</param>
    /// <param name="revision">The exact sprite content revision.</param>
    public async Task<IResult> BrowserSprite(string library, string variant, string revision, CancellationToken cancelToken)
    {
        var path = await browser.GetSpriteFileAsync(library, variant, revision, cancelToken);
        if (path == null)
        {
            Response.Headers.CacheControl = "no-store";
            return Results.NotFound();
        }

        Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return Results.File(path, "image/svg+xml", entityTag: new EntityTagHeaderValue('"' + revision + '"'));
    }

    /// <summary>
    /// Renders a single requested icon; presentation-only attributes remain client-side.
    /// </summary>
    /// <param name="name">The address, including optional query modifiers.</param>
    /// <param name="lib">The optional library selector.</param>
    /// <param name="variant">The optional variant selector.</param>
    /// <param name="rotate">An explicit rotation override.</param>
    /// <param name="flipH">An explicit horizontal flip override.</param>
    /// <param name="flipV">An explicit vertical flip override.</param>
    /// <param name="strokeScale">An explicit stroke multiplier override.</param>
    public async Task<IResult> Render(string name, string lib, string variant, int? rotate,
        bool? flipH, bool? flipV, double? strokeScale, string area = null, CancellationToken cancelToken = default)
    {
        Response.Headers.CacheControl = "no-store";
        if (string.IsNullOrEmpty(name) || name.Length > 512)
        {
            return Results.BadRequest();
        }

        try
        {
            var icon = await icons.GetIconAsync(name, lib, variant, area, cancelToken);
            var svg = await renderer.RenderAsync(icon, new IconOptions
            {
                Area = area, RequestedName = name, Rotate = rotate, FlipHorizontal = flipH, FlipVertical = flipV, StrokeScale = strokeScale
            }, cancelToken);
            if (svg == null)
            {
                return Results.NotFound();
            }

            using var writer = new StringWriter();
            svg.WriteTo(writer, HtmlEncoder.Default);
            var bytes = Encoding.UTF8.GetBytes(writer.ToString());
            Response.Headers.CacheControl = "public,no-cache";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return Results.File(bytes, "image/svg+xml", entityTag: new EntityTagHeaderValue('"' + Convert.ToHexStringLower(SHA256.HashData(bytes)) + '"'));
        }
        catch (ArgumentException)
        {
            return Results.BadRequest();
        }
        catch (FormatException)
        {
            return Results.BadRequest();
        }
        catch (InvalidDataException)
        {
            return Results.BadRequest();
        }
    }
}
