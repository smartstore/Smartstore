using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
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
        public MediaTag()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private MediaTag(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

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
