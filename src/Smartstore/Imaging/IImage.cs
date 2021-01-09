using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace Smartstore.Imaging
{
    /// <summary>
    /// Represents a processable image.
    /// </summary>
    public interface IProcessableImage : IImage
    {
        /// <summary>
        /// Transforms the image.
        /// </summary>
        /// <param name="transformer"></param>
        IImage Transform(Action<IImageTransformer> transformer);
    }

    /// <summary>
    /// Represents an image.
    /// </summary>
    public interface IImage : IImageInfo, IDisposable
    {
        /// <summary>
        /// Gets or sets the format of the image. 
        /// <see cref="Save(Stream, IImageFormat)"/> / <see cref="SaveAsync(Stream, IImageFormat)"/> will use
        /// the format to encode the image.
        /// </summary>
        new IImageFormat Format { get; set; }

        /// <summary>
        /// Gets the original width and height of the source image before any transform has been applied.
        /// </summary>
        Size SourceSize { get; }

        /// <summary>
        /// Saves the current image to the specified output stream.
        /// </summary>
        /// <param name="stream">The stream to save the image to.</param>
        /// <param name="format">The format to encode the image with. If <c>null</c>, the source format from <see cref="IImageInfo.Format"/> will be used.</param>
        /// <returns>The current <see cref="IImage"/>.</returns>
        IImage Save(Stream stream, IImageFormat format = null);

        /// <summary>
        /// Saves the current image to the specified output stream.
        /// </summary>
        /// <param name="stream">The stream to save the image to.</param>
        /// <param name="format">The format to encode the image with. If <c>null</c>, the source format from <see cref="IImageInfo.Format"/> will be used.</param>
        /// <returns>The current <see cref="IImage"/>.</returns>
        Task<IImage> SaveAsync(Stream stream, IImageFormat format = null);
    }
}