using System.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using SharpColor = SixLabors.ImageSharp.Color;

namespace Smartstore.Imaging
{
    public static class ImagingHelper
    {
        /// <summary>
        /// Converts a <see cref="SixLabors.ImageSharp.Color"/> to <see cref="System.Drawing.Color"/>.
        /// </summary>
        internal static Color ConvertColor(SharpColor input)
        {
            var p = input.ToPixel<Rgba32>();
            return Color.FromArgb(p.A, p.R, p.G, p.B);
        }

        /// <summary>
        /// Converts a <see cref="System.Drawing.Color"/> to <see cref="SharpColor"/>.
        /// </summary>
        internal static SharpColor ConvertColor(Color input)
        {
            return SharpColor.FromRgba(input.R, input.G, input.B, input.A);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Color"/> struct from the given input string.
        /// </summary>
        /// <param name="htmlColor">
        /// The name of the color or the hexadecimal representation of the combined color components arranged
        /// in rgb, rgba, rrggbb, or rrggbbaa format to match web syntax.
        /// </param>
        /// <returns>The <see cref="Color"/>.</returns>
        public static Color TranslateColor(string htmlColor)
        {
            Guard.NotEmpty(htmlColor, nameof(htmlColor));
            
            if (!SharpColor.TryParse(htmlColor, out var sharpColor))
            {
                throw new ArgumentException("Input string color is not in the correct format.", nameof(htmlColor));
            }

            return ConvertColor(sharpColor);
        }

        /// <summary>
        /// Attempts to create a new instance of the <see cref="Color"/> struct from the given input string.
        /// </summary>
        /// <param name="htmlColor">
        /// The name of the color or the hexadecimal representation of the combined color components arranged
        /// in rgb, rgba, rrggbb, or rrggbbaa format to match web syntax.
        /// </param>
        /// <param name="result">When this method returns, contains the <see cref="Color"/> equivalent of the html input.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        public static bool TryTranslateColor(string htmlColor, out Color result)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(htmlColor))
            {
                return false;
            }

            if (SharpColor.TryParse(htmlColor, out var sharpColor))
            {
                result = ConvertColor(sharpColor);
                return true;
            }

            return false;
        }

        public static int GetPerceivedBrightness(string htmlColor)
        {
            if (string.IsNullOrEmpty(htmlColor))
            {
                htmlColor = "#ffffff";
            }

            return GetPerceivedBrightness(ColorTranslator.FromHtml(htmlColor));
        }

        /// <summary>
        /// Calculates the perceived brightness of a color.
        /// </summary>
        /// <param name="color">The color</param>
        /// <returns>
        /// A number in the range of 0 (black) to 255 (White). 
        /// For text contrast colors, an optimal cutoff value is 130.
        /// </returns>
        public static int GetPerceivedBrightness(Color color)
        {
            return (int)Math.Sqrt(
               color.R * color.R * .241 +
               color.G * color.G * .691 +
               color.B * color.B * .068);
        }

        /// <summary>
        /// Recalculates an image size while keeping aspect ratio but does not upscale.
        /// </summary>
        /// <param name="original">Original size</param>
        /// <param name="maxSize">New max size</param>
        /// <returns>The rescaled size</returns>
        public static Size Rescale(Size original, int maxSize)
        {
            Guard.IsPositive(maxSize, nameof(maxSize));

            return Rescale(original, new Size(maxSize, maxSize));
        }

        /// <summary>
        /// Recalculates an image size while keeping aspect ratio but does not upscale.
        /// </summary>
        /// <param name="original">Original size</param>
        /// <param name="maxSize">New max size</param>
        /// <returns>The rescaled size</returns>
        public static Size Rescale(Size original, Size maxSize)
        {
            if (original.IsEmpty || maxSize.IsEmpty || (original.Width <= maxSize.Width && original.Height <= maxSize.Height))
            {
                return original;
            }

            // Figure out the ratio
            double ratioX = (double)maxSize.Width / (double)original.Width;
            double ratioY = (double)maxSize.Height / (double)original.Height;
            // use whichever multiplier is smaller
            double ratio = ratioX < ratioY ? ratioX : ratioY;

            return new Size(Convert.ToInt32(original.Width * ratio), Convert.ToInt32(original.Height * ratio));
        }
    }
}