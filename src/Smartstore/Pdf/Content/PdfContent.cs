using System;
using Microsoft.AspNetCore.Http;
using Autofac;
using Smartstore.Engine;
using DinkToPdf;

namespace Smartstore.Pdf
{
    public enum PdfContentKind
    {
        Html,
        Url
    }

    public abstract class PdfContent
    {
        static PdfContent()
        {
            var baseUrl = EngineContext.Current?.Application?.AppConfiguration?.PdfEngineBaseUrl.TrimSafe().NullEmpty();
            if (baseUrl != null)
            {
                if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
                {
                    EngineBaseUri = uri;
                }
            }
        }

        public PdfContent(HttpRequest request)
        {
            Request = request ?? EngineContext.Current?.Application?.Services?.ResolveOptional<IHttpContextAccessor>()?.HttpContext?.Request;
        }

        protected static Uri EngineBaseUri { get; private set; }

        protected HttpRequest Request { get; private set; }

        public abstract PdfContentKind Kind { get; }

        public abstract string ProcessContent(string flag);

        protected internal abstract void Apply(string flag, HtmlToPdfDocument document);

        public virtual void Teardown()
        {
            // Noop
        }
    }
}