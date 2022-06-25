namespace Smartstore.Pdf
{
    public partial class PdfTocOptions : PdfPageOptions
    {
        /// <summary>
        /// TOC creation enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// The header text of the toc (default Table of Contents)
        /// </summary>
        public string TocHeaderText { get; set; }

        /// <summary>
        /// Do not use dotted lines in the TOC. Default: true.
        /// </summary>
        public bool DisableDottedLines { get; set; } = true;

        /// <summary>
        /// Do not link from toc to sections
        /// </summary>
        public bool DisableTocLinks { get; set; }

        /// <summary>
        /// For each level of headings in the toc indent by this length (default 1em)
        /// </summary>
        public string TocLevelIndendation { get; set; }

        /// <summary>
        /// For each level of headings in the toc the font is scaled by this factor (default 0.8)
        /// </summary>
        public float? TocTextSizeShrink { get; set; }
    }
}