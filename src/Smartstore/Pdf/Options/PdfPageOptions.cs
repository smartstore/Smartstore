namespace Smartstore.Pdf
{
    public partial class PdfPageOptions : IPdfOptions
    {
        /// <summary>
        /// Get or set zoom factor. Default = 1.
        /// </summary>
        public float Zoom { get; set; } = 1f;

        /// <summary>
        /// Indicates whether the page background should be disabled.
        /// </summary>
        public bool DisableBackground { get; set; }

        /// <summary>
        /// Use print media-type instead of screen. Default = true.
        /// </summary>
        public bool UsePrintMediaType { get; set; } = true;

        /// <summary>
        /// Specifies a user style sheet to load with every page
        /// </summary>
        public string UserStylesheetUrl { get; set; }

        /// <summary>
        /// Do not load or print images.
        /// </summary>
        public bool DisableImages { get; set; }

        /// <summary>
        /// Enable installed plugins (plugins will likely not work)
        /// </summary>
        public bool EnablePlugins { get; set; }

        /// <summary>
        /// Do not allow web pages to run javascript.
        /// </summary>
        public bool DisableJavascript { get; set; }

        /// <summary>
        /// Disable the intelligent shrinking strategy used by WebKit that makes the pixel/dpi ratio non-constant
        /// </summary>
        public bool DisableSmartShrinking { get; set; }

        /// <summary>
        /// Minimum font size
        /// </summary>
        public int? MinimumFontSize { get; set; }

        /// <summary>
        /// Custom page PDF tool arguments/options
        /// </summary>
        public string CustomArguments { get; set; }
    }
}