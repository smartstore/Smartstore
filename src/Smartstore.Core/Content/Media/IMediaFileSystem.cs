using Smartstore.IO;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Storage abstraction for media files.
    /// </summary>
    public interface IMediaFileSystem : IFileSystem
    {
        /// <summary>
        /// Contains media storage and path configuration.
        /// </summary>
        IMediaStorageConfiguration StorageConfiguration { get; }

        /// <summary>
        /// Retrieves the public URL for a given file within the storage provider.
        /// </summary>
        /// <param name="file">The file to resolve the public url for.</param>
        /// <param name="forCloud">
        /// If <c>true</c> and the storage is in the cloud, returns the actual remote cloud URL to the resource.
        /// If <c>false</c>, retrieves an app relative URL to delegate further processing to the media middleware (which can handle remote files)
        /// </param>
        /// <returns>The public URL.</returns>
        string MapToPublicUrl(IFile file, bool forCloud = false);

        /// <summary>
        /// Retrieves the public URL for a given file within the storage provider.
        /// </summary>
        /// <param name="path">The relative path within the storage provider.</param>
        /// <param name="forCloud">
        /// If <c>true</c> and the storage is in the cloud, returns the actual remote cloud URL to the resource.
        /// If <c>false</c>, retrieves an app relative URL to delegate further processing to the media middleware (which can handle remote files)
        /// </param>
        /// <returns>The public URL.</returns>
        string MapToPublicUrl(string path, bool forCloud = false);

        /// <summary>
        /// Retrieves the path within the storage provider for a given public url.
        /// </summary>
        /// <param name="url">The virtual or public url of a file.</param>
        /// <returns>The storage path or <value>null</value> if the url is not in a correct format.</returns>
        string MapUrlToStoragePath(string url);
    }
}