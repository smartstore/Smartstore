namespace Smartstore.Core.Media
{
    public interface IMediaFile
    {
        /// <summary>
        /// Gets or sets the media identifier.
        int MediaFileId { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        int DisplayOrder { get; set; }

        /// TODO: (mg) (core): Implement media file navigation property for IMediaFile.
        /// <summary>
        /// Gets the media file.
        /// </summary>
        //[JsonIgnore]
        //MediaFile MediaFile { get; set; }
    }
}
