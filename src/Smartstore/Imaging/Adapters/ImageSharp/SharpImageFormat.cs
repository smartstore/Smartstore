using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SharpFormat = SixLabors.ImageSharp.Formats.IImageFormat;
using SharpBmpBitsPerPixel = SixLabors.ImageSharp.Formats.Bmp.BmpBitsPerPixel;
using SharpGifColorTableMode = SixLabors.ImageSharp.Formats.Gif.GifColorTableMode;
using SharpJpgColorType = SixLabors.ImageSharp.Formats.Jpeg.JpegColorType;
using SharpPngBitDepth = SixLabors.ImageSharp.Formats.Png.PngBitDepth;
using SharpPngChunkFilter = SixLabors.ImageSharp.Formats.Png.PngChunkFilter;
using SharpPngColorType = SixLabors.ImageSharp.Formats.Png.PngColorType;
using SharpPngCompressionLevel = SixLabors.ImageSharp.Formats.Png.PngCompressionLevel;
using SharpPngInterlaceMode = SixLabors.ImageSharp.Formats.Png.PngInterlaceMode;
using SharpPngTransparentColorMode = SixLabors.ImageSharp.Formats.Png.PngTransparentColorMode;
using SharpWebpEncodingMethod = SixLabors.ImageSharp.Formats.Webp.WebpEncodingMethod;
using SharpWebpFileFormatType = SixLabors.ImageSharp.Formats.Webp.WebpFileFormatType;

namespace Smartstore.Imaging.Adapters.ImageSharp
{
    internal class SharpImageFormat : IImageFormat
    {
        private readonly SharpFormat _sharpFormat;

        public SharpImageFormat(SharpFormat sharpFormat)
        {
            Guard.NotNull(sharpFormat, nameof(sharpFormat));
            _sharpFormat = sharpFormat;
        }

        public SharpFormat WrappedFormat
            => _sharpFormat;

        public string Name
            => _sharpFormat.Name;

        public string DefaultExtension
            => _sharpFormat.FileExtensions.First();

        public string DefaultMimeType
            => _sharpFormat.DefaultMimeType;

        public IEnumerable<string> FileExtensions
            => _sharpFormat.FileExtensions;

        public IEnumerable<string> MimeTypes
            => _sharpFormat.MimeTypes;

        public virtual IImageEncoder CreateEncoder()
            => SixLabors.ImageSharp.Configuration.Default.ImageFormatsManager.FindEncoder(_sharpFormat);
    }

    internal class BmpFormat : SharpImageFormat, IBmpFormat
    {
        public BmpFormat(SharpFormat sharpFormat)
            : base(sharpFormat)
        {
        }

        /// <inheritdoc/>
        public BmpBitsPerPixel? BitsPerPixel { get; set; }

        /// <inheritdoc/>
        public QuantizationMethod? QuantizationMethod { get; set; }

        /// <inheritdoc/>
        public bool? SupportTransparency { get; set; }

        public override IImageEncoder CreateEncoder()
        {
            if (BitsPerPixel != null
                || (SupportTransparency != null && SupportTransparency == true)
                || (QuantizationMethod != null && QuantizationMethod != Imaging.QuantizationMethod.Octree))
            {
                return new BmpEncoder
                {
                    BitsPerPixel = (SharpBmpBitsPerPixel?)BitsPerPixel,
                    Quantizer = ImageSharpUtility.CreateQuantizer(QuantizationMethod),
                    SupportTransparency = SupportTransparency.GetValueOrDefault()
                };
            }

            return base.CreateEncoder();
        }
    }

    internal class JpegFormat : SharpImageFormat, IJpegFormat
    {
        public JpegFormat(SharpFormat sharpFormat)
            : base(sharpFormat)
        {
        }

        /// <inheritdoc/>
        public int? Quality { get; set; }

        /// <inheritdoc/>
        public JpegColorType? ColorType { get; set; }

        public override IImageEncoder CreateEncoder()
        {
            if (Quality != null || ColorType != null)
            {
                return new JpegEncoder
                {
                    Quality = Quality,
                    ColorType = (SharpJpgColorType?)ColorType
                };
            }

            return base.CreateEncoder();
        }
    }

    internal class PngFormat : SharpImageFormat, IPngFormat
    {
        public PngFormat(SharpFormat sharpFormat)
            : base(sharpFormat)
        {
        }

        /// <inheritdoc/>
        public PngBitDepth? BitDepth { get; set; }

        /// <inheritdoc/>
        public PngColorType? ColorType { get; set; }

        /// <inheritdoc/>
        public PngCompressionLevel? CompressionLevel { get; set; }

        /// <inheritdoc/>
        public float? Gamma { get; set; }

        /// <inheritdoc/>
        public byte? Threshold { get; set; }

