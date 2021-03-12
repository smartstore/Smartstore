using System.Collections.Generic;

namespace Smartstore.Imaging
{
    /// <summary>
    /// Defines the contract for an image format.
    /// </summary>
    public interface IImageFormat // TODO: >> IEquatable<IImageFormat> & Equals & GetHashCode()
    {
        /// <summary>
        /// Gets the name that describes this image format.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the default file extension.
        /// </summary>
        string DefaultExtension { get; }

        /// <summary>
        /// Gets the default mimetype that the image format uses
        /// </summary>
        string DefaultMimeType { get; }

        /// <summary>
        /// Gets the file extensions this image format commonly uses.
        /// </summary>
        IEnumerable<string> FileExtensions { get; }

        /// <summary>
        /// Gets all the mimetypes that have been used by this image format.
        /// </summary>
        IEnumerable<string> MimeTypes { get; }
    }

    public interface IJpegFormat : IImageFormat
    {
        /// <summary>
        /// Gets or sets the quality that will be used to encode the image. Quality
        /// index must be between 0 and 100 (compression from max to min).
        /// </summary>
        int? Quality { get; set; }

        /// <summary>
        /// Gets or sets the subsample ratio that will be used to encode the image.
        /// </summary>
        JpegSubsample? Subsample { get; set; }
    }

    public interface IPngFormat : IImageFormat
    {
        /// <summary>
        /// Gets or sets the number of bits per sample or per palette index (not per pixel).
        /// </summary>
        PngBitDepth? BitDepth { get; set; }

        /// <summary>
        /// Gets or sets the color type.
        /// </summary>
        PngColorType? ColorType { get; set; }

        /// <summary>
        /// Gets or sets the compression level 1-9.
        /// <remarks>Defaults to <see cref="PngCompressionLevel.DefaultCompression"/> 6.</remarks>
        /// </summary>
        PngCompressionLevel? CompressionLevel { get; set; }

        /// <summary>
        /// Gets or sets the gamma value that will be written to the image.
        /// </summary>
        float? Gamma { get; set; }

        /// <summary>
        /// Gets or sets the transparency threshold. Defaults to 255.
        /// </summary>
        byte? Threshold { get; set; }

        /// <summary>
        /// Gets or sets the quantizer for reducing the color count. Defaults to <see cref="QuantizationMethod.Wu"/>.
        /// </summary>
        QuantizationMethod? QuantizationMethod { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance should write an Adam7 interlaced image.
        /// </summary>
        PngInterlaceMode? InterlaceMode { get; set; }

        /// <summary>
        /// Gets or sets the chunk filter method. This allows to filter ancillary chunks.
        /// </summary>
        PngChunkFilter? ChunkFilter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether fully transparent pixels that may contain R, G, B values which are not 0,
        /// should be converted to transparent black, which can yield in better compression in some cases.
        /// </summary>
        PngTransparentColorMode? TransparentColorMode { get; set; }

        /// <summary>
        /// Gets a value indicating whether the metadata should be ignored when the image is being encoded.
        /// When set to true, all ancillary chunks will be skipped.
        /// </summary>
        bool IgnoreMetadata { get; set; }
    }

    public interface IGifFormat : IImageFormat
    {
        /// <summary>
        /// Gets the quantizer for reducing the color count. Defaults to <see cref="QuantizationMethod.Wu"/>.
        /// </summary>
        QuantizationMethod? QuantizationMethod { get; set; }

        /// <summary>
        /// Gets the color table mode: Global or local.
        /// </summary>
        GifColorTableMode? ColorTableMode { get; set; }
    }
}