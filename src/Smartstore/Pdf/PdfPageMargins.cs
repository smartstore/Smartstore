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
        public float? Bottom { get; set; }

        /// <summary>
        ///  Get or set left margin (in mm)
        /// </summary>
        public float? Left { get; set; } = 20;

        /// <summary>
        /// Get or set right margin (in mm)
        /// </summary>
        public float? Right { get; set; }

        /// <summary>
        /// Get or set top margin (in mm)
        /// </summary>
        public float? Top { get; set; }
    }
}