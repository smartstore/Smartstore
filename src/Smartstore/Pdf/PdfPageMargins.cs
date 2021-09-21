namespace Smartstore.Pdf
{
    /// <summary>
    /// Represents PDF page margins (unit size is mm)
    /// </summary>
    public class PdfPageMargins
    {
        /// <summary>
        /// Get or set bottom margin (in mm)
        /// </summary>
        [PdfOption("-B")]
        public float? Bottom { get; set; }

        /// <summary>
        ///  Get or set left margin (in mm)
        /// </summary>
        [PdfOption("-L")]
        public float? Left { get; set; } = 20;

        /// <summary>
        /// Get or set right margin (in mm)
        /// </summary>
        [PdfOption("-R")]
        public float? Right { get; set; }

        /// <summary>
        /// Get or set top margin (in mm)
        /// </summary>
        [PdfOption("-T")]
        public float? Top { get; set; }
    }
}