using System.Text;
using Microsoft.AspNetCore.Http;
using Smartstore.Engine;

namespace Smartstore.Pdf
{
    public enum PdfInputKind
    {
        /// <summary>
        /// Input is HTML content
        /// </summary>
        Html,

        /// <summary>
        /// Input is an absolute or relative URL, or a physical file path.
        /// </summary>
        File
    }

    /// <summary>
    /// Represents PDF input for page, cover or header/footer section.
    /// </summary>
    public interface IPdfInput
    {
        /// <summary>
        /// The input type.
        /// </summary>
        PdfInputKind Kind { get; }

        /// <summary>
        /// The content. Either HTML or path according to <see cref="Kind"/>.
        /// </summary>
        string Content { get; }

        /// <summary>
        /// Cleans up resources and resets state.
        /// </summary>
        void Teardown();
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