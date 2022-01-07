using System.ComponentModel.DataAnnotations;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Represents the raw media data.
    /// </summary>
    public partial class MediaStorage : BaseEntity
    {
        /// <summary>
        /// Gets or sets the media binary data.
        /// </summary>
        [Required, MaxLength]
        public byte[] Data { get; set; }
    }
}
