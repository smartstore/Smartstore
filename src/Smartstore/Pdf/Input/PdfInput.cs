using System;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Http;
using Smartstore.Engine;

namespace Smartstore.Pdf
{
    public enum PdfInputKind
    {
        Html,
        Url
    }

    public abstract class PdfInput
    {
        static PdfInput()
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

        protected static Uri EngineBaseUri { get; }

        public abstract PdfInputKind Kind { get; }

        public abstract Task<string> ProcessInputAsync(string flag, HttpContext httpContext);

        public abstract void BuildCommandFragment(string flag, HttpContext httpContext, StringBuilder builder);

        public virtual void Teardown()
        {
            // Noop
        }
    }
}