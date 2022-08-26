using System.Drawing;

namespace Smartstore.Imaging.QRCodes
{
    public interface IQRCode
    {
        string GenerateSvg(int border, string foreColor, string backColor);

        IImage GenerateImage(int scale, int border, Color foreColor, Color backColor);
    }
}
