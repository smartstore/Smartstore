using System;
using Microsoft.AspNetCore.Http;
using Smartstore.Http;

namespace Smartstore.Pdf.WkHtml
{
    // TODO: (core) SendAuthCookie (?)
    internal class WkUrlInput : IPdfInput
    {
        private readonly string _url;
        private readonly WkHtmlToPdfOptions _options;
        private readonly HttpContext _httpContext;

        public WkUrlInput(string url, WkHtmlToPdfOptions options, HttpContext httpContext)
        {
            _url = url;
            _options = options;
            // Can be null
            _httpContext = httpContext;
        }

        public PdfInputKind Kind => PdfInputKind.Url;

        public string Content
        {
            get
            {
                if (_url.Contains(Uri.SchemeDelimiter))
                {
                    return _url;
                }
                else if (_options.BaseUrl != null)
                {
                    var url = _url;
                    if (url.StartsWith('~'))
                    {
                        url = WebHelper.ToAbsolutePath(url);
                    }

                    return _options.BaseUrl.ToString() + url.TrimStart('/');
                }
                else if (_httpContext?.Request != null)
                {
                    return WebHelper.GetAbsoluteUrl(_url, _httpContext.Request);
                }

                return _url;
            }
        }

        public void Teardown()
        {
            // Noop
        }
    }
}
