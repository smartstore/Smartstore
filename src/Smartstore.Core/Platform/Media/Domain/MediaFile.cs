using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;
using Smartstore.Domain;

namespace Smartstore.Core.Media
{
    public class MediaFileMap : IEntityTypeConfiguration<MediaFile>
    {
        public void Configure(EntityTypeBuilder<MediaFile> builder)
        {
            builder.HasOne(c => c.MediaStorage)
                .WithMany()
                .HasForeignKey(c => c.MediaStorageId)
                .OnDelete(DeleteBehavior.SetNull);

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
    public partial class MediaFile : BaseEntity, ITransient, IHasMedia, IAuditable, ISoftDeletable, ILocalizedEntity
    {
        private readonly ILazyLoader _lazyLoader;

        public MediaFile()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private MediaFile(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets the associated folder identifier.
        /// </summary>
        public int? FolderId { get; set; }

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
        [JsonIgnore]
        public bool Deleted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the file is hidden.
        /// </summary>
        [JsonIgnore]
        public bool Hidden { get; set; }

        /// <summary>
        /// Internally used for migration stuff only.
        /// 0 = needs migration 'cause existed in previous versions already, 1 = was migrated by migrator, 2 = relations has been detected.
        /// </summary>
        [JsonIgnore]
        public int Version { get; set; } = 2;

        /// <inheritdoc/>
        public int? MediaStorageId { get; set; }

        private MediaStorage _mediaStorage;
        /// <inheritdoc/>
        [JsonIgnore]
        public MediaStorage MediaStorage
        {
            get => _lazyLoader?.Load(this, ref _mediaStorage) ?? _mediaStorage;
            set => _mediaStorage = value;
        }

        private ICollection<ProductMediaFile> _productMediaFiles;
        /// <summary>
        /// Gets or sets the product media files.
        /// </summary>
        public ICollection<ProductMediaFile> ProductMediaFiles
        {
            get => _lazyLoader?.Load(this, ref _productMediaFiles) ?? (_productMediaFiles ??= new HashSet<ProductMediaFile>());
            protected set => _productMediaFiles = value;
        }

        // TODO: (mg) (core): Complete implementation of media file entity.
    }
}
