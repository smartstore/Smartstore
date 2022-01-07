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
    /// Enumerates the chroma subsampling method applied to the image.
    /// </summary>
    public enum JpegSubsample
    {
        /// <summary>
        /// High Quality - Each of the three Y'CbCr components have the same sample rate,
        /// thus there is no chroma subsampling.
        /// </summary>
        Ratio444,

        /// <summary>
        /// Medium Quality - The horizontal sampling is halved and the Cb and Cr channels are only
        /// sampled on each alternate line.
        /// </summary>
        Ratio420
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
        /// Level 0. Equivalent to <see cref="NoCompression"/>.
        /// </summary>
        Level0 = 0,

        /// <summary>
        /// No compression. Equivalent to <see cref="Level0"/>.
        /// </summary>
        NoCompression = Level0,

        /// <summary>
        /// Level 1. Equivalent to <see cref="BestSpeed"/>.
        /// </summary>
        Level1 = 1,

        /// <summary>
        /// Best speed compression level.
        /// </summary>
        BestSpeed = Level1,

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
        /// Level 6. Equivalent to <see cref="DefaultCompression"/>.
        /// </summary>
        Level6 = 6,

        /// <summary>
        /// The default compression level. Equivalent to <see cref="Level6"/>.
        /// </summary>
        DefaultCompression = Level6,

        /// <summary>
        /// Level 7.
        /// </summary>
        Level7 = 7,

        /// <summary>
        /// Level 8.
        /// </summary>
        Level8 = 8,

        /// <summary>
        /// Level 9. Equivalent to <see cref="BestCompression"/>.
        /// </summary>
        Level9 = 9,

        /// <summary>
        /// Best compression level. Equivalent to <see cref="Level9"/>.
        /// </summary>
        BestCompression = Level9,
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
}