namespace Smartstore.Imaging
{
    /// <summary>
    /// Defines the contract for an image format.
    /// </summary>
    public interface IImageFormat
    {
        /// <summary>
        /// Gets the name that describes this image format.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the default (dotless) file extension.
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

    public interface IBmpFormat : IImageFormat
    {
        /// <summary>
        /// Gets or sets the number of bits per pixel.
        /// </summary>
        BmpBitsPerPixel? BitsPerPixel { get; set; }

        /// <summary>
        /// Gets or sets the quantizer for reducing the color count for 8-Bit images.
        /// Defaults to <see cref="QuantizationMethod.Octree"/>.
        /// </summary>
        QuantizationMethod? QuantizationMethod { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the encoder should support transparency.
        /// Note: Transparency support only works together with 32 bits per pixel. This option will
        /// change the default behavior of the encoder of writing a bitmap version 3 info header with no compression.
        /// Instead a bitmap version 4 info header will be written with the BITFIELDS compression.
        /// </summary>
        bool? SupportTransparency { get; set; }
    }

    public interface IJpegFormat : IImageFormat
    {
        /// <summary>
        /// Gets or sets the quality that will be used to encode the image. Quality
        /// index must be between 0 and 100 (compression from max to min).
        /// </summary>
        int? Quality { get; set; }

        /// <summary>
        /// Gets or sets the color type that will be used to encode the image.
        /// </summary>
        JpegColorType? ColorType { get; set; }
    }

    public interface IPngFormat : IImageFormat
    {
        /// <summary>
        /// Gets or sets the number of bits per sample or per palette index (not per pixel).
        /// </summary>
        PngBitDepth? BitDepth { get; set; }

        /// <summary>
        /// Gets or sets the color type that will be used to encode the image.
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

    public interface IWebpFormat : IImageFormat
    {
        /// <summary>
        /// Gets the webp file format used. Either lossless or lossy.
        /// Defaults to lossy.
        /// </summary>
        WebpFileFormatType? FileFormat { get; }

        /// <summary>
        /// Gets the compression quality. Between 0 and 100.
        /// For lossy, 0 gives the smallest size and 100 the largest. For lossless,
        /// this parameter is the amount of effort put into the compression: 0 is the fastest but gives larger
        /// files compared to the slowest, but best, 100.
        /// Defaults to 75.
        /// </summary>
        int? Quality { get; set; }

        /// <summary>
        /// Gets the encoding method to use. Its a quality/speed trade-off (0=fast, 6=slower-better).
        /// Defaults to 4.
        /// </summary>
        WebpEncodingMethod? Method { get; }

        /// <summary>
        /// Gets a value indicating whether the alpha plane should be compressed with Webp lossless format.
        /// Defaults to true.
        /// </summary>
        bool? UseAlphaCompression { get; }

        /// <summary>
        /// Gets the number of entropy-analysis passes (in [1..10]).
        /// Defaults to 1.
        /// </summary>
        int? EntropyPasses { get; }

        /// <summary>
        /// Gets the amplitude of the spatial noise shaping. Spatial noise shaping (or sns for short) refers to a general collection of built-in algorithms
        /// used to decide which area of the picture should use relatively less bits, and where else to better transfer these bits.
        /// The possible range goes from 0 (algorithm is off) to 100 (the maximal effect).
        /// Defaults to 50.
        /// </summary>
        int? SpatialNoiseShaping { get; }

        /// <summary>
        /// Gets the strength of the deblocking filter, between 0 (no filtering) and 100 (maximum filtering).
        /// A value of 0 will turn off any filtering. Higher value will increase the strength of the filtering process applied after decoding the picture.
        /// The higher the value the smoother the picture will appear.
        /// Typical values are usually in the range of 20 to 50.
        /// Defaults to 60.
        /// </summary>
        int? FilterStrength { get; }

        /// <summary>
        /// Gets a value indicating whether near lossless mode should be used.
        /// This option adjusts pixel values to help compressibility, but has minimal impact on the visual quality.
        /// </summary>
        bool? NearLossless { get; }

        /// <summary>
        /// Gets the quality of near-lossless image preprocessing. The range is 0 (maximum preprocessing) to 100 (no preprocessing, the default).
        /// The typical value is around 60. Note that lossy with -q 100 can at times yield better results.
        /// </summary>
        int? NearLosslessQuality { get; }
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