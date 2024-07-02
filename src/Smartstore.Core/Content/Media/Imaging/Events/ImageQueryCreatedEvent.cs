using Microsoft.AspNetCore.Http;

namespace Smartstore.Core.Content.Media.Imaging
{
    /// <summary>
    /// Published after image query has been created and initialized
    /// by the media middleware with data from HttpContent.Request.QueryString.
    /// This event implies that a thumbnail is about to be created.
    /// </summary>
    public class ImageQueryCreatedEvent(
        ProcessImageQuery query, 
        HttpContext httpContext, 
        string mimeType, 
        string extension)
    {
        public ProcessImageQuery Query { get; init; } = query;
        public HttpContext HttpContext { get; init; } = httpContext;
        public string MimeType { get; init; } = mimeType;
        public string Extension { get; init; } = extension;
    }
}