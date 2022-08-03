using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Smartstore.Core.Content.Media
{
    internal class DownloadMap : IEntityTypeConfiguration<Download>
    {
        public void Configure(EntityTypeBuilder<Download> builder)
        {
            builder.HasOne(c => c.MediaFile)
                .WithMany()
                .HasForeignKey(c => c.MediaFileId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    /// <summary>
    /// Represents a downloadable file.
    /// </summary>
    [Index(nameof(DownloadGuid), Name = "IX_DownloadGuid")]
    [Index(nameof(EntityId), nameof(EntityName), Name = "IX_EntityId_EntityName")]
    [Index(nameof(UpdatedOnUtc), nameof(IsTransient), Name = "IX_UpdatedOn_IsTransient")]
    public partial class Download : BaseEntity, ICloneable<Download>
    {
        public Download()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private Download(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets a GUID.
        /// </summary>
        public Guid DownloadGuid { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use the <see cref="DownloadUrl"/> property.
        /// </summary>
        public bool UseDownloadUrl { get; set; }

        /// <summary>
        /// Gets or sets a download URL.
        /// </summary>
        [StringLength(4000)]
        public string DownloadUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is transient/preliminary.
        /// </summary>
        public bool IsTransient { get; set; }

        /// <summary>
        /// Gets or sets the date of instance update.
        /// </summary>
        public DateTime UpdatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the media file identifier.
        /// </summary>
        public int? MediaFileId { get; set; }

        private MediaFile _mediaFile;
        /// <summary>
        /// Gets or sets the media file.
        /// </summary>
        public MediaFile MediaFile
        {
            get => _mediaFile ?? LazyLoader.Load(this, ref _mediaFile);
            set => _mediaFile = value;
        }

        /// <summary>
        /// Gets or sets the corresponding entity id.
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// Gets or sets the corresponding entity name.
        /// </summary>
        [StringLength(100)]
        public string EntityName { get; set; }

        /// <summary>
        /// Gets or sets the file version.
        /// </summary>
        [StringLength(30)]
        public string FileVersion { get; set; }

        /// <summary>
        /// Gets or sets informations about changes of the current download version.
        /// </summary>
        [MaxLength]
        public string Changelog { get; set; }

        /// <inheritdoc/>
        public Download Clone()
        {
            var download = new Download
            {
                DownloadGuid = Guid.NewGuid(),
                UseDownloadUrl = UseDownloadUrl,
                DownloadUrl = DownloadUrl,
                IsTransient = IsTransient,
                UpdatedOnUtc = DateTime.UtcNow,
                MediaFileId = MediaFileId,
                EntityId = EntityId,
                EntityName = EntityName,
                FileVersion = FileVersion,
                Changelog = Changelog
            };

            return download;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}