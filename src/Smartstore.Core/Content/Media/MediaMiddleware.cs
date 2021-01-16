using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.Customers;
using Smartstore.Core.Security;
using Smartstore.Events;
using Smartstore.IO;

namespace Smartstore.Core.Content.Media
{
    public class MediaMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IEventPublisher _eventPublisher;

        public MediaMiddleware(RequestDelegate next, IEventPublisher eventPublisher)
        {
            _next = next;
            _eventPublisher = eventPublisher;
        }

        public async Task Invoke(
            HttpContext context, 
            IMediaService mediaService,
            IFolderService folderService,
            IPermissionService permissionService,
            IWorkContext workContext,
            MediaSettings mediaSettings,
            MediaHelper mediaHelper,
            Lazy<IEnumerable<IMediaHandler>> mediaHandlers,
            ILogger<MediaMiddleware> logger)
        {
            var mediaFileId = context.GetRouteValue("id")?.Convert<int>() ?? 0;
            var path = context.GetRouteValue("path")?.Convert<string>();

            if (context.Request.Method != HttpMethods.Get && context.Request.Method != HttpMethods.Head)
            {
                await NotFound(null);
                return;
            }

            var method = context.Request.Method;

            MediaFileInfo mediaFile = null;
            MediaPathData pathData = null;

            if (mediaFileId == 0)
            {
                // This is most likely a request for a default placeholder image
                pathData = new MediaPathData(path);
            }
            else if (!mediaHelper.TokenizePath(path, false, out pathData))
            {
                // Missing or malformed Uri: get file metadata from DB by id, but only when current user has media manage rights
                if (!(await permissionService.AuthorizeAsync(Permissions.Media.Update)))
                {
                    await NotFound(null);
                    return;
                }

                mediaFile = await mediaService.GetFileByIdAsync(mediaFileId, MediaLoadFlags.AsNoTracking);
                if (mediaFile == null || mediaFile.FolderId == null || mediaFile.Deleted)
                {
                    await NotFound(mediaFile?.MimeType);
                    return;
                }

                pathData = new MediaPathData(folderService.GetNodeById(mediaFile.FolderId.Value), mediaFile.Name)
                {
                    Extension = mediaFile.Extension,
                    MimeType = mediaFile.MimeType
                };
            }

            var q = await CreateImageQuery(context, pathData.MimeType, pathData.Extension);

            // Security: check allowed thumnail sizes and return 404 if disallowed.
            var thumbSizeAllowed = IsThumbnailSizeAllowed(q.MaxWidth) && (q.MaxHeight == q.MaxWidth || IsThumbnailSizeAllowed(q.MaxHeight));
            if (!thumbSizeAllowed)
            {
                await NotFound(pathData.MimeType);
                return;
            }

            var handlerContext = new MediaHandlerContext
            {
                HttpContext = context,
                CurrentCustomer = workContext.CurrentCustomer,
                PermissionService = permissionService,
                MediaFileId = mediaFileId,
                RawPath = path,
                MediaService = mediaService,
                PathData = pathData,
                ImageQuery = q
            };
            handlerContext.SetSourceFile(mediaFile);

            var handlers = mediaHandlers.Value.OrderBy(x => x.Order).ToArray();

            IMediaHandler currentHandler;
            for (var i = 0; i < handlers.Length; i++)
            {
                currentHandler = handlers[i];

                // Execute handler
                await currentHandler.ExecuteAsync(handlerContext);

                if (handlerContext.Exception != null)
                {
                    var isThumbExtractFail = handlerContext.Exception is ExtractThumbnailException;
                    var statusCode = isThumbExtractFail ? StatusCodes.Status204NoContent : StatusCodes.Status500InternalServerError;
                    var statusMessage = isThumbExtractFail ? handlerContext.Exception.InnerException?.Message.EmptyNull() : handlerContext.Exception.Message;

                    await SendStatus(statusCode, statusMessage);
                    return;
                }

                if (handlerContext.Executed || handlerContext.ResultFile != null)
                {
                    // Get out if the handler produced a result file or has been executed in any way
                    break;
                }
            }

            try
            {
                var responseFile = handlerContext.ResultFile ?? await handlerContext.GetSourceFileAsync();
                if (responseFile == null || !responseFile.Exists)
                {
                    await NotFound(pathData.MimeType);
                    return;
                }

                if (string.Equals(responseFile.Extension, "." + pathData.Extension, StringComparison.CurrentCultureIgnoreCase))
                {
                    pathData.MimeType = MimeTypes.MapNameToMimeType(responseFile.Extension);
                }

                // TODO: (core) cache-control
                var fileResult = CreateFileResult(responseFile, pathData);
                var actionContext = new ActionContext
                {
                    HttpContext = context,
                    RouteData = context.GetRouteData()
                };
                await fileResult.ExecuteResultAsync(actionContext);
            }
            finally
            {
                var imageProcessor = context.RequestServices.GetRequiredService<IImageProcessor>();
                Debug.WriteLine("ImageProcessor TOTAL: {0} ms.".FormatCurrent(imageProcessor.TotalProcessingTimeMs));
            }

            #region Functions

            bool IsThumbnailSizeAllowed(int? size)
            {
                return size.GetValueOrDefault() == 0
                    || mediaSettings.IsAllowedThumbnailSize(size.Value)
                    || permissionService.Authorize(Permissions.Media.Update, workContext.CurrentCustomer);
            }

            async Task NotFound(string mime)
            {
                context.Response.ContentType = mime.NullEmpty() ?? "text/html";
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("404: Not Found");
            }

            async Task SendStatus(int code, string message)
            {
                context.Response.StatusCode = code;
                await context.Response.WriteAsync(message);
            }

            #endregion
        }

        private FileStreamResult CreateFileResult(IFile file, MediaPathData pathData)
        {
            return new FileStreamResult(file.OpenRead(), pathData.MimeType)
            {
                EnableRangeProcessing = true,
                LastModified = file.LastModified,
                EntityTag = GenerateETag(file)
            };
        }

        private static EntityTagHeaderValue GenerateETag(IFile file)
        {
            // TODO: (core) Make extension methods for IFile, FileInfo etc.
            var len = file.Length;
            var last = file.LastModified;
            var lastModified = new DateTimeOffset(last.Year, last.Month, last.Day, last.Hour, last.Minute, last.Second, last.Offset).ToUniversalTime();
            long etagHash = lastModified.ToFileTime() ^ len;

            return new EntityTagHeaderValue('\"' + Convert.ToString(etagHash, 16) + '\"');
        }

        private async Task<ProcessImageQuery> CreateImageQuery(HttpContext context, string mimeType, string extension)
        {
            if (extension == "svg")
            {
                return new ProcessImageQuery { Format = "svg" };
            }

            // TODO: (mc) implement "raw" image handling later
            //if (qs.GetValues(null).Contains("raw", StringComparer.OrdinalIgnoreCase) || qs["raw"] != null)
            //{
            //	return null;
            //}

            var query = new ProcessImageQuery(null, context.Request.Query);

            //if (query.Quality == null)
            //{
            //    query.Quality = _mediaSettings.DefaultImageQuality;
            //}

            await _eventPublisher.PublishAsync(new ImageQueryCreatedEvent(query, context, mimeType, extension));

            return query;
        }
    }
}
