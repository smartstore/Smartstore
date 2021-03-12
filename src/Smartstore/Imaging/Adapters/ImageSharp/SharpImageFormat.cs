using System.Collections.Generic;
using System.Linq;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SharpFormat = SixLabors.ImageSharp.Formats.IImageFormat;
using SharpGifColorTableMode = SixLabors.ImageSharp.Formats.Gif.GifColorTableMode;
using SharpJpegSubsample = SixLabors.ImageSharp.Formats.Jpeg.JpegSubsample;
using SharpPngBitDepth = SixLabors.ImageSharp.Formats.Png.PngBitDepth;
using SharpPngChunkFilter = SixLabors.ImageSharp.Formats.Png.PngChunkFilter;
using SharpPngColorType = SixLabors.ImageSharp.Formats.Png.PngColorType;
using SharpPngCompressionLevel = SixLabors.ImageSharp.Formats.Png.PngCompressionLevel;
using SharpPngInterlaceMode = SixLabors.ImageSharp.Formats.Png.PngInterlaceMode;
using SharpPngTransparentColorMode = SixLabors.ImageSharp.Formats.Png.PngTransparentColorMode;

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

    internal class JpegFormat : SharpImageFormat, IJpegFormat
    {
        public JpegFormat(SharpFormat sharpFormat)
            : base(sharpFormat)
        {
        }

        public int? Quality { get; set; }
        public JpegSubsample? Subsample { get; set; }

        public override IImageEncoder CreateEncoder()
        {
            if (Quality != null || Subsample != null)
            {
                return new JpegEncoder { Quality = Quality, Subsample = (SharpJpegSubsample?)Subsample };
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

        public QuantizationMethod? QuantizationMethod { get; set; }
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

    internal class PngFormat : SharpImageFormat, IPngFormat
    {
        public PngFormat(SharpFormat sharpFormat)
            : base(sharpFormat)
        {
        }

        public PngBitDepth? BitDepth { get; set; }
        public PngColorType? ColorType { get; set; }
        public PngCompressionLevel? CompressionLevel { get; set; }
        public float? Gamma { get; set; }
        public byte? Threshold { get; set; }
        public QuantizationMethod? QuantizationMethod { get; set; }
        public PngInterlaceMode? InterlaceMode { get; set; }
        public PngChunkFilter? ChunkFilter { get; set; }
        public PngTransparentColorMode? TransparentColorMode { get; set; }
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
                {
                    encoder.TransparentColorMode = (SharpPngTransparentColorMode)TransparentColorMode.Value;
                }

                if (CompressionLevel != null)
                {
                    encoder.CompressionLevel = (SharpPngCompressionLevel)CompressionLevel;
                }

                if (Threshold != null)
                {
                    encoder.Threshold = Threshold.Value;
                }

                return encoder;
            }

            return base.CreateEncoder();
        }
    }
}