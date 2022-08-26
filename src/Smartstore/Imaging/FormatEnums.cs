namespace Smartstore.Imaging
{
    /// <summary>
    /// Provides enumeration for the available image quantization methods.
    /// </summary>
    public enum QuantizationMethod
    {
        Octree,
        WebSafePalette,
        WernerPalette,
        Wu
    }

    /// <summary>
    /// Enumerates the available bits per pixel for bitmap format.
    /// </summary>
    public enum BmpBitsPerPixel : short
    {
        /// <summary>
        /// 1 bit per pixel.
        /// </summary>
        Pixel1 = 1,

        /// <summary>
        /// 4 bits per pixel.
        /// </summary>
        Pixel4 = 4,

        /// <summary>
        /// 8 bits per pixel. Each pixel consists of 1 byte.
        /// </summary>
        Pixel8 = 8,

        /// <summary>
        /// 16 bits per pixel. Each pixel consists of 2 bytes.
        /// </summary>
        Pixel16 = 16,

        /// <summary>
        /// 24 bits per pixel. Each pixel consists of 3 bytes.
        /// </summary>
        Pixel24 = 24,

        /// <summary>
        /// 32 bits per pixel. Each pixel consists of 4 bytes.
        /// </summary>
        Pixel32 = 32
    }

    /// <summary>
    /// Provides enumeration of available JPEG color types.
    /// </summary>
    public enum JpegColorType : byte
    {
        /// <summary>
        /// YCbCr (luminance, blue chroma, red chroma) color as defined in the ITU-T T.871 specification.
        /// Medium Quality - The horizontal sampling is halved and the Cb and Cr channels are only
        /// sampled on each alternate line.
        /// </summary>
        YCbCrRatio420 = 0,

        /// <summary>
        /// YCbCr (luminance, blue chroma, red chroma) color as defined in the ITU-T T.871 specification.
        /// High Quality - Each of the three Y'CbCr components have the same sample rate,
        /// thus there is no chroma subsampling.
        /// </summary>
        YCbCrRatio444 = 1,

        /// <summary>
        /// Single channel, luminance.
        /// </summary>
        Luminance = 5,

        /// <summary>
        /// The pixel data will be preserved as RGB without any sub sampling.
        /// </summary>
        Rgb = 6
    }

    /// <summary>
    /// Provides enumeration for the available color table modes.
    /// </summary>
    public enum GifColorTableMode
    {
        /// <summary>
        /// A single color table is calculated from the first frame and reused for subsequent frames.
        /// </summary>
        Global,

        /// <summary>
        /// A unique color table is calculated for each frame.
        /// </summary>
        Local
    }

    /// <summary>
    /// Provides enumeration for the available PNG bit depths.
    /// </summary>
    public enum PngBitDepth : byte
    {
        /// <summary>
        /// 1 bit per sample or per palette index (not per pixel).
        /// </summary>
        Bit1 = 1,

        /// <summary>
        /// 2 bits per sample or per palette index (not per pixel).
        /// </summary>
        Bit2 = 2,

        /// <summary>
        /// 4 bits per sample or per palette index (not per pixel).
        /// </summary>
        Bit4 = 4,

        /// <summary>
        /// 8 bits per sample or per palette index (not per pixel).
        /// </summary>
        Bit8 = 8,

        /// <summary>
        /// 16 bits per sample or per palette index (not per pixel).
        /// </summary>
        Bit16 = 16
    }

    /// <summary>
    /// Provides enumeration of available PNG color types.
    /// </summary>
    public enum PngColorType : byte
    {
        /// <summary>
        /// Each pixel is a grayscale sample.
        /// </summary>
        Grayscale = 0,

        /// <summary>
        /// Each pixel is an R,G,B triple.
        /// </summary>
        Rgb = 2,

        /// <summary>
        /// Each pixel is a palette index; a PLTE chunk must appear.
        /// </summary>
        Palette = 3,

        /// <summary>
        /// Each pixel is a grayscale sample, followed by an alpha sample.
        /// </summary>
        GrayscaleWithAlpha = 4,

        /// <summary>
        /// Each pixel is an R,G,B triple, followed by an alpha sample.
        /// </summary>
        RgbWithAlpha = 6
    }

    /// <summary>
    /// Provides enumeration of available PNG compression levels.
    /// </summary>
    public enum PngCompressionLevel
    {
        /// <summary>
        /// No compression (Level 0).
        /// </summary>
        NoCompression = 0,

        /// <summary>
        /// Best speed compression level (Level 1).
        /// </summary>
        BestSpeed = 1,

        /// <summary>
        /// Level 2.
        /// </summary>
        Level2 = 2,

        /// <summary>
        /// Level 3.
        /// </summary>
        Level3 = 3,

        /// <summary>
        /// Level 4.
        /// </summary>
        Level4 = 4,

        /// <summary>
        /// Level 5.
        /// </summary>
        Level5 = 5,

        /// <summary>
        /// The default compression level (Level 6).
        /// </summary>
        DefaultCompression = 6,

        /// <summary>
        /// Level 7.
        /// </summary>
        Level7 = 7,

        /// <summary>
        /// Level 8.
        /// </summary>
        Level8 = 8,

        /// <summary>
        /// Best compression level (Level 9).
        /// </summary>
        BestCompression = 9,
    }

    /// <summary>
    /// Provides enumeration of available PNG interlace modes.
    /// </summary>
    public enum PngInterlaceMode : byte
    {
        /// <summary>
        /// Non interlaced
        /// </summary>
        None = 0,

        /// <summary>
        /// Adam 7 interlacing.
        /// </summary>
        Adam7 = 1
    }

    /// <summary>
    /// Enum indicating how the transparency should be handled on encoding.
    /// </summary>
    public enum PngTransparentColorMode
    {
        /// <summary>
        /// The transparency will be kept as is.
        /// </summary>
        Preserve = 0,

        /// <summary>
        /// Converts fully transparent pixels that may contain R, G, B values which are not 0,
        /// to transparent black, which can yield in better compression in some cases.
        /// </summary>
        Clear = 1,
    }

    /// <summary>
    /// Provides enumeration of available PNG optimization methods.
    /// </summary>
    [Flags]
    public enum PngChunkFilter
    {
        /// <summary>
        /// With the None filter, all chunks will be written.
        /// </summary>
        None = 0,

        /// <summary>
        /// Excludes the physical dimension information chunk from encoding.
        /// </summary>
        ExcludePhysicalChunk = 1 << 0,

        /// <summary>
        /// Excludes the gamma information chunk from encoding.
        /// </summary>
        ExcludeGammaChunk = 1 << 1,

        /// <summary>
        /// Excludes the eXIf chunk from encoding.
        /// </summary>
        ExcludeExifChunk = 1 << 2,

        /// <summary>
        /// Excludes the tTXt, iTXt or zTXt chunk from encoding.
        /// </summary>
        ExcludeTextChunks = 1 << 3,

        /// <summary>
        /// All ancillary chunks will be excluded.
        /// </summary>
        ExcludeAll = ~None
    }

    /// <summary>
    /// Info about the webp file format used.
    /// </summary>
    public enum WebpFileFormatType
    {
        /// <summary>
        /// The lossless webp format.
        /// </summary>
        Lossless,

        /// <summary>
        /// The lossy webp format.
        /// </summary>
        Lossy,
    }

    /// <summary>
    /// Quality/speed trade-off for the webp encoding process (0=fast, 6=slower-better).
    /// </summary>
    public enum WebpEncodingMethod
    {
        /// <summary>
        /// Fastest (Level 0), but quality compromise.
        /// </summary>
        Fastest = 0,

        /// <summary>
        /// Level1.
        /// </summary>
        Level1 = 1,

        /// <summary>
        /// Level 2.
        /// </summary>
        Level2 = 2,

        /// <summary>
        /// Level 3.
        /// </summary>
        Level3 = 3,

        /// <summary>
        /// BestQuality trade off between speed and quality (Level 4).
        /// </summary>
        Default = 4,

        /// <summary>
        /// Level 5.
        /// </summary>
        Level5 = 5,

        /// <summary>
        /// Slowest option, but best quality (Level 6).
        /// </summary>
        BestQuality = 6
    }
}