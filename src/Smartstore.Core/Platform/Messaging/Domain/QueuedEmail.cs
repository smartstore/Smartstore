using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Smartstore.Core.Messaging
{
    /// <summary>
    /// Represents a queued email item.
    /// </summary>
    [Index(nameof(CreatedOnUtc), Name = "[IX_QueuedEmail_CreatedOnUtc]")]
    [Index(nameof(EmailAccountId), Name = "IX_EmailAccountId")]
    public partial class QueuedEmail : BaseEntity
    {
        /// <summary>
        /// Gets or sets the priority.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Gets or sets the From property.
        /// </summary>
        [Required, StringLength(500)]
        public string From { get; set; }

        /// <summary>
        /// Gets or sets the To property.
        /// </summary>
        [Required, StringLength(500)]
        public string To { get; set; }

        /// <summary>
        /// Gets or sets the ReplyTo property.
        /// </summary>
        [StringLength(500)]
        public string ReplyTo { get; set; }

        /// <summary>
        /// Gets or sets the CC.
        /// </summary>
        [StringLength(500)]
        public string CC { get; set; }

        /// <summary>
        /// Gets or sets the Bcc.
        /// </summary>
        [StringLength(500)]
        public string Bcc { get; set; }

        /// <summary>
        /// Gets or sets the subject.
        /// </summary>
        [StringLength(1000)]
        public string Subject { get; set; }

        /// <summary>
        /// Gets or sets the body.
        /// </summary>
        [MaxLength, NonSummary]
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the date and time of item creation in UTC.
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the send tries.
        /// </summary>
        public int SentTries { get; set; }

        /// <summary>
        /// Gets or sets the sent date and time.
        /// </summary>
        public DateTime? SentOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the used email account identifier.
        /// </summary>
        public int EmailAccountId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether emails are only send manually.
        /// </summary>
        public bool SendManually { get; set; }

        private EmailAccount _emailAccount;
        /// <summary>
        /// Gets the email account.
        /// </summary>
        public EmailAccount EmailAccount
        {
            get => _emailAccount ?? LazyLoader.Load(this, ref _emailAccount);
            set => _emailAccount = value;
        }

        private ICollection<QueuedEmailAttachment> _attachments;
        /// <summary>
        /// Gets or sets the collection of attachments.
        /// </summary>
        public ICollection<QueuedEmailAttachment> Attachments
        {
            get => _attachments ?? LazyLoader.Load(this, ref _attachments) ?? (_attachments ??= []);
            protected set => _attachments = value;
        }
    }
}
