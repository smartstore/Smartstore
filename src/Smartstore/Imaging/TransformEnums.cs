namespace Smartstore.Imaging
{
    /// <summary>
    /// Provides enumeration over how the image should be rotated.
    /// </summary>
    public enum RotateMode
    {
        /// <summary>
        /// Do not rotate the image.
        /// </summary>
        None,

        /// <summary>
        /// Rotate the image by 90 degrees clockwise.
        /// </summary>
        Rotate90 = 90,

        /// <summary>
        /// Rotate the image by 180 degrees clockwise.
        /// </summary>
        Rotate180 = 180,

        /// <summary>
        /// Rotate the image by 270 degrees clockwise.
        /// </summary>
        Rotate270 = 270
    }

    /// <summary>
    /// Provides enumeration over how a image should be flipped.
    /// </summary>
    public enum FlipMode
    {
        /// <summary>
        /// Don't flip the image.
        /// </summary>
        None,

        /// <summary>
        /// Flip the image horizontally.
        /// </summary>
        Horizontal,

        /// <summary>
        /// Flip the image vertically.
        /// </summary>
        Vertical
    }

    /// <summary>
    /// Enumerates the various color blending modes.
    /// </summary>
    public enum PixelColorBlendingMode
    {
        /// <summary>
        /// Default blending mode, also known as "Normal" or "Alpha Blending"
        /// </summary>
        Normal = 0,

        /// <summary>
        /// Blends the 2 values by multiplication.
        /// </summary>
        Multiply,

        /// <summary>
        /// Blends the 2 values by addition.
        /// </summary>
        Add,

        /// <summary>
        /// Blends the 2 values by subtraction.
        /// </summary>
        Subtract,

        /// <summary>
        /// Multiplies the complements of the backdrop and source values, then complements the result.
        /// </summary>
        Screen,

        /// <summary>
        /// Selects the minimum of the backdrop and source values.
        /// </summary>
        Darken,

        /// <summary>
        /// Selects the max of the backdrop and source values.
        /// </summary>
        Lighten,

        /// <summary>
        /// Multiplies or screens the values, depending on the backdrop vector values.
        /// </summary>
        Overlay,

        /// <summary>
        /// Multiplies or screens the colors, depending on the source value.
        /// </summary>
        HardLight
    }

    /// <summary>
    /// Enumerates the various alpha composition modes.
    /// </summary>
    public enum PixelAlphaCompositionMode
    {
        /// <summary>
        /// Returns the destination over the source.
        /// </summary>
        SrcOver = 0,

        /// <summary>
        /// Returns the source colors.
        /// </summary>
        Src,

        /// <summary>
        /// Returns the source over the destination.
        /// </summary>
        SrcAtop,

        /// <summary>
        /// The source where the destination and source overlap.
        /// </summary>
        SrcIn,

        /// <summary>
        /// The destination where the destination and source overlap.
        /// </summary>
        SrcOut,

        /// <summary>
        /// The destination where the source does not overlap it.
        /// </summary>
        Dest,

        /// <summary>
        /// The source where they don't overlap otherwise dest in overlapping parts.
        /// </summary>
        DestAtop,

        /// <summary>
        /// The destination over the source.
        /// </summary>
        DestOver,

        /// <summary>
        /// The destination where the destination and source overlap.
        /// </summary>
        DestIn,

        /// <summary>
        /// The source where the destination and source overlap.
        /// </summary>
        DestOut,

        /// <summary>
        /// The clear.
        /// </summary>
        Clear,

        /// <summary>
        /// Clear where they overlap.
        /// </summary>
        Xor
    }

    /// <summary>
    /// Enumerates known dithering algorithms.
    /// </summary>
    public enum DitheringMode : byte
    {
        /// <summary>
        /// 3x3 ordered dithering matrix.
        /// </summary>
        Ordered3x3,

        /// <summary>
        /// 8x8 Bayer ordered dithering matrix.
        /// </summary>
        Bayer8x8,

        /// <summary>
        /// Atkinson error dithering algorithm.
        /// </summary>
        Atkinson,

        /// <summary>
        /// Burks error dithering algorithm.
        /// </summary>
        Burks,

        /// <summary>
        /// FloydSteinberg error dithering algorithm.
        /// </summary>
        FloydSteinberg,

        /// <summary>
        /// Stucki error dithering algorithm.
        /// </summary>
        Stucki
    }
}
