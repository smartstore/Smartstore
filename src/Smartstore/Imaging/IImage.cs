using System.Drawing;

namespace Smartstore.Imaging
{
    /// <summary>
    /// Represents a processable image.
    /// </summary>
    public interface IProcessableImage : IImage
    {
        /// <summary>
        /// Transforms the source image by applying the image operations to it.
        /// </summary>
        /// <param name="operation">The operation to perform on the source.</param>
        void Transform(Action<IImageTransformer> operation);

        /// <summary>
        /// Creates a deep clone of the current image. The clone is then transformed by the given operation.
        /// </summary>
        /// <param name="operation">The operation to perform on the clone.</param>
        /// <returns>The new <see cref="IProcessableImage"/>.</returns>
        IProcessableImage Clone(Action<IImageTransformer> operation);
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