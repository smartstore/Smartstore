using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.Security;
using Smartstore.Events;
using Smartstore.IO;
using Smartstore.Net;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Media middleware that handles redirection from legacy media URLs to main media route.
    /// </summary>
    public class MediaLegacyMiddleware
    {
        public async Task Invoke(HttpContext context, IMediaService mediaService, IMediaUrlGenerator mediaUrlGenerator)
        {
            var path = context.GetRouteValueAs<string>("path");
            if (path.IsEmpty())
            {
                // Cannot operate without path
                context.Response.ContentType = "text/html";
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("404: Not Found");
                return;
            }
            
            var mediaFileId = context.GetRouteValueAs<int?>("id");

            MediaFileInfo mediaFile = null;

            if (mediaFileId.HasValue)
            {
                /// Redirect legacy URL "/{pub}/image/234/file.png" to "/{pub}/234/catalog/path/to/file.png"
                mediaFile = await mediaService.GetFileByIdAsync(mediaFileId.Value, MediaLoadFlags.AsNoTracking);
            }
            else
            {
                // Redirect legacy URL "/{tenant?}/uploaded/some/file.png" to "/file/1234/some/file.png"
                mediaFile = await mediaService.GetFileByPathAsync(
                    SystemAlbumProvider.Files + "/" + path, 
                    MediaLoadFlags.AsNoTracking);
            }

            var url = mediaUrlGenerator.GenerateUrl(mediaFile, context.Request.QueryString, string.Empty, false);

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