using System.Runtime.Serialization;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Represents a media album.
    /// </summary>
    public partial class MediaAlbum : MediaFolder
    {
        /// <summary>
        /// Gets or sets the display name resource key.
        /// </summary>
        [IgnoreDataMember]
        public string ResKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include the folder paths in file URL generation.
        /// </summary>
        public bool IncludePath { get; set; }

        /// <summary>
        /// Gets or sets the media album display order.
        /// </summary>
        public int? Order { get; set; }
    }
}
