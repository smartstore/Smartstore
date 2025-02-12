namespace Smartstore.Core.Content.Media.Imaging
{
    /// <summary>
    /// Will be fired after a file has been saved.
    /// </summary>
    /// <remarks>
    /// In order to include <paramref name="entityName"/> when uploading in the backend, 
    /// this event is currently not fired in all cases of saving a media file.
    /// </remarks>
    public class MediaSavedEvent(MediaFileInfo mediaFileInfo, string entityName)
    {
        /// <summary>
        /// The media file info of the saved image.
        /// </summary>
        public MediaFileInfo MediaFileInfo { get; } = Guard.NotNull(mediaFileInfo);

        /// <summary>
        /// The name of the entity that the image belongs to. <c>null</c> if unknown.
        /// </summary>
        public string EntityName { get; } = entityName;
    }
}