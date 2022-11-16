using Smartstore.Imaging;

namespace Smartstore.Core.Content.Media.Imaging
{
    /// <summary>
    /// A service interface responsible for processing images.
    /// </summary>
    public interface IImageProcessor
    {
        /// <summary>
        /// The image factory adapter used to load and process images.
        /// </summary>
        IImageFactory Factory { get; }

        /// <summary>
        /// Processes an image
        /// </summary>
        /// <param name="query">Resize request</param>
        /// <param name="disposeOutput">Whether to dispose the output stream when <see cref="ProcessImageResult"/> instance gets disposed.</param>
        /// <returns>The resizing result encapsulated in <see cref="ProcessImageResult"/> type</returns>
        Task<ProcessImageResult> ProcessImageAsync(ProcessImageQuery query, bool disposeOutput = true);

        /// <summary>
        /// Gets the cumulative total processing time since app start in miliseconds
        /// </summary>
        long TotalProcessingTimeMs { get; }
    }
}