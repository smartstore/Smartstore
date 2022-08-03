using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Filters a request by the size of uploaded files according to <see cref="MediaSettings.MaxUploadFileSize"/>.
    /// </summary>
    public sealed class MaxMediaFileSizeAttribute : TypeFilterAttribute
    {
        private readonly long? _maxSize;

        public MaxMediaFileSizeAttribute()
            : base(typeof(MaxMediaFileSizeFilter))
        {
            Arguments = new object[] { this };
        }

        /// <param name="maxSize">
        /// Overrides the the maximum allowed size (in KB) of an uploaded media file for a particular action. 
        /// If <c>null</c>, uses <see cref="MediaSettings.MaxUploadFileSize"/>.
        /// </param>
        public MaxMediaFileSizeAttribute(long maxSize)
            : this()
        {
            _maxSize = maxSize;
        }

        internal long? MaxUploadFileSize
        {
            get => _maxSize;
        }

        class MaxMediaFileSizeFilter : IAuthorizationFilter, IRequestFormLimitsPolicy
        {
            private readonly MaxMediaFileSizeAttribute _attribute;
            private readonly MediaSettings _mediaSettings;

            public MaxMediaFileSizeFilter(
                MaxMediaFileSizeAttribute attribute,
                MediaSettings mediaSettings)
            {
                _attribute = attribute;
                _mediaSettings = mediaSettings;
            }

            public void OnAuthorization(AuthorizationFilterContext context)
            {
                var maxFileSize = 1024 * (_attribute.MaxUploadFileSize ?? _mediaSettings.MaxUploadFileSize);

                var effectiveFormPolicy = context.FindEffectivePolicy<IRequestFormLimitsPolicy>();
                if (effectiveFormPolicy == null || effectiveFormPolicy == this)
                {
                    var features = context.HttpContext.Features;
                    var formFeature = features.Get<IFormFeature>();

                    if (formFeature == null || formFeature.Form == null)
                    {
                        // Request form has not been read yet, so set the limits
                        var formOptions = new FormOptions
                        {
                            MultipartBodyLengthLimit = maxFileSize
                        };

                        features.Set<IFormFeature>(new FormFeature(context.HttpContext.Request, formOptions));
                    }
                }

                var effectiveRequestSizePolicy = context.FindEffectivePolicy<IRequestSizePolicy>();
                if (effectiveRequestSizePolicy == null || effectiveRequestSizePolicy == this)
                {
                    //  Will only be available when running OutOfProcess with Kestrel
                    var maxRequestBodySizeFeature = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();

                    if (maxRequestBodySizeFeature != null && !maxRequestBodySizeFeature.IsReadOnly)
                    {
                        maxRequestBodySizeFeature.MaxRequestBodySize = maxFileSize;
                    }
                }
            }

            //public void OnActionExecuting(ActionExecutingContext context)
            //{
            //    var request = context.HttpContext.Request;

            //    if (!request.HasFormContentType)
            //    {
            //        return;
            //    }

            //    var numFiles = request.Form.Files.Count;
            //    if (numFiles <= 0)
            //    {
            //        return;
            //    }

            //    long maxBytes = 1024 * _mediaSettings.MaxUploadFileSize;
            //    foreach (var file in request.Form.Files)
            //    {
            //        if (file.Length > maxBytes)
            //        {
            //            throw _exceptionFactory.MaxFileSizeExceeded(file.FileName, file.Length, maxBytes);
            //        }
            //    }
            //}

            //public void OnActionExecuted(ActionExecutedContext context)
            //{
            //}
        }
    }
}
