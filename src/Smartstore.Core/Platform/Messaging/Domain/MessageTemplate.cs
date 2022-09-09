using System.ComponentModel.DataAnnotations;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Messaging
{
    /// <summary>
    /// Represents a message template.
    /// </summary>
    [CacheableEntity]
    [LocalizedEntity("IsActive")]
    public partial class MessageTemplate : EntityWithAttributes, ILocalizedEntity, IStoreRestricted
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Required, StringLength(200)]
        public string Name { get; set; }

        [StringLength(500), Required]
        public string To { get; set; }

        [StringLength(500)]
        public string ReplyTo { get; set; }

        /// <summary>
        /// A comma separated list of required model types (e.g.: Product, Order, Customer, GiftCard).
        /// </summary>
        [StringLength(500)]
        public string ModelTypes { get; set; }

        [MaxLength]
        public string LastModelTree { get; set; }

        /// <summary>
        /// Gets or sets the BCC Email addresses.
        /// </summary>
        [StringLength(200)]
        public string BccEmailAddresses { get; set; }

        /// <summary>
        /// Gets or sets the subject.
        /// </summary>
        [StringLength(1000)]
        [LocalizedProperty]
        public string Subject { get; set; }

        /// <summary>
        /// Gets or sets the body.
        /// </summary>
        [MaxLength]
        [LocalizedProperty]
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the template is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the used email account identifier.
        /// </summary>
        [Required]
        public int EmailAccountId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores.
        /// </summary>
        public bool LimitedToStores { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether emails derived from the template are only send manually.
        /// </summary>
        public bool SendManually { get; set; }

        /// <summary>
        /// Gets or sets the attachment 1 file identifier.
        /// </summary>
        public int? Attachment1FileId { get; set; }

        /// <summary>
        /// Gets or sets the attachment 2 file identifier.
        /// </summary>
        public int? Attachment2FileId { get; set; }

        /// <summary>
        /// Gets or sets the attachment 3 file identifier.
        /// </summary>
        public int? Attachment3FileId { get; set; }
    }
}
