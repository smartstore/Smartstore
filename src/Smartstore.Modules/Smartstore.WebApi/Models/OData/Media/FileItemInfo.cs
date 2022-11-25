using System.Drawing;

namespace Smartstore.Web.Api.Models.Media
{
    /// <summary>
    /// Represents a media file.
    /// </summary>
    public partial class FileItemInfo
    {
        /// <summary>
        /// The MediaFile identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The relative path without the file part (but with trailing slash).
        /// </summary>
        /// <example>catalog</example>
        public string Directory { get; set; }

        /// <summary>
        /// The path of the file.
        /// </summary>
        /// <example>content/my-file.jpg</example>
        public string Path { get; set; }

        /// <summary>
        /// The URL of the file.
        /// </summary>
        /// <example>media/40/catalog/my-picture.jpg</example>
        public string Url { get; set; }

        /// <summary>
        /// The thumbnail URL of the file.
        /// </summary>
        /// <example>media/40/catalog/my-picture.jpg?size=256</example>
        public string ThumbUrl { get; set; }

        /// <summary>
        /// Gets or sets the SEO friendly name of the media file including file extension.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the (dotless) file extension.
        /// </summary>
        public string Extension { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        public long Length { get; set; }

        /// <summary>
        /// Gets or sets the dimwnsion (width and height) of the file.
        /// </summary>
        public Size Size { get; set; }

        /// <summary>
        /// Gets or sets the associated folder identifier.
        /// </summary>
        public int? FolderId { get; set; }

        /// <summary>
        /// Gets or sets the file MIME type.
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// Gets or sets the file media type (image, video, audio, document etc.).
        /// </summary>
        public string MediaType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the file is transient/preliminary.
        /// </summary>
        public bool IsTransient { get; set; }

        /// <summary>
        /// Gets or sets the date and time of file creation (in UTC).
        /// </summary>
        public DateTimeOffset CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the date and time of file update (in UTC).
        /// </summary>
        public DateTimeOffset LastModified { get; set; }

        /// <summary>
        /// Gets or sets the localizable image ALT text.
        /// </summary>
        public string Alt { get; set; }

        /// <summary>
        /// Gets or sets the localizable media file title text.
        /// </summary>
        public string TitleAttribute { get; set; }

        /// <summary>
        /// Gets or sets an internal admin comment.
        /// </summary>
        public string AdminComment { get; set; }
    }
}
