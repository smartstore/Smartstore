using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Content.Media
{
    internal class MediaFolderMap : IEntityTypeConfiguration<MediaFolder>
    {
        public void Configure(EntityTypeBuilder<MediaFolder> builder)
        {
            builder
                .Property("Discriminator")
                .HasMaxLength(128);

            builder.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }

    /// <summary>
    /// Represents a media folder.
    /// </summary>
    [Index(nameof(ParentId), nameof(Name), Name = "IX_NameParentId", IsUnique = true)]
    [CacheableEntity]
    public partial class MediaFolder : EntityWithAttributes
    {
        public MediaFolder()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private MediaFolder(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the parent folder id.
        /// </summary>
        public int? ParentId { get; set; }

        private MediaFolder _parent;
        /// <summary>
        /// Gets or sets the parent folder.
        /// </summary>
        [IgnoreDataMember]
        public MediaFolder Parent
        {
            get => _parent ?? LazyLoader.Load(this, ref _parent);
            set => _parent = value;
        }

        /// <summary>
        /// Gets or sets the media folder name.
        /// </summary>
        [Required, StringLength(255)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the folder URL part slug.
        /// </summary>
        [StringLength(255)]
        public string Slug { get; set; } // TBD: localizable (?)

        /// <summary>
        /// Gets or sets a value indicating whether the folder can track relations to entities for containing files.
        /// </summary>
        public bool CanDetectTracks { get; set; }

        /// <summary>
        /// Gets or sets the media folder metadata as raw JSON string.
        /// </summary>
        [MaxLength]
        public string Metadata { get; set; }

        /// <summary>
        /// (Perf) Gets or sets the total number of files in this folder (excluding files from sub-folders).
        /// </summary>
        public int FilesCount { get; set; }

        private ICollection<MediaFolder> _children;
        /// <summary>
        /// Gets or sets the child folders.
        /// </summary>
        [IgnoreDataMember]
        public ICollection<MediaFolder> Children
        {
            get => _children ?? LazyLoader.Load(this, ref _children) ?? (_children ??= new HashSet<MediaFolder>());
            protected set => _children = value;
        }

        private ICollection<MediaFile> _files;
        /// <summary>
        /// Gets or sets the associated media files.
        /// </summary>
        [IgnoreDataMember]
        public ICollection<MediaFile> Files
        {
            get => _files ?? LazyLoader.Load(this, ref _files) ?? (_files ??= new HashSet<MediaFile>());
            protected set => _files = value;
        }
    }
}
