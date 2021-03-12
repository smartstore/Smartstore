using System.Collections.Generic;
using DinkToPdf;
using Microsoft.AspNetCore.Http;
using Smartstore.Net;

namespace Smartstore.Pdf
{
    public class PdfUrlContent : PdfContent
    {
        private readonly string _url;

        public PdfUrlContent(string url, HttpRequest request)
            : base(request)

        {
            Guard.NotEmpty(url, nameof(url));
            _url = url;
        }

        #region Options

        /// <summary>
        /// Send FormsAuthentication cookie to authorize
        /// </summary>
        public bool SendAuthCookie { get; set; }

        /// <summary>
        /// HTTP Authentication username.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// HTTP Authentication password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Sets cookies.
        /// </summary>
        public IDictionary<string, string> Post { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Sets post values.
        /// </summary>
        public IDictionary<string, string> Cookies { get; set; } = new Dictionary<string, string>();

        #endregion

        public override PdfContentKind Kind => PdfContentKind.Url;

        protected virtual string GetAbsoluteUrl()
        {
            if (EngineBaseUri != null)
            {
                var url = _url;
                // TODO: (core) PdfUrlContent: VirtualPathUtility.ToAbsolute() replacement (??)
                //if (url.StartsWith("~"))
                //{
                //    url = VirtualPathUtility.ToAbsolute(url);
                //}

                return EngineBaseUri.ToString() + url.TrimStart('/');
            }
            else if (Request != null)
            {
                return WebHelper.GetAbsoluteUrl(_url, Request);
            }

            return _url;
        }

        public override string ProcessContent(string flag)
        {
            return GetAbsoluteUrl();
        }

        protected internal override void Apply(string flag, HtmlToPdfDocument document)
        {
            // TODO: (core) Apply PdfUrlContent
        }
    }
}
