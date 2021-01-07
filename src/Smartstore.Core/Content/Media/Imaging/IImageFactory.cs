using System;
using System.IO;
using System.Threading.Tasks;

namespace Smartstore.Core.Content.Media.Imaging
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
        /// <param name="extension">The (dot-less) file extension.</param>
        /// <returns>An object that adapts the library specific format implementation.</returns>
        IImageFormat GetImageFormat(string extension);

        /// <summary>
        /// Loads an image by path.
        /// </summary>
        /// <param name="path">The full physical path to the image file.</param>
        /// <param name="preserveExif">Whether to preserve exif metadata. Defaults to false. </param>
        /// <returns>An object that adapts the library specific imaging implementation.</returns>
        Task<IProcessableImage> LoadImageAsync(string path, bool preserveExif = false);

        /// <summary>
        /// Loads an image by stream.
        /// </summary>
        /// <param name="stream">The stream that contains image data.</param>
        /// <param name="preserveExif">Whether to preserve exif metadata. Defaults to false. </param>
        /// <returns>An object that adapts the library specific imaging implementation.</returns>
        Task<IProcessableImage> LoadImageAsync(Stream stream, bool preserveExif = false);
    }
}
