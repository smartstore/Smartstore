using Smartstore.IO;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Storage abstraction for media files.
    /// </summary>
    public interface IMediaFileSystem : IFileSystem
    {
        /// <summary>
        /// Checks whether the underlying storage is remote, like 'Azure' for example. 
        /// </summary>
        bool IsCloudStorage { get; }

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

        /// <summary>
        /// Gets the public base path to the media storage used to generate URLs for output HTML.
        /// e.g.: "media" (default), "static", "storage/files" etc. 
        /// </summary>
        string PublicPath { get; }

        /// <summary>
        /// Gets the storage path for media files
        /// either as app local relative path or as a fully qualified physical path to a shared location. E.g.:
        /// <list type="bullet">
        ///     <item>"Media" points to the subfolder named "Media" in the application root.</item>
        ///     <item>"F:\SharedMedia" points to a (mapped network) drive.</item>
        ///     <item>"\\Server1\SharedMedia" points to a network drive.</item>
        /// </list>
        /// <para>Default is <c>App_Data/Tenants/{Tenant}/Media</c></para>
        /// </summary>
        string StoragePath { get; }
    }
}