        /// <inheritdoc/>
        public QuantizationMethod? QuantizationMethod { get; set; }

        /// <inheritdoc/>
        public PngInterlaceMode? InterlaceMode { get; set; }

        /// <inheritdoc/>
        public PngChunkFilter? ChunkFilter { get; set; }

        /// <inheritdoc/>
        public PngTransparentColorMode? TransparentColorMode { get; set; }

        /// <inheritdoc/>
        public bool IgnoreMetadata { get; set; }

        public override IImageEncoder CreateEncoder()
        {
            if (BitDepth != null
                || ColorType != null
                || CompressionLevel != null
                || Gamma != null
                || Threshold != null
                || QuantizationMethod != null
                || InterlaceMode != null
                || ChunkFilter != null
                || TransparentColorMode != null
                || IgnoreMetadata)
            {
                var encoder = new PngEncoder
                {
                    BitDepth = (SharpPngBitDepth?)BitDepth,
                    ColorType = (SharpPngColorType?)ColorType,
                    Gamma = Gamma,
                    InterlaceMethod = (SharpPngInterlaceMode?)InterlaceMode,
                    ChunkFilter = (SharpPngChunkFilter?)ChunkFilter,
                    Quantizer = ImageSharpUtility.CreateQuantizer(QuantizationMethod),
                    IgnoreMetadata = IgnoreMetadata
                };

                if (TransparentColorMode != null)
                    encoder.TransparentColorMode = (SharpPngTransparentColorMode)TransparentColorMode.Value;
                if (CompressionLevel != null)
                    encoder.CompressionLevel = (SharpPngCompressionLevel)CompressionLevel;
                if (Threshold != null)
                    encoder.Threshold = Threshold.Value;

                return encoder;
            }

            return base.CreateEncoder();
        }
    }

    internal class WebpFormat : SharpImageFormat, IWebpFormat
    {
        public WebpFormat(SharpFormat sharpFormat)
            : base(sharpFormat)
        {
        }

        /// <inheritdoc/>
        public WebpFileFormatType? FileFormat { get; set; }

        /// <inheritdoc/>
        public int? Quality { get; set; }

        /// <inheritdoc/>
        public WebpEncodingMethod? Method { get; set; }

        /// <inheritdoc/>
        public bool? UseAlphaCompression { get; set; }

        /// <inheritdoc/>
        public int? EntropyPasses { get; set; }

        /// <inheritdoc/>
        public int? SpatialNoiseShaping { get; set; }

        /// <inheritdoc/>
        public int? FilterStrength { get; set; }

        /// <inheritdoc/>
        public bool? NearLossless { get; set; }

        /// <inheritdoc/>
        public int? NearLosslessQuality { get; set; }

        public override IImageEncoder CreateEncoder()
        {
            if (FileFormat != null
                || Quality != null
                || Method != null
                || UseAlphaCompression != null
                || EntropyPasses != null
                || SpatialNoiseShaping != null
                || FilterStrength != null
                || NearLossless != null
                || NearLosslessQuality != null)
            {
                var encoder = new WebpEncoder
                {
                    FileFormat = (SharpWebpFileFormatType?)FileFormat
                };

                if (Quality != null)
                    encoder.Quality = Quality.Value;
                if (Method != null)
                    encoder.Method = (SharpWebpEncodingMethod)Method.Value;
                if (UseAlphaCompression != null)
                    encoder.UseAlphaCompression = UseAlphaCompression.Value;
                if (EntropyPasses != null)
                    encoder.EntropyPasses = EntropyPasses.Value;
                if (SpatialNoiseShaping != null)
                    encoder.SpatialNoiseShaping = SpatialNoiseShaping.Value;
                if (FilterStrength != null)
                    encoder.FilterStrength = FilterStrength.Value;
                if (NearLossless != null)
                    encoder.NearLossless = NearLossless.Value;
                if (NearLosslessQuality != null)
                    encoder.NearLosslessQuality = NearLosslessQuality.Value;

                return encoder;
            }

            return base.CreateEncoder();
        }
    }

    internal class GifFormat : SharpImageFormat, IGifFormat
    {
        public GifFormat(SharpFormat sharpFormat)
            : base(sharpFormat)
        {
        }

        /// <inheritdoc/>
        public QuantizationMethod? QuantizationMethod { get; set; }

        /// <inheritdoc/>
        public GifColorTableMode? ColorTableMode { get; set; }

        public override IImageEncoder CreateEncoder()
        {
            if (QuantizationMethod != null || ColorTableMode != null)
            {
                return new GifEncoder
                {
                    ColorTableMode = (SharpGifColorTableMode?)ColorTableMode,
                    Quantizer = ImageSharpUtility.CreateQuantizer(QuantizationMethod)
                };
            }

            return base.CreateEncoder();
        }
    }
}