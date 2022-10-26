using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Net.Http.Headers;
using Smartstore.Data;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Media middleware that handles redirection from legacy media URLs to main media route.
    /// </summary>
    public class MediaLegacyMiddleware
    {
        const string IdToken = "id";
        const string TenantToken = "tenant";
        const string PathToken = "path";
        const string DefaultTenantName = "default";

        private readonly List<TemplateMatcher> _matchers = new(3);

        public MediaLegacyMiddleware(RequestDelegate next, IMediaStorageConfiguration mediaStorageConfiguration)
        {
            _matchers.AddRange(BuildTemplateMatchers(mediaStorageConfiguration.PublicPath));
        }

        private static IEnumerable<TemplateMatcher> BuildTemplateMatchers(string publicPath)
        {
            // INFO: TemplateMatcher cannot handle inline constraints at this stage. We gonna check ourselves.
            var endpoints = new[]
            {
                // Match legacy URL pattern /media/uploaded/{path}[?{query}], e.g. '/media/uploaded/subfolder/image.png' 
                publicPath + "uploaded/{**path}",

                // Match legacy URL pattern /media/{tenant=default}/uploaded/{path}[?{query}], e.g. '/media/default/uploaded/subfolder/image.png' 
                publicPath + "{tenant:regex(^default$)}/uploaded/{**path}",

                // Match legacy URL pattern /media/image/{id}/{path}[?{query}], e.g. '/media/image/234/myproduct.png?size=250' 
                publicPath + "image/{id:int}/{**path}"
            };

            return endpoints.Select(x => new TemplateMatcher(TemplateParser.Parse(x), new RouteValueDictionary()));
        }

        private bool TryMatchRoute(PathString path, out int? id, out string remainingPath)
        {
            remainingPath = null;
            id = null;

            var values = new RouteValueDictionary();

            // media/uploaded/*
            if (_matchers[0].TryMatch(path, values))
            {
                remainingPath = (string)values[PathToken];
                return true;
            }
            
            // media/{tenant}/uploaded/*
            if (_matchers[1].TryMatch(path, values))
            {
                if (values.TryGetValueAs<string>(TenantToken, out var tenant) && 
                    (tenant.EqualsNoCase(DefaultTenantName) || tenant.EqualsNoCase(DataSettings.Instance.TenantName)))
                {
                    remainingPath = (string)values[PathToken];
                    return true;
                }
            }

            // media/image/{id}/*
            if (_matchers[2].TryMatch(path, values))
            {
                if (values.TryGetAndConvertValue<int>(IdToken, out var value))
                {
                    id = value;
                    remainingPath = (string)values[PathToken];
                    return true;
                }
            }

            return false;
        }

        public async Task Invoke(
            HttpContext context,
            Lazy<IMediaService> mediaService,
            Lazy<IMediaUrlGenerator> mediaUrlGenerator)
        {
            if (context.Request.Method != HttpMethods.Get && context.Request.Method != HttpMethods.Head)
            {
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                return;
            }

            if (!TryMatchRoute(context.Request.Path, out var mediaFileId, out var path))
            {
                await NotFound(null);
                return;
            }

            if (path.IsEmpty())
            {
                // Cannot operate without path
                await NotFound(null);
                return;
            }


            MediaFileInfo mediaFile;

            if (mediaFileId.HasValue)
            {
                // Redirect legacy URL "/{pub}/image/234/file.png" to "/{pub}/234/catalog/path/to/file.png"
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
                await NotFound(mediaFile?.MimeType);
                return;
            }
            else
            {
                // Redirect to new location
                context.Response.StatusCode = context.Connection.IsLocal() ? 302 : 301;
                context.Response.Headers[HeaderNames.Location] = url;
            }

            async Task NotFound(string mime)
            {
                context.Response.ContentType = mime.NullEmpty() ?? "text/html";
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("404: Not Found");
            }
        }
    }
}