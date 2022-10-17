using System.Drawing;

namespace Smartstore.Imaging.Barcodes
{
    public abstract class BarcodeOptions
    {
        /// <summary>
        /// Image margin in pixel. If <c>null</c>, the barcode type decides what margin is best (Default = null).
        /// </summary>
        public int? Margin { get; set; }

        /// <summary>
        /// Whether to draw the EAN text below the barcode (Default = false).
        /// </summary>
        public bool IncludeEanAsText { get; set; }

        /// <summary>
        /// Font to use for EAN text drawing (Default = Arial).
        /// </summary>
        public string EanFontFamily { get; set; } = "Arial";
    }

    /// <summary>
    /// Barcode SVG drawing options.
    /// </summary>
    public class BarcodeSvgOptions : BarcodeOptions
    {
        /// <summary>
        /// The background color of the barcode drawing as a valid web color (Default = #fff).
        /// </summary>
        public string BackColor { get; set; } = "#fff";

        /// <summary>
        /// The foregound color of the barcode drawing as a valid web color (Default = #000).
        /// </summary>
        public string ForeColor { get; set; } = "#000";

        /// <summary>
        /// The (EAN) text color of the barcode drawing as a valid web color (Default = #000).
        /// </summary>
        public string TextColor { get; set; } = "#000";
    }

    /// <summary>
    /// Barcode image drawing options.
    /// </summary>
    public class BarcodeImageOptions : BarcodeOptions
    {
        /// <summary>
        /// The background color of the barcode drawing (Default = <see cref="Color.White"/>).
        /// </summary>
        public Color BackColor { get; set; } = Color.White;

        /// <summary>
        /// The foregound color of the barcode drawing (Default = <see cref="Color.Black"/>).
        /// </summary>
        public Color ForeColor { get; set; } = Color.Black;

        /// <summary>
        /// The (EAN) text color of the barcode drawing (Default = <see cref="Color.Black"/>).
        /// </summary>
        public Color TextColor { get; set; } = Color.Black;

        /// <summary>
        /// Drawing scale (Default = 3).
        /// </summary>
        public int Scale { get; set; } = 3;

        /// <summary>
        /// Height of barcode for 1D drawings in pixel (Default = 40).
        /// </summary>
        public int BarHeightFor1DCode { get; set; } = 40;
    }
}
