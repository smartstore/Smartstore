using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Data.Hooks;
using Smartstore.Utilities;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Represents a media file reference.
    /// </summary>
    [Hookable(false)]
    [Index(nameof(Album), Name = "IX_Album")]
    [Index(nameof(MediaFileId), nameof(EntityId), nameof(EntityName), nameof(Property), Name = "IX_MediaTrack_Composite")]
    public partial class MediaTrack : BaseEntity, IEquatable<MediaTrack>
    {
        private int _mediaFileId;
        private int _entityId;
        private string _entityName;
        private string _property;
        private int? _hashCode;

        /// <summary>
        /// Gets or sets the media file identifier.
        /// </summary>
        public int MediaFileId
        {
            get => _mediaFileId;
            set
            {
                _mediaFileId = value;
                _hashCode = null;
            }
        }

        private MediaFile _mediaFile;
        /// <summary>
        /// Gets or sets the media file.
        /// </summary>
        [IgnoreDataMember]
        public MediaFile MediaFile
        {
            get => _mediaFile ?? LazyLoader.Load(this, ref _mediaFile);
            set => _mediaFile = value;
        }

        /// <summary>
        /// Gets or sets the origin album system name.
        /// </summary>
        [Required, StringLength(50)]
        public string Album { get; set; }

        /// <summary>
        /// Gets or sets the related entity identifier.
        /// </summary>
        public int EntityId
        {
            get => _entityId;
            set
            {
                _entityId = value;
                _hashCode = null;
            }
        }

        /// <summary>
        /// Gets or sets the related entity set name.
        /// </summary>
        [Required, StringLength(255)]
        public string EntityName
        {
            get => _entityName;
            set
            {
                _entityName = value;
                _hashCode = null;
            }
        }

        /// <summary>
        /// Gets or sets the media file property name in the tracked entity.
        /// </summary>
        [StringLength(255)]
        public string Property
        {
            get => _property;
            set
            {
                _property = value;
                _hashCode = null;
            }
        }

        /// <summary>
        /// Gets or sets the media track operation.
        /// </summary>
        [NotMapped]
        public MediaTrackOperation Operation { get; set; }

        protected override bool Equals(BaseEntity other)
        {
            return ((IEquatable<MediaTrack>)this).Equals(other as MediaTrack);
        }

        bool IEquatable<MediaTrack>.Equals(MediaTrack other)
        {
            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return MediaFileId == other.MediaFileId
                && EntityId == other.EntityId
                && string.Equals(EntityName, other.EntityName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Property, other.Property, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            if (_hashCode == null)
            {
                var combiner = HashCodeCombiner
                    .Start()
                    .Add(GetType())
                    .Add(MediaFileId)
                    .Add(EntityId)
                    .Add(EntityName)
                    .Add(Property);

                _hashCode = combiner.CombinedHash;
            }

            return _hashCode.Value;
        }

        public override string ToString()
        {
            return $"MediaTrack (MediaFileId: {MediaFileId}, EntityName: {EntityName}, EntityId: {EntityId}, Property: {Property})";
        }
    }
}
