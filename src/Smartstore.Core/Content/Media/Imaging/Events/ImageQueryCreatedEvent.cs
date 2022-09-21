using Microsoft.AspNetCore.Http;

namespace Smartstore.Core.Content.Media.Imaging
{
    /// <summary>
    /// Published after image query has been created and initialized
    /// by the media middleware with data from HttpContent.Request.QueryString.
    /// This event implies that a thumbnail is about to be created.
    /// </summary>
    public class ImageQueryCreatedEvent
    {
        public ImageQueryCreatedEvent(ProcessImageQuery query, HttpContext httpContext, string mimeType, string extension)
        {
            Query = query;
            HttpContext = httpContext;
            MimeType = mimeType;
            Extension = extension;
        }

        public ProcessImageQuery Query { get; init; }
        public HttpContext HttpContext { get; init; }
        public string MimeType { get; init; }
        public string Extension { get; init; }
    }
}