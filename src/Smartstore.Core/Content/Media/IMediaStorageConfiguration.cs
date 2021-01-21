namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Contains media storage and path configuration.
    /// </summary>
    public interface IMediaStorageConfiguration
    {
        /// <summary>
        /// Gets the public base path to the media storage used to generate URLs for output HTML.
        /// e.g.: "media" (default), "static", "storage/files" etc. 
        /// </summary>
        string PublicPath { get; }

        /// <summary>
        /// Gets the storage path for media files either as app local relative path or
        /// as a fully qualified physical path to a shared location. E.g.:
        /// <list type="bullet">
        ///     <item>"Media" points to the subfolder named "Media" in the application root.</item>
        ///     <item>"F:\SharedMedia" points to a (mapped network) drive.</item>
        ///     <item>"\\Server1\SharedMedia" points to a network drive.</item>
        /// </list>
        /// <para>Default is <c>App_Data/Tenants/{Tenant}/Media</c></para>
        /// </summary>
        string StoragePath { get; }

        /// <summary>
        /// Checks whether <see cref="StoragePath"/> is a fully qualified physical path (e.g. 'D:\SharedMedia', \\MyServer\SharedMedia etc.)
        /// </summary>
        bool StoragePathIsAbsolute { get; }

        /// <summary>
        /// Gets the storage root physical path for media files.
        /// </summary>
        string RootPath { get; }

        /// <summary>
        /// Checks whether the underlying storage is remote, like 'Azure' for example. 
        /// </summary>
        bool IsCloudStorage { get; }
    }
}
