namespace Smartstore.Pdf
{
    public class PdfConversionSettings
    {
        /// <summary>
        /// The title of the generated pdf file (The title of the first document is used if not specified)
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Be less verbose. Default: true.
        /// </summary>
        public bool Quiet { get; set; } = true;

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
        /// Custom global pdf tool options
        /// </summary>
        public string CustomFlags { get; set; }



        /// <summary>
        /// Cover content
        /// </summary>
        public PdfInput Cover { get; set; }

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
        public PdfInput Page { get; set; }

        /// <summary>
        /// Page content options
        /// </summary>
        public PdfPageOptions PageOptions { get; set; } = new PdfPageOptions();

        /// <summary>
        /// Footer content
        /// </summary>
        public PdfInput Footer { get; set; }

        /// <summary>
        /// Footer content options
        /// </summary>
        public PdfSectionOptions FooterOptions { get; set; } = new PdfSectionOptions();

        /// <summary>
        /// Header content
        /// </summary>
        public PdfInput Header { get; set; }

        /// <summary>
        /// Header content options
        /// </summary>
        public PdfSectionOptions HeaderOptions { get; set; } = new PdfSectionOptions();
    }
}