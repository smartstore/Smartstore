namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Represents an entity with media storage.
    /// </summary>
    public interface IMediaAware
    {
        /// <summary>
        /// Gets or sets the media storage identifier.
        /// </summary>
        int? MediaStorageId { get; set; }

        /// <summary>
        /// Gets or sets the media storage.
        /// </summary>
        MediaStorage MediaStorage { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        int Size { get; set; }
    }
}
