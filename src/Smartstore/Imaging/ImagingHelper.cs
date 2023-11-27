using System.Drawing;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp.PixelFormats;
using SharpColor = SixLabors.ImageSharp.Color;

namespace Smartstore.Imaging
{
    public static class ImagingHelper
    {
        public static ILogger Logger { get; set; } = NullLogger.Instance;

        private static readonly string CssColorComponentDelimiterRegex = @"(\s*/\s*)|(\s*,\s*)|(\s+)";
        private static readonly Color ColorTranslationFallback = Color.White;

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
            Color colorFromHtml;

            if (string.IsNullOrEmpty(htmlColor))
            {
                colorFromHtml = ColorTranslationFallback;
            }
            else
            {
                try
                {
                    colorFromHtml = ColorTranslator.FromHtml(htmlColor);
                }
                catch
                {
                    // Either an unknown color name, a CSS function, or an invalid hex string.
                    colorFromHtml = Color.Empty;
                }

                if (colorFromHtml.IsEmpty)
                {
                    try
                    {
                        if ((htmlColor.StartsWithNoCase("rgb(") || htmlColor.StartsWithNoCase("rgba(")) && htmlColor.EndsWith(')'))
                        {
                            colorFromHtml = ConvertRgbaCssColor(htmlColor);
                        }
                        else
                        {
                            // Invalid or unknown color name / function. Use fallback color.
                            colorFromHtml = ColorTranslationFallback;
                        }
                    }
                    catch
                    {
                        // Invalid or too complex color code. Use fallback color.
                        colorFromHtml = ColorTranslationFallback;
                    }
                }
            }

            return GetPerceivedBrightness(colorFromHtml);
        }

        /// <summary>
        /// Converts a CSS rgba() color string into a <see cref="Color"/> instance.
        /// </summary>
        /// <param name="htmlColor">The CSS color string must be in rgb(r g b) or rgba(r g b a) format. Valid delimiters are space, comma, and slash.</param>
        /// <returns>The <see cref="Color"/>.</returns>
        private static Color ConvertRgbaCssColor(string htmlColor)
        {
            var rgba = htmlColor.Substring(htmlColor.IndexOf('(') + 1, htmlColor.IndexOf(')') - htmlColor.IndexOf('(') - 1);

            // Separate the values by spaces and /or commas.
            rgba = rgba.RegexReplace(CssColorComponentDelimiterRegex, " ");
            var rgbParts = rgba.Split(' ');

            // Convert the values to integers.
            var r = ConvertCssColorComponent(rgbParts[0]);
            var g = ConvertCssColorComponent(rgbParts[1]);
            var b = ConvertCssColorComponent(rgbParts[2]);
            var a = rgbParts.Length == 3 ? 255 : ConvertCssColorComponent(rgbParts[3]);

            // On error, use fallback color.
            if (r == null || g == null || b == null || a == null)
            {
                return ColorTranslationFallback;
            }
            else
            {
                return Color.FromArgb(a.Value, r.Value, g.Value, b.Value);
            }
        }

        /// <summary>
        /// Converts a CSS color component into an integer between 0 and 255.
        /// </summary>
        /// <param name="colorComponent">The CSS color component can be an integer, a double, or a percentage.</param>
        /// <returns>An <see cref="int"/> between 0 and 255. Invalid values default to 0.</returns>
        private static int? ConvertCssColorComponent(string colorComponent)
        {
            // Check for percentage values.
            if (colorComponent.EndsWith('%'))
            {
                if (!double.TryParse(colorComponent.Substring(0, colorComponent.Length - 1), NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleVal))
                {
                    return null;
                }
                else
                {
                    return (int)((doubleVal / 100) * 255);
                }
            }
            // Check for double values.
            else if (colorComponent.Contains('.'))
            {
                if (!double.TryParse(colorComponent, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleVal))
                {
                    return null;
                }
                else
                {
                    return (int)(doubleVal * 255);
                }
            }
            else
            {
                if (!int.TryParse(colorComponent, out int integerValue))
                {
                    return null;
                }
                else
                {
                    return integerValue;
                }
            }
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