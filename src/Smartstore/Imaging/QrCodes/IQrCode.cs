using System.Drawing;

namespace Smartstore.Imaging.QrCodes
{
    /// <summary>
    /// Generates SVG or images from QR codes.
    /// </summary>
    public interface IQrCode
    {
        /// <summary>
        /// The source payload that was used to generate this encoder instance.
        /// </summary>
        QrPayload Payload { get; }

        /// <summary>
        /// Generates SVG for a QR code.
        /// </summary>
        /// <param name="foreColor">The foreground color of the QR code drawing as a valid web color.</param>
        /// <param name="backColor">The background color of the QR code drawing as a valid web color.</param>
        /// <param name="border">The border of the QR code drawing.</param>
        /// <returns>XML of the generated SVG drawing.</returns>
        string GenerateSvg(string foreColor = "#000", string backColor = "#fff", int border = 3);

        /// <summary>
        /// Generates an image for a QR code.
        /// </summary>
        /// <param name="foreColor">The foreground color of the QR code image.</param>
        /// <param name="backColor">The background color of the QR code image.</param>
        /// <param name="scale">Defines the size of the image.</param>
        /// <param name="border">The border of the QR code image.</param>
        /// <returns>The generated image instance.</returns>
        IImage GenerateImage(Color foreColor, Color backColor, int scale = 3, int border = 3);
    }

    public static class IQrCodeExtensions
    {
        /// <summary>
        /// Generates an image for a QR code.
        /// </summary>
        /// <param name="scale">Defines the size of the image.</param>
        /// <param name="border">The border of the QR code image.</param>
        /// <returns>The generated image instance.</returns>
        public static IImage GenerateImage(this IQrCode qrCode, int scale = 3, int border = 3)
        {
            return qrCode.GenerateImage(Color.Black, Color.White, scale, border);
        }
    }
}
