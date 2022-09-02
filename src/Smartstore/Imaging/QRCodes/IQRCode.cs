using System.Drawing;

namespace Smartstore.Imaging.QRCodes
{
    // TODO: (mh) (core) Very unprecise dev docs: e.g. how do scale and border behave? What effect do they have on the resulting drawing?

    /// <summary>
    /// Generates svg or images from QR codes.
    /// </summary>
    public interface IQRCode
    {
        /// <summary>
        /// The source payload that was used to generate this encoder instance.
        /// </summary>
        QRPayload Payload { get; }

        /// <summary>
        /// Generates SVG for a QR code.
        /// </summary>
        /// <param name="foreColor">The foreground color of the QR code drawing as a valid web color.</param>
        /// <param name="backColor">The background color of the QR code drawing as a valid web color.</param>
        /// <param name="border">The border of the QR code drawing.</param>
        /// <returns>XML of the generated SVG drawing.</returns>
        string GenerateSvg(string foreColor = "#000", string backColor = "#fff", int border = 3);

        // TODO: (mh) (core) Make overloads as extension methods with varying parameters (e.g., without color params)
        // TODO: (mh) (core) Are you sure that default value 3 makes sense for scale and border?
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
}
