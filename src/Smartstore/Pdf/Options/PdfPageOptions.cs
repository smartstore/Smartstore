using DinkToPdf;

namespace Smartstore.Pdf
{
    public class PdfPageOptions : PdfOptions
    {
        /// <summary>
        /// Get or set zoom factor. Default = 1.
        /// </summary>
        public float Zoom { get; set; } = 1f;

        /// <summary>
        /// Indicates whether the page background should be disabled.
        /// </summary>
        public bool BackgroundDisabled { get; set; }

        /// <summary>
        /// Use print media-type instead of screen. Default = true.
        /// </summary>
        public bool UsePrintMediaType { get; set; } = true;

        /// <summary>
        /// Specifies a user style sheet to load with every page
        /// </summary>
        public string UserStylesheetUrl { get; set; }

        /// <summary>
        /// Custom page pdf tool options
        /// </summary>
        public string CustomFlags { get; set; }

        /// <inheritdoc/>
        protected internal override void Apply(string flag, HtmlToPdfDocument document)
        {
            // TODO: (core) Apply PdfPageOptions
        }
    }
}