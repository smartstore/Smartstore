using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Filters a request by the size of uploaded files according to <see cref="MediaSettings.MaxUploadFileSize"/>.
    /// </summary>
    public sealed class MaxMediaFileSizeAttribute : TypeFilterAttribute
    {
        public MaxMediaFileSizeAttribute()
            : base(typeof(MaxMediaFileSizeFilter))
        {
        }

        class MaxMediaFileSizeFilter : IActionFilter
        {
            private readonly MediaSettings _mediaSettings;
            private readonly MediaExceptionFactory _exceptionFactory;

            public MaxMediaFileSizeFilter(MediaSettings mediaSettings, MediaExceptionFactory exceptionFactory)
            {
                _mediaSettings = mediaSettings;
                _exceptionFactory = exceptionFactory;
            }

            public void OnActionExecuting(ActionExecutingContext context)
            {
                var request = context.HttpContext.Request;

                if (!request.HasFormContentType)
                {
                    return;
                }

                var numFiles = request.Form.Files.Count;
                if (numFiles <= 0)
                {
                    return;
                }

                long maxBytes = 1024 * _mediaSettings.MaxUploadFileSize;
                for (var i = 0; i < numFiles; ++i)
                {
                    var file = request.ToPostedFileResult();
                    if (file.Size > maxBytes)
                    {
                        throw _exceptionFactory.MaxFileSizeExceeded(file.FileName, file.Size, maxBytes);
                    }
                }
            }

            public void OnActionExecuted(ActionExecutedContext context)
            {
            }
        }
    }
}
