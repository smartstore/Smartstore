using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Content.Media;

namespace Smartstore.Core.Messaging
{
    internal class QueuedEmailAttachmentMap : IEntityTypeConfiguration<QueuedEmailAttachment>
    {
        public void Configure(EntityTypeBuilder<QueuedEmailAttachment> builder)
        {
            builder.HasOne(c => c.MediaFile)
                .WithMany()
                .HasForeignKey(c => c.MediaFileId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(c => c.MediaStorage)
                .WithMany()
                .HasForeignKey(c => c.MediaStorageId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents an e-mail attachment.
    /// </summary>
    [Index(nameof(QueuedEmailId), Name = "IX_QueuedEmailId")]
    [Index(nameof(MediaStorageId), Name = "IX_MediaStorageId")]
    [Index(nameof(MediaFileId), Name = "IX_MediaFileId")]
    public partial class QueuedEmailAttachment : BaseEntity, IMediaAware
    {
        public QueuedEmailAttachment()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private QueuedEmailAttachment(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the queued email identifier.
        /// </summary>
        public int QueuedEmailId { get; set; }

        private QueuedEmail _queuedEmail;
        /// <summary>
        /// Gets or sets the queued email entity instance.
        /// </summary>
        public QueuedEmail QueuedEmail
        {
            get => _queuedEmail ?? LazyLoader.Load(this, ref _queuedEmail);
            set => _queuedEmail = value;
        }

        /// <summary>
        /// Gets or sets the storage location.
        /// </summary>
        public EmailAttachmentStorageLocation StorageLocation { get; set; }

        /// <summary>
        /// A physical or virtual path to the file (only applicable if location is <c>Path</c>).
        /// </summary>
        [StringLength(1000)]
        public string Path { get; set; }

        /// <summary>
        /// The id of a <see cref="Smartstore.Core.Content.Media.MediaFile"/> record (only applicable if location is <c>FileReference</c>).
        /// </summary>
        public int? MediaFileId { get; set; }

        private MediaFile _mediaFile;
        /// <summary>
        /// Gets the file object.
        /// </summary>
        /// <remarks>
        /// This property is not named <c>Download</c> on purpose, because we're going to rename Download to File in a future release.
        /// </remarks>
        public MediaFile MediaFile
        {
            get => _mediaFile ?? LazyLoader.Load(this, ref _mediaFile);
            set => _mediaFile = value;
        }

        /// <summary>
        /// The attachment file name (without path).
        /// </summary>
        [Required, StringLength(200)]
        public string Name { get; set; }

        /// <summary>
        /// The attachment file's mime type, e.g. <c>application/pdf</c>.
        /// </summary>
        [Required, StringLength(200)]
        public string MimeType { get; set; }

        /// <summary>
        /// Gets or sets the media storage identifier (when location is BLOB).
        /// </summary>
        public int? MediaStorageId { get; set; }

        /// <summary>
        /// Whether attachment is embedded in mail body.
        /// </summary>
        public bool IsEmbedded { get; set; }

        /// <summary>
        /// Unique content id of attachment used to reference embedded attachment from HTML body.
        /// </summary>
        [StringLength(64)]
        public string ContentId { get; set; }

        private MediaStorage _mediaStorage;
        /// <summary>
        /// Gets or sets the media storage (when location is BLOB).
        /// </summary>
        public MediaStorage MediaStorage
        {
            get => _mediaStorage ?? LazyLoader.Load(this, ref _mediaStorage);
            set => _mediaStorage = value;
        }

        [NotMapped]
        int IMediaAware.Size { get; set; }
    }
}
