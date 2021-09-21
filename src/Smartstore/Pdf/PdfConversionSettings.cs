namespace Smartstore.Pdf
{
    public class PdfConversionSettings
    {
        /// <summary>
        /// The title of the generated pdf file (The title of the first document is used if not specified)
        /// </summary>
        [PdfOption("--title")]
        public string Title { get; set; }

        /// <summary>
        /// Get or set option to generate grayscale PDF 
        /// </summary>
        [PdfOption("-g")]
        public bool Grayscale { get; set; }

        /// <summary>
        /// Get or set option to generate low quality PDF (shrink the result document space) 
        /// </summary>
        [PdfOption("-l")]
        public bool LowQuality { get; set; }

        /// <summary>
        /// Get or set PDF page margins (in mm) 
        /// </summary>
        public PdfPageMargins Margins { get; set; } = new PdfPageMargins();

        /// <summary>
        /// Get or set PDF page orientation
        /// </summary>
        [PdfOption("-O")]
        public PdfPageOrientation? Orientation { get; set; }

        /// <summary>
        /// Get or set PDF page size 
        /// </summary>
        [PdfOption("--page-size")]
        public PdfPageSize? Size { get; set; }

        /// <summary>
        /// Get or set PDF page width (in mm)
        /// </summary>
        [PdfOption("--page-width")]
        public float? PageWidth { get; set; }

        /// <summary>
        /// Get or set PDF page height (in mm) 
        /// </summary>
        [PdfOption("--page-height")]
        public float? PageHeight { get; set; }

        /// <summary>
        /// Custom global pdf tool options
        /// </summary>
        public string CustomFlags { get; set; }



        /// <summary>
        /// Cover content
        /// </summary>
        public PdfContent Cover { get; set; }

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
        public PdfContent Page { get; set; }

        /// <summary>
        /// Page content options
        /// </summary>
        public PdfPageOptions PageOptions { get; set; } = new PdfPageOptions();

        /// <summary>
        /// Footer content
        /// </summary>
        public PdfContent Footer { get; set; }

        /// <summary>
        /// Footer content options
        /// </summary>
        public PdfHeaderFooterOptions FooterOptions { get; set; } = new PdfHeaderFooterOptions();

        /// <summary>
        /// Header content
        /// </summary>
        public PdfContent Header { get; set; }

        /// <summary>
        /// Header content options
        /// </summary>
        public PdfHeaderFooterOptions HeaderOptions { get; set; } = new PdfHeaderFooterOptions();
    }
}