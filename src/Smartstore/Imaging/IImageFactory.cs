namespace Smartstore.Imaging
{
    /// <summary>
    /// A provider interface for imaging libraries.
    /// </summary>
    public interface IImageFactory
    {
        /// <summary>
        /// Determines whether the given file name is processable by the image provider
        /// </summary>
        /// <param name="extension">The file extension to check.</param>
        /// <returns>A value indicating whether processing is possible</returns>
        bool IsSupportedImage(string extension);

        /// <summary>
        /// Resolves the image format instance for a given file extension.
        /// </summary>
        /// <param name="extension">The file extension (with or without dot).</param>
        /// <returns>An object that adapts the library specific format implementation.</returns>
        IImageFormat FindFormatByExtension(string extension);

        /// <summary>
        /// By reading the header on the provided stream this calculates the images format type.
        /// </summary>
        /// <param name="stream">The image stream to read the header from.</param>
        /// <exception cref="ArgumentNullException">The stream is null.</exception>
        /// <exception cref="NotSupportedException">The stream is not readable.</exception>
        /// <returns>The format type or null if none found.</returns>
        IImageFormat DetectFormat(Stream stream);

        /// <summary>
        /// By reading the header on the provided stream this calculates the images format type.
        /// </summary>
        /// <param name="stream">The image stream to read the header from.</param>
        /// <exception cref="ArgumentNullException">The stream is null.</exception>
        /// <exception cref="NotSupportedException">The stream is not readable.</exception>
        /// <returns>The format type or null if none found.</returns>
        Task<IImageFormat> DetectFormatAsync(Stream stream);

        /// <summary>
        /// Reads the raw image information from the specified stream without fully decoding it.
        /// </summary>
        /// <param name="stream">The image stream to read the header from.</param>
        /// <exception cref="ArgumentNullException">The stream is null.</exception>
        /// <exception cref="NotSupportedException">The stream is not readable.</exception>
        /// <returns>
        /// The <see cref="IImageInfo"/> or null if a suitable info detector is not found.
        /// </returns>
        IImageInfo DetectInfo(Stream stream);

        /// <summary>
        /// Reads the raw image information from the specified stream without fully decoding it.
        /// </summary>
        /// <param name="stream">The image stream to read the header from.</param>
        /// <exception cref="ArgumentNullException">The stream is null.</exception>
        /// <exception cref="NotSupportedException">The stream is not readable.</exception>
        /// <returns>
        /// The <see cref="IImageInfo"/> or null if a suitable info detector is not found.
        /// </returns>
        Task<IImageInfo> DetectInfoAsync(Stream stream);

        /// <summary>
        /// Loads an image by path.
        /// </summary>
        /// <param name="path">The full physical path to the image file.</param>
        /// <returns>An object that adapts the library specific imaging implementation.</returns>
        IProcessableImage Load(string path);

        /// <summary>
        /// Loads an image by stream.
        /// </summary>
        /// <param name="stream">The stream that contains image data.</param>
        /// <returns>An object that adapts the library specific imaging implementation.</returns>
        IProcessableImage Load(Stream stream);

        /// <summary>
        /// Loads an image by stream.
        /// </summary>
        /// <param name="stream">The stream that contains image data.</param>
        /// <returns>An object that adapts the library specific imaging implementation.</returns>
        Task<IProcessableImage> LoadAsync(Stream stream);

        /// <summary>
        /// Releases all retained resources not being in use. Eg: by resetting array pools and letting GC to free the arrays.
        /// </summary>
        void ReleaseMemory();
    }
}
