namespace Smartstore.Core.Content.Media.Imaging
{
    /// <summary>
    /// Will be fired after a file has been saved.
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

        // TODO: (mg) (ai) Hack, totally wrong approach! The entity type is not known at the time of saving the media file, only during tracking.
        /// <summary>
        /// The type of the entity that the image belongs to.
        /// </summary>
        public string EntityType { get; }
    }
}