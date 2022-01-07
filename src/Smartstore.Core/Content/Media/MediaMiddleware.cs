using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Net.Http.Headers;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.Security;
using Smartstore.Events;
using Smartstore.IO;
using Smartstore.Net;

namespace Smartstore.Core.Content.Media
{
    internal class MediaEndpoint
    {
        public RouteTemplate Template { get; init; }
        public TemplateMatcher Matcher { get; init; }
        public bool IsLegacy { get; init; }
    }
    
    public class MediaMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IApplicationContext _appContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly TemplateMatcher _matcher;

        public MediaMiddleware(
            RequestDelegate next, 
            IApplicationContext appContext, 
            IEventPublisher eventPublisher,
            IMediaStorageConfiguration mediaStorageConfiguration)
        {
            _next = next;
            _appContext = appContext;
            _eventPublisher = eventPublisher;

            // Match main URL pattern /{pub}/{id}/{path}[?{query}], e.g. '/media/234/{album}/myproduct.png?size=250'
            var mediaPublicPath = mediaStorageConfiguration.PublicPath;
            var template = TemplateParser.Parse(mediaPublicPath + "{id:int}/{**path}");
            _matcher = new TemplateMatcher(template, new RouteValueDictionary());
        }

        private bool TryMatchRoute(PathString path, out RouteValueDictionary values)
        {
            values = new RouteValueDictionary();

            if (_matcher.TryMatch(path, values))
            {
                return true;
            }

            values = null;
            return false;
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
            if (!TryMatchRoute(context.Request.Path, out var routeValues))
            {
                await _next(context);
                return;
            }

            var method = context.Request.Method;

            if (method != HttpMethods.Get && method != HttpMethods.Head)
            {
                await NotFound(null);
                return;
            }

            var mediaFileId = routeValues["id"].Convert<int>();
            var path = routeValues["path"].Convert<string>();

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
            var thumbMaxWidth = q.MaxWidth;
            var thumbMaxHeight = q.MaxHeight;
            var thumbSizeAllowed = IsThumbnailSizeAllowed(thumbMaxWidth) && (thumbMaxHeight == thumbMaxWidth || IsThumbnailSizeAllowed(thumbMaxHeight));
            if (!thumbSizeAllowed)
            {
                await NotFound(pathData.MimeType);
                return;
            }

            // Create the handler context
            var handlerContext = new MediaHandlerContext
            {
                ApplicationContext = _appContext,
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

            // Run every registered media handler to obtain a thumbnail for the requested media file
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
                    var statusMessage = isThumbExtractFail ? handlerContext.Exception.InnerException?.Message : handlerContext.Exception.Message;

                    await SendStatus(statusCode, statusMessage.EmptyNull());
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

                // Create FileStreamResult object
                var fileResult = CreateFileResult(responseFile, pathData);

                // Cache control
                ApplyResponseCaching(context, mediaSettings);

                // INFO: Although we are outside of the MVC pipeline we gonna use ActionContext anyway, because "FileStreamResult"
                // does everything we need (ByteRange, ETag etc.), so wo we gonna use it instead of reinventing the wheel.
                // A look at the MVC source code reveals that HttpContext is the only property that gets accessed, therefore we can omit 
                // all the other stuff like ActionDescriptor or ModelState (which we cannot access or create from a middleware anyway).
                await fileResult.ExecuteResultAsync(new ActionContext { HttpContext = context, RouteData = context.GetRouteData() });
            }
            finally
            {
                var imageProcessor = context.RequestServices.GetRequiredService<IImageProcessor>();
                logger.Debug("ImageProcessor TOTAL: {0} ms.", imageProcessor.TotalProcessingTimeMs);
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

        private static FileStreamResult CreateFileResult(IFile file, MediaPathData pathData)
        {
            return new FileStreamResult(file.OpenRead(), pathData.MimeType)
            {
                EnableRangeProcessing = true,
                // INFO: (core)(perf)I think ETag is sufficient and ignoring this reduces header comparison by one item.
                //LastModified = file.LastModified,
                EntityTag = new EntityTagHeaderValue(ETagUtility.GenerateETag(file))
            };
        }

        private static void ApplyResponseCaching(HttpContext context, MediaSettings mediaSettings)
        {
            var headers = context.Response.Headers;

            // Clear current cache headers
            headers.Remove(HeaderNames.CacheControl);
            headers.Remove(HeaderNames.Pragma);

            if (mediaSettings.ResponseCacheNoStore)
            {
                headers[HeaderNames.CacheControl] = "no-store";

                // Cache-control: no-store, no-cache is valid.
                if (mediaSettings.ResponseCacheLocation == ResponseCacheLocation.None)
                {
                    headers.AppendCommaSeparatedValues(HeaderNames.CacheControl, "no-cache");
                    headers[HeaderNames.Pragma] = "no-cache";
                }
            }
            else
            {
                string cacheControlValue;
                switch (mediaSettings.ResponseCacheLocation)
                {
                    case ResponseCacheLocation.Any:
                        cacheControlValue = "public,";
                        break;
                    case ResponseCacheLocation.Client:
                        cacheControlValue = "private,";
                        break;
                    case ResponseCacheLocation.None:
                        cacheControlValue = "no-cache,";
                        headers[HeaderNames.Pragma] = "no-cache";
                        break;
                    default:
                        cacheControlValue = null;
                        break;
                }

                var duration = mediaSettings.ResponseCacheDuration;
                if (duration <= 0)
                {
                    duration = 60; // 1 minute.
                }

                cacheControlValue = $"{cacheControlValue}max-age={duration}";
                headers[HeaderNames.CacheControl] = cacheControlValue;
            }
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
