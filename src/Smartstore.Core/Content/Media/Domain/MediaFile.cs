using System.Collections.Frozen;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Content.Media
{
    internal class MediaFileMap : IEntityTypeConfiguration<MediaFile>
    {
        public void Configure(EntityTypeBuilder<MediaFile> builder)
        {
            builder.HasOne(c => c.Folder)
                .WithMany(c => c.Files)
                .HasForeignKey(c => c.FolderId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(c => c.MediaStorage)
                .WithMany()
                .HasForeignKey(c => c.MediaStorageId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(c => c.Tags)
                .WithMany(c => c.MediaFiles)
                .UsingEntity<Dictionary<string, object>>(
                    "MediaFile_Tag_Mapping",
                    c => c
                        .HasOne<MediaTag>()
                        .WithMany()
                        .HasForeignKey("MediaTag_Id")
                        .HasConstraintName("FK_dbo.MediaFile_Tag_Mapping_dbo.MediaTag_MediaTag_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c => c
                        .HasOne<MediaFile>()
                        .WithMany()
                        .HasForeignKey("MediaFile_Id")
                        .HasConstraintName("FK_dbo.MediaFile_Tag_Mapping_dbo.MediaFile_MediaFile_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c =>
                    {
                        c.HasIndex("MediaFile_Id");
                        c.HasKey("MediaFile_Id", "MediaTag_Id");
                    });
        }
    }

    /// <summary>
    /// Represents a media file.
    /// </summary>
    [Index(nameof(FolderId), nameof(MediaType), nameof(Extension), nameof(PixelSize), nameof(Deleted), Name = "IX_Media_MediaType")]
    [Index(nameof(FolderId), nameof(Extension), nameof(PixelSize), nameof(Deleted), Name = "IX_Media_Extension")]
    [Index(nameof(FolderId), nameof(PixelSize), nameof(Deleted), Name = "IX_Media_PixelSize")]
    [Index(nameof(FolderId), nameof(Name), nameof(Deleted), Name = "IX_Media_Name")]
    [Index(nameof(FolderId), nameof(Size), nameof(Deleted), Name = "IX_Media_Size")]
    // Exact Core port. UpdatedOnUtc was mistakenly missing in the original implementation.
    [Index(nameof(FolderId), nameof(Deleted), Name = "IX_Media_UpdatedOnUtc")]
    [Index(nameof(FolderId), nameof(Deleted), Name = "IX_Media_FolderId")]
    public partial class MediaFile : EntityWithAttributes, IMediaAware, ITransient, IAuditable, ISoftDeletable, ILocalizedEntity
    {
        #region static

        private static readonly FrozenSet<string> _outputAffectingProductProps = new string[]
        {
            nameof(MediaFile.FolderId),
            nameof(MediaFile.Name),
            nameof(MediaFile.Alt),
            nameof(MediaFile.Title),
            nameof(MediaFile.Hidden),
            nameof(MediaFile.Deleted)
        }.ToFrozenSet();

        public static IReadOnlyCollection<string> GetOutputAffectingPropertyNames()
        {
            return _outputAffectingProductProps;
        }

        #endregion

        /// <summary>
        /// Gets or sets the associated folder identifier.
        /// </summary>
        public int? FolderId { get; set; }

        private MediaFolder _folder;
        /// <summary>
        /// Gets or sets the associated folder.
        /// </summary>
        public MediaFolder Folder
        {
            get => _folder ?? LazyLoader.Load(this, ref _folder);
            set => _folder = value;
        }

        /// <summary>
        /// Gets or sets the SEO friendly name of the media file including file extension.
        /// </summary>
        [StringLength(300)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the localizable image ALT text.
        /// </summary>
        [StringLength(400)]
        public string Alt { get; set; }

        /// <summary>
        /// Gets or sets the localizable media file title text.
        /// </summary>
        [StringLength(400)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the (dotless) file extension.
        /// </summary>
        [StringLength(50)]
        public string Extension { get; set; }

        /// <summary>
        /// Gets or sets the file MIME type.
        /// </summary>
        [Required, StringLength(100)]
        public string MimeType { get; set; }

        /// <summary>
        /// Gets or sets the file media type (image, video, audio, document etc.).
        /// </summary>
        [Required, StringLength(20)]
        public string MediaType { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Gets or sets the total pixel size of an image (width * height).
        /// </summary>
        public int? PixelSize { get; set; }

        /// <summary>
        /// Gets or sets the file metadata as raw JSON dictionary (width, height, video length, EXIF etc.).
        /// </summary>
        [MaxLength]
        public string Metadata { get; set; }

        /// <summary>
        /// Gets or sets an internal admin comment.
        /// </summary>
        [StringLength(400)]
        public string AdminComment { get; set; }

        /// <summary>
        /// Gets or sets the image width (if file is an image).
        /// </summary>
        public int? Width { get; set; }

        /// <summary>
        /// Gets or sets the image height (if file is an image).
        /// </summary>
        public int? Height { get; set; }

        /// <inheritdoc/>
        public DateTime CreatedOnUtc { get; set; }

        /// <inheritdoc/>
        public DateTime UpdatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the file is transient/preliminary.
        /// </summary>
        public bool IsTransient { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the file has been soft deleted.
        /// </summary>
        [IgnoreDataMember]
        public bool Deleted { get; set; }

        bool ISoftDeletable.ForceDeletion
        {
            // User decides whether media file should be deleted permanently/physically.
            get => true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the file is hidden.
        /// </summary>
        [IgnoreDataMember]
        public bool Hidden { get; set; }

        /// <summary>
        /// Internally used for migration stuff only.
        /// 0 = needs migration because existed in previous versions already, 1 = was migrated by migrator, 2 = relations have been detected.
        /// </summary>
        [IgnoreDataMember]
        public int Version { get; set; } = 2;

        /// <inheritdoc/>
        public int? MediaStorageId { get; set; }

        private MediaStorage _mediaStorage;
        /// <inheritdoc/>
        [IgnoreDataMember]
        public MediaStorage MediaStorage
        {
            get => _mediaStorage ?? LazyLoader.Load(this, ref _mediaStorage);
            set => _mediaStorage = value;
        }

        private ICollection<MediaTag> _tags;
        /// <summary>
        /// Gets or sets the associated tags.
        /// </summary>
        public ICollection<MediaTag> Tags
        {
            get => _tags ?? LazyLoader.Load(this, ref _tags) ?? (_tags ??= new HashSet<MediaTag>());
            protected set => _tags = value;
        }

        private ICollection<MediaTrack> _tracks;
        /// <summary>
        /// Gets or sets the related entity tracks.
        /// </summary>
        public ICollection<MediaTrack> Tracks
        {
            get => _tracks ?? LazyLoader.Load(this, ref _tracks) ?? (_tracks ??= new HashSet<MediaTrack>());
            protected set => _tracks = value;
        }

        private ICollection<ProductMediaFile> _productMediaFiles;
        /// <summary>
        /// Gets or sets the product media files.
        /// </summary>
        public ICollection<ProductMediaFile> ProductMediaFiles
        {
            get => _productMediaFiles ?? LazyLoader.Load(this, ref _productMediaFiles) ?? (_productMediaFiles ??= new HashSet<ProductMediaFile>());
            protected set => _productMediaFiles = value;
        }
    }
}
