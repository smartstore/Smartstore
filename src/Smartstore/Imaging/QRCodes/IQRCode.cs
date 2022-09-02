using System.Drawing;

namespace Smartstore.Imaging.QRCodes
{
    /// <summary>
    /// Interface for QR code implementations.
    /// </summary>
    public interface IQRCode
    {
        /// <summary>
        /// Generates a SVG graphic for a QR code.
        /// </summary>
        /// <param name="border">The border of the QR code graphic.</param>
        /// <param name="foreColor">The foreground color of the QR code graphic.</param>
        /// <param name="backColor">The background color of the QR code graphic.</param>
        /// <returns>XML of the generated SVG graphic.</returns>
        string GenerateSvg(int border = 3, string foreColor = "#000", string backColor = "#fff");

        /// <summary>
        /// Generates an image for a QR code.
        /// </summary>
        /// <param name="scale">Defines the size of the graphic.</param>
        /// <param name="border">The border of the QR code graphic.</param>
        /// <param name="foreColor">The foreground color of the QR code graphic.</param>
        /// <param name="backColor">The background color of the QR code graphic.</param>
        /// <returns>The image </returns>
        IImage GenerateImage(Color foreColor, Color backColor, int scale = 3, int border = 3);

        /// <summary>
        /// The serialized payload.
        /// </summary>
        string Payload { get; set; }
    }
}
