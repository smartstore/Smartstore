using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Content.Media.Storage
{
    public interface IMediaStorageProvider : IProvider
    {
        /// <summary>
        /// Gets a value indicating whether the provider saves data in a remote cloud storage (e.g. Azure)
        /// </summary>
        bool IsCloudStorage { get; }

        /// <summary>
        /// Gets the size of the media item in bytes.
        /// </summary>
        /// <param name="mediaFile">Media file item</param>
        Task<long> GetLengthAsync(MediaFile mediaFile);

        /// <summary>
        /// Opens the media item for reading
        /// </summary>
        /// <param name="mediaFile">Media file item</param>
        Stream OpenRead(MediaFile mediaFile);

        /// <inheritdoc cref="OpenRead(MediaFile)"/>
        Task<Stream> OpenReadAsync(MediaFile mediaFile);

        /// <summary>
        /// Asynchronously loads media item data
        /// </summary>
        /// <param name="mediaFile">Media file item</param>
        Task<byte[]> LoadAsync(MediaFile mediaFile);

        /// <summary>
        /// Asynchronously saves media item data
        /// </summary>
        /// <param name="mediaFile">Media file item</param>
        /// <param name="item">The source item</param>
        Task SaveAsync(MediaFile mediaFile, MediaStorageItem item);

        /// <summary>
        /// Remove media storage item(s)
        /// </summary>
        /// <param name="mediaFiles">Media file items</param>
        Task RemoveAsync(params MediaFile[] mediaFiles);

        /// <summary>
        /// Changes the extension of the stored file if the provider supports
        /// </summary>
        /// <param name="mediaFile">Media file item</param>
        /// <param name="extension">The nex file extension</param>
        Task ChangeExtensionAsync(MediaFile mediaFile, string extension);
    }
}