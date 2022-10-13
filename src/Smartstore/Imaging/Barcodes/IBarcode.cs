using System.Drawing;

namespace Smartstore.Imaging.Barcodes
{
    /// <summary>
    /// Generates SVG or images from barcodes.
    /// </summary>
    public interface IBarcode
    {
        /// <summary>
        /// The source payload that was used to generate this encoder instance.
        /// </summary>
        BarcodePayload Payload { get; }

        /// <summary>
        /// Generates SVG for a barcode.
        /// </summary>
        /// <param name="foreColor">The foreground color of the barcode drawing as a valid web color.</param>
        /// <param name="backColor">The background color of the barcode drawing as a valid web color.</param>
        /// <param name="border">The border of the barcode drawing.</param>
        /// <returns>XML of the generated SVG drawing.</returns>
        string GenerateSvg(string foreColor = "#000", string backColor = "#fff", int border = 3);

        /// <summary>
        /// Generates an image for a barcode.
        /// </summary>
        /// <param name="foreColor">The foreground color of the barcode image.</param>
        /// <param name="backColor">The background color of the barcode image.</param>
        /// <param name="scale">Defines the size of the barcode image.</param>
        /// <param name="border">The border of the barcode image.</param>
        /// <returns>The generated image instance.</returns>
        IImage GenerateImage(Color foreColor, Color backColor, int scale = 3, int border = 3);
    }
}
