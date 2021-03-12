namespace Smartstore.Pdf
{
    public class PdfConvertSettings
    {
        /// <summary>
        /// The title of the generated pdf file (The title of the first document is used if not specified)
        /// </summary>
        public string Title { get; set; }

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
        public PdfPagePrientation Orientation { get; set; } = PdfPagePrientation.Default;

        /// <summary>
        /// Get or set PDF page width (in mm)
        /// </summary>
        public float? PageWidth { get; set; }

        /// <summary>
        /// Get or set PDF page height (in mm) 
        /// </summary>
        public float? PageHeight { get; set; }

        /// <summary>
        /// Get or set PDF page orientation 
        /// </summary>
        public PdfPageSize Size { get; set; } = PdfPageSize.Default;

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