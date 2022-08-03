using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http;
using Smartstore.Http;
using Smartstore.IO;

namespace Smartstore.Pdf.WkHtml
{
    internal class WkFileInput : IPdfInput
    {
        private string _normalizedUrlOrPath;

        private readonly string _urlOrPath;
        private readonly bool _isLocalUrl;
        private readonly WkHtmlToPdfOptions _options;
        private readonly HttpContext _httpContext;

        public WkFileInput(string urlOrPath, WkHtmlToPdfOptions options, HttpContext httpContext)
        {
            _urlOrPath = urlOrPath;
            _options = options;
            _isLocalUrl = WebHelper.IsLocalUrl(urlOrPath);
            // Can be null
            _httpContext = httpContext;
        }

        public PdfInputKind Kind => PdfInputKind.File;
        public bool IsLocalUrl => _isLocalUrl;

        public string Content
        {
            get
            {
                if (_normalizedUrlOrPath != null)
                {
                    return _normalizedUrlOrPath;
                }

                if (IsPathRooted(_urlOrPath) || _urlOrPath.Contains(Uri.SchemeDelimiter))
                {
                    _normalizedUrlOrPath = _urlOrPath;
                }
                else if (_options.BaseUrl != null)
                {
                    var url = _urlOrPath;
                    if (url.StartsWith('~'))
                    {
                        url = WebHelper.ToAbsolutePath(url);
                    }

                    _normalizedUrlOrPath = _options.BaseUrl.ToString() + PathUtility.NormalizeRelativePath(url);
                }
                else if (_httpContext?.Request != null)
                {
                    _normalizedUrlOrPath = WebHelper.GetAbsoluteUrl(_urlOrPath, _httpContext.Request);
                }
                else
                {
                    _normalizedUrlOrPath = _urlOrPath;
                }

                return _normalizedUrlOrPath;
            }
        }

        public void Teardown()
        {
            // Noop
        }

        internal static bool IsPathRooted(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.IsPathFullyQualified(path);
            }

            return false;
        }
    }
}
