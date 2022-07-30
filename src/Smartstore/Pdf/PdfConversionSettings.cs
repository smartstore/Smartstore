namespace Smartstore.Pdf
{
    /// <summary>
    /// Marker interface for PDF options.
    /// </summary>
    public interface IPdfOptions
    {
    }

    public class PdfConversionSettings
    {
        /// <summary>
        /// The title of the generated pdf file (The title of the first document is used if not specified)
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Sets log verbosity. <c>false</c> corresponds to log-level <c>error</c>, 
        /// <c>true</c> corresponds to log-level <c>none</c>. Default: false.
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// Get or set option to generate grayscale PDF 
        /// </summary>
        public bool Grayscale { get; set; }

        /// <summary>
        /// Get or set option to generate low quality PDF (shrink the result document space) 
        /// </summary>
        public bool LowQuality { get; set; }

        /// <summary>
        /// Get or set PDF page margins (in mm) 
        /// </summary>
        public PdfPageMargins Margins { get; set; } = new PdfPageMargins();

        /// <summary>
        /// Get or set PDF page orientation
        /// </summary>
        public PdfPageOrientation? Orientation { get; set; }

        /// <summary>
        /// Get or set PDF page size 
        /// </summary>
        public PdfPageSize? Size { get; set; }

        /// <summary>
        /// Get or set PDF page width (in mm)
        /// </summary>
        public float? PageWidth { get; set; }

        /// <summary>
        /// Get or set PDF page height (in mm) 
        /// </summary>
        public float? PageHeight { get; set; }

        /// <summary>
        /// The depth of the document outline. Default: 1.
        /// </summary>
        public byte OutlineDepth { get; set; } = 1;

        /// <summary>
        /// Custom global pdf tool arguments/options
        /// </summary>
        public string CustomArguments { get; set; }



        /// <summary>
        /// Cover content
        /// </summary>
        public IPdfInput Cover { get; set; }

        /// <summary>
        /// Cover content options
        /// </summary>
        public PdfPageOptions CoverOptions { get; set; } = new PdfPageOptions();

        /// <summary>
        /// Toc (table of contents) options
        /// </summary>
        public PdfTocOptions TocOptions { get; set; } = new PdfTocOptions();

        /// <summary>
        /// Page content (required)
        /// </summary>
        public IPdfInput Page { get; set; }

        /// <summary>
        /// Page content options
        /// </summary>
        public PdfPageOptions PageOptions { get; set; } = new PdfPageOptions();

        /// <summary>
        /// Footer content
        /// </summary>
        public IPdfInput Footer { get; set; }

        /// <summary>
        /// Footer content options
        /// </summary>
        public PdfSectionOptions FooterOptions { get; set; } = new PdfSectionOptions();

        /// <summary>
        /// Header content
        /// </summary>
        public IPdfInput Header { get; set; }

        /// <summary>
        /// Header content options
        /// </summary>
        public PdfSectionOptions HeaderOptions { get; set; } = new PdfSectionOptions();
    }
}