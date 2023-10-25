#nullable enable

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Represents a detector for finding duplicate <see cref="MediaFile"/> entities.
    /// </summary>
    public interface IMediaDupeDetector
    {
        /// <summary>
        /// Tries to detects a duplicate file.
        /// </summary>
        /// <param name="fileName">The name of the file to search for.</param>
        /// <returns>Found duplicate file or <c>null</c> if it does not exist.</returns>
        Task<MediaFile?> DetectFileAsync(string fileName, CancellationToken cancelToken = default);

        /// <summary>
        /// Gets a list of all file names in the folder.
        /// </summary>
        /// <returns>Set of file names.</returns>
        Task<HashSet<string>> GetAllFileNamesAsync(CancellationToken cancelToken = default);
    }
}
