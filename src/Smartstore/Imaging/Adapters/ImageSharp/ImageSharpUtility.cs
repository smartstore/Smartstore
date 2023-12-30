using System.Collections.Frozen;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using SharpFormat = SixLabors.ImageSharp.Formats.IImageFormat;

namespace Smartstore.Imaging.Adapters.ImageSharp
{
    internal static class ImageSharpUtility
    {
        private readonly static FrozenDictionary<ResamplingMode, Func<IResampler>> _samplerMap = new Dictionary<ResamplingMode, Func<IResampler>>()
        {
            [ResamplingMode.Bicubic] = () => KnownResamplers.Bicubic,
            [ResamplingMode.Box] = () => KnownResamplers.Box,
            [ResamplingMode.CatmullRom] = () => KnownResamplers.CatmullRom,
            [ResamplingMode.Hermite] = () => KnownResamplers.Hermite,
            [ResamplingMode.Lanczos2] = () => KnownResamplers.Lanczos2,
            [ResamplingMode.Lanczos3] = () => KnownResamplers.Lanczos3,
            [ResamplingMode.Lanczos5] = () => KnownResamplers.Lanczos5,
            [ResamplingMode.Lanczos8] = () => KnownResamplers.Lanczos8,
            [ResamplingMode.MitchellNetravali] = () => KnownResamplers.MitchellNetravali,
            [ResamplingMode.NearestNeighbor] = () => KnownResamplers.NearestNeighbor,
            [ResamplingMode.Robidoux] = () => KnownResamplers.Robidoux,
            [ResamplingMode.RobidouxSharp] = () => KnownResamplers.RobidouxSharp,
            [ResamplingMode.Spline] = () => KnownResamplers.Spline,
            [ResamplingMode.Triangle] = () => KnownResamplers.Triangle,
            [ResamplingMode.Welch] = () => KnownResamplers.Welch
        }.ToFrozenDictionary();

        public static SharpImageFormat CreateFormat(SharpFormat sharpFormat)
        {
            return sharpFormat.Name switch
            {
                "PNG" => new PngFormat(sharpFormat),
                "JPEG" => new JpegFormat(sharpFormat),
                "WEBP" => new WebpFormat(sharpFormat),
                "GIF" => new GifFormat(sharpFormat),
                "BMP" => new BmpFormat(sharpFormat),
                _ => new SharpImageFormat(sharpFormat),
            };
        }

        public static IQuantizer CreateQuantizer(QuantizationMethod? method)
        {
            if (method == null)
            {
                return null;
            }

            return method.Value switch
            {
                QuantizationMethod.Octree => new OctreeQuantizer(),
                QuantizationMethod.WebSafePalette => new WebSafePaletteQuantizer(),
                QuantizationMethod.WernerPalette => new WernerPaletteQuantizer(),
                _ => new WuQuantizer(),
            };
        }

        public static IResampler GetResampler(ResamplingMode mode)
        {
            if (_samplerMap.TryGetValue(mode, out var sampler))
            {
                return sampler();
            }

            return null;
        }
    }
}
