#nullable enable

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Represents a detector to find duplicate <see cref="MediaFile"/> entities.
    /// </summary>
    public interface IMediaDupeDetector : IOrdered
    {
        /// <summary>
        /// Gets a value indicating whether this detector caches <see cref="MediaFile"/> entities internally.
        /// </summary>
        bool UsesCache { get; }

        /// <summary>
        /// Gets a file or <c>null</c> if it does not exist.
        /// </summary>
        /// <param name="folderId"><see cref="MediaFolderNode.Id"/> of the folder to be searched.</param>
        /// <param name="fileName">The name of the file to search for.</param>
        /// <returns>Found file or <c>null</c> if it does not exist.</returns>
        Task<MediaFile?> GetFileAsync(int folderId, string fileName, CancellationToken cancelToken = default);

        /// <summary>
        /// Gets a list of all file names of a folder.
        /// </summary>
        /// <param name="folderId"><see cref="MediaFolderNode.Id"/> of the folder whose names are to be returned.</param>
        /// <returns>List of file names.</returns>
        Task<HashSet<string>> GetFileNamesAsync(int folderId, CancellationToken cancelToken = default);
    }
}
