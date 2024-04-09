namespace Smartstore.Core.Content.Media.Imaging
{
    /// <summary>
    /// Will be fired after a media upload has been saved.
    /// </summary>
    public class MediaSavedEvent
    {
        public MediaSavedEvent(MediaFileInfo mediaFileInfo, string entityType)
        {
            MediaFileInfo = Guard.NotNull(mediaFileInfo);
            EntityType = entityType;
        }

        /// <summary>
        /// The media file info of the saved image.
        /// </summary>
        public MediaFileInfo MediaFileInfo { get; }

        /// <summary>
        /// The type of the entity that the image belongs to.
        /// </summary>
        public string EntityType { get; }
    }
}