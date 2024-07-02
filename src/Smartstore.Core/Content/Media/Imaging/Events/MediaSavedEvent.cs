namespace Smartstore.Core.Content.Media.Imaging
{
    /// <summary>
    /// Will be fired after a media upload has been saved.
    /// </summary>
    public class MediaSavedEvent(MediaFileInfo mediaFileInfo, string entityType)
    {

        /// <summary>
        /// The media file info of the saved image.
        /// </summary>
        public MediaFileInfo MediaFileInfo { get; } = Guard.NotNull(mediaFileInfo);

        /// <summary>
        /// The type of the entity that the image belongs to.
        /// </summary>
        public string EntityType { get; } = entityType;
    }
}