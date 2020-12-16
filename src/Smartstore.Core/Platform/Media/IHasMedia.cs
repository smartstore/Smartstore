namespace Smartstore.Core.Media
{
    /// <summary>
    /// Represents an entity with media storage.
    /// </summary>
    public interface IHasMedia
    {
        /// <summary>
        /// Gets or sets the media storage identifier.
        /// </summary>
        int? MediaStorageId { get; set; }

        /// <summary>
        /// Gets or sets the media storage.
        /// </summary>
        MediaStorage MediaStorage { get; set; }
    }
}
