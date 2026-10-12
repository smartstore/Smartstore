#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Smartstore.IconKit;

namespace Smartstore.Bootstrapping;

/// <summary>
/// Maps public icon endpoints independently of MVC controllers.
/// </summary>
public static class IconKitEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps manifest, sprite, rendering and picker routes under the configured route prefix.
    /// </summary>
    /// <param name="endpoints">The application's endpoint route builder.</param>
    public static RouteGroupBuilder MapIconKit(this IEndpointRouteBuilder endpoints)
    {
        Guard.NotNull(endpoints);

        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<IconKitOptions>>().Value;
        var group = endpoints.MapGroup(options.RoutePrefix).AllowAnonymous();

        group.MapMethods("/{kit}-{revision:length(24)}.svg", ["GET", "HEAD"],
            (HttpContext context, string kit, string revision, CancellationToken cancelToken) =>
                Create(context).Kit(kit, revision, cancelToken)).WithName("IconKit.Kit");

        group.MapMethods("/manifest/{revision:length(24)}.json", ["GET", "HEAD"],
            (HttpContext context, string revision, CancellationToken cancelToken) =>
                Create(context).Manifest(revision, cancelToken)).WithName("IconKit.Manifest");

        group.MapMethods("/browser/{library}/{variant}/{revision:length(24)}.svg", ["GET", "HEAD"],
            (HttpContext context, string library, string variant, string revision, CancellationToken cancelToken) =>
                Create(context).BrowserSprite(library, variant, revision, cancelToken)).WithName("IconKit.BrowserSprite");

        group.MapGet("/browser/search",
            (HttpContext context, string? kit, string? lib, string? variant, string? term, int? page, CancellationToken cancelToken) =>
                Create(context).Browse(kit, lib, variant, term, page ?? 1, cancelToken)).WithName("IconKit.Browse");

        group.MapGet("/render",
            (HttpContext context, string? name, string? lib, string? variant, int? rotate, bool? flipH, bool? flipV,
                double? strokeScale, string? area, CancellationToken cancelToken) =>
                Create(context).Render(name, lib, variant, rotate, flipH, flipV, strokeScale, area, cancelToken)).WithName("IconKit.Render");

        return group;
    }

    private static IconEndpoints Create(HttpContext context)
    {
        var services = context.RequestServices;
        return new IconEndpoints(context, services.GetRequiredService<IIconKitService>(), services.GetRequiredService<IIconService>(),
            services.GetRequiredService<IIconRenderer>(), services.GetRequiredService<IIconBrowser>());
    }
}
