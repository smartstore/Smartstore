#nullable enable

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Represents a detector for finding duplicate <see cref="MediaFile"/> entities.
    /// </summary>
    public interface IMediaDupeDetector : IDisposable
    {
        /// <summary>
        /// Tries to detect a duplicate file.
        /// </summary>
        /// <param name="fileName">The name of the file to search for.</param>
        /// <returns>Found duplicate file or <c>null</c> if it does not exist.</returns>
        Task<MediaFile?> DetectFileAsync(string fileName, CancellationToken cancelToken = default);

        /// <summary>
        /// Gets a unique file name.
        /// </summary>
        /// <param name="title">File title to be checked and to be used to generate unique file names.</param>
        /// <param name="extension">Dot-less file extension, e.g. <c>png</c>.</param>
        /// <returns>Unique file name, or <c>null</c> if no file with the name <paramref name="title"/> exists.</returns>
        Task<string> GetUniqueFileNameAsync(string title, string extension, CancellationToken cancelToken = default);
    }

    public static class IMediaDupeDetectorExtensions
    {
        /// <summary>
        /// Applies a unique file name to <paramref name="pathData."/> if it is not unique.
        /// </summary>
        /// <returns><c>true</c> if a unique file name was applied to <paramref name="pathData."/>, otherwise <c>false</c>.</returns>
        public static async Task<bool> CheckUniqueFileNameAsync(this IMediaDupeDetector detector, MediaPathData pathData, CancellationToken cancelToken = default)
        {
            Guard.NotNull(detector);
            Guard.NotNull(pathData);

            var uniqueName = await detector.GetUniqueFileNameAsync(pathData.FileTitle, pathData.Extension, cancelToken);
            if (uniqueName != null)
            {
                pathData.FileName = uniqueName;
                return true;
            }

            return false;
        }
    }
}
