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
        /// <param name="options">SVG drawing options.</param>
        /// <returns>XML of the generated SVG drawing.</returns>
        string GenerateSvg(BarcodeSvgOptions options = null);

        /// <summary>
        /// Generates an image for a barcode.
        /// </summary>
        /// <param name="options">Image drawing options.</param>
        /// <returns>The generated image instance.</returns>
        IImage GenerateImage(BarcodeImageOptions options = null);
    }
}
