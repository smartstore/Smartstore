using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Smartstore.Http;
using Smartstore.IO;

namespace Smartstore.Pdf.WkHtml
{
    // TODO: (core) SendAuthCookie (?)
    internal class WkFileInput : IPdfInput
    {
        private readonly string _urlOrPath;
        private readonly WkHtmlToPdfOptions _options;
        private readonly HttpContext _httpContext;

        public WkFileInput(string urlOrPath, WkHtmlToPdfOptions options, HttpContext httpContext)
        {
            _urlOrPath = urlOrPath;
            _options = options;
            // Can be null
            _httpContext = httpContext;
        }

        public PdfInputKind Kind => PdfInputKind.File;

        public string Content
        {
            get
            {
                if (Path.IsPathFullyQualified(_urlOrPath) || _urlOrPath.Contains(Uri.SchemeDelimiter))
                {
                    return _urlOrPath;
                }
                else if (_options.BaseUrl != null)
                {
                    var url = _urlOrPath;
                    if (url.StartsWith('~'))
                    {
                        url = WebHelper.ToAbsolutePath(url);
                    }

                    return _options.BaseUrl.ToString() + PathUtility.NormalizeRelativePath(url);
                }
                else if (_httpContext?.Request != null)
                {
                    return WebHelper.GetAbsoluteUrl(_urlOrPath, _httpContext.Request);
                }

                return _urlOrPath;
            }
        }

        public void Teardown()
        {
            // Noop
        }
    }
}
