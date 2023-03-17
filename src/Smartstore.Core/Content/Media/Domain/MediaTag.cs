using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Represents a media tag.
    /// </summary>
    [Index(nameof(Name), Name = "IX_MediaTag_Name")]
    [CacheableEntity]
    public partial class MediaTag : BaseEntity
    {
        /// <summary>
        /// Gets or sets the media tag name.
        /// </summary>
        [Required, StringLength(100)]
        public string Name { get; set; }

        private ICollection<MediaFile> _mediaFiles;
        /// <summary>
        /// Gets or sets the associated media files.
        /// </summary>
        [IgnoreDataMember]
        public ICollection<MediaFile> MediaFiles
        {
            get => _mediaFiles ?? LazyLoader.Load(this, ref _mediaFiles) ?? (_mediaFiles ??= new HashSet<MediaFile>());
            protected set => _mediaFiles = value;
        }
    }
}
