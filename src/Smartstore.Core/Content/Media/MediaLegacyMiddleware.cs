using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Net.Http.Headers;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Media middleware that handles redirection from legacy media URLs to main media route.
    /// </summary>
    public class MediaLegacyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly List<TemplateMatcher> _matchers = new(3);

        public MediaLegacyMiddleware(RequestDelegate next, IMediaStorageConfiguration mediaStorageConfiguration)
        {
            _next = next;
            _matchers.AddRange(BuildTemplateMatchers(mediaStorageConfiguration.PublicPath));
        }

        private static IEnumerable<TemplateMatcher> BuildTemplateMatchers(string publicPath)
        {
            var endpoints = new[]
            {
                // Match legacy URL pattern /{pub}/uploaded/{path}[?{query}], e.g. '/media/uploaded/subfolder/image.png' 
                publicPath + "uploaded/{**path}",

                // Match legacy URL pattern /{pub}/{tenant=default}/uploaded/{path}[?{query}], e.g. '/media/default/uploaded/subfolder/image.png' 
                publicPath + "{tenant:regex(^default$)}/uploaded/{**path}",

                // Match legacy URL pattern /{pub}/image/{id}/{path}[?{query}], e.g. '/media/image/234/myproduct.png?size=250' 
                publicPath + "image/{id:int}/{**path}"
            };

            return endpoints.Select(x => new TemplateMatcher(TemplateParser.Parse(x), new RouteValueDictionary()));
        }

        private bool TryMatchRoute(PathString path, out RouteValueDictionary values)
        {
            values = new RouteValueDictionary();

            foreach (var matcher in _matchers)
            {
                if (matcher.TryMatch(path, values))
                {
                    return true;
                }
            }

            values = null;
            return false;
        }

        public async Task Invoke(
            HttpContext context, 
            Lazy<IMediaService> mediaService, 
            Lazy<IMediaUrlGenerator> mediaUrlGenerator)
        {
            if (!TryMatchRoute(context.Request.Path, out var routeValues))
            {
                await _next(context);
                return;
            }

            var path = (string)routeValues["path"];
            if (path.IsEmpty())
            {
                // Cannot operate without path
                context.Response.ContentType = "text/html";
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("404: Not Found");
                return;
            }
            
            var mediaFileId = (int?)routeValues["id"];

            MediaFileInfo mediaFile;

            if (mediaFileId.HasValue)
            {
                /// Redirect legacy URL "/{pub}/image/234/file.png" to "/{pub}/234/catalog/path/to/file.png"
                mediaFile = await mediaService.Value.GetFileByIdAsync(mediaFileId.Value, MediaLoadFlags.AsNoTracking);
            }
            else
            {
                // Redirect legacy URL "/{tenant?}/uploaded/some/file.png" to "/file/1234/some/file.png"
                mediaFile = await mediaService.Value.GetFileByPathAsync(
                    SystemAlbumProvider.Files + "/" + path, 
                    MediaLoadFlags.AsNoTracking);
            }

            var url = mediaUrlGenerator.Value.GenerateUrl(mediaFile, context.Request.QueryString, string.Empty, false);

            if (url.IsEmpty())
            {
                // Cannot redirect, return 404
                context.Response.ContentType = mediaFile?.MimeType ?? "text/html";
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("404: Not Found");
            } 
            else
            {
                // Redirect to new location
                context.Response.StatusCode = context.Connection.IsLocal() ? 302 : 301;
                context.Response.Headers[HeaderNames.Location] = path;
            } 
        }
    }
}