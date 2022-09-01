namespace Smartstore.Imaging
{
    /// <summary>
    /// Encapsulates properties that describe basic image information including dimensions, pixel type information
    /// and additional metadata.
    /// </summary>
    public interface IImageInfo
    {
        /// <summary>
        /// Image width.
        /// </summary>
        int Width { get; }

        /// <summary>
        /// Image width.
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Gets the color depth in number of bits per pixel (1, 4, 8, 16, 24, 32)
        /// </summary>
        byte BitDepth { get; }

        /// <summary>
        /// Gets the format of the image.
        /// </summary>
        IImageFormat Format { get; }

        /// <summary>
        /// Enumerates IPTC and EXIF metadata
        /// </summary>
        IEnumerable<ImageMetadataEntry> GetMetadata();
    }
}