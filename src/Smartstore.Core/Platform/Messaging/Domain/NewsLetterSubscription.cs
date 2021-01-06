using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Smartstore.Domain;

namespace Smartstore.Core.Messages
{
    /// <summary>
    /// Represents NewsLetterSubscription entity.
    /// </summary>
    [Index(nameof(Email), nameof(StoreId), Name = "IX_NewsletterSubscription_Email_StoreId")]
    [Index(nameof(Active), Name = "IX_Active")]
    [Table("NewsLetterSubscription")]
    public partial class NewsletterSubscription : EntityWithAttributes
    {
        /// <summary>
        /// Gets or sets the newsletter subscription GUID.
        /// </summary>
        public Guid NewsLetterSubscriptionGuid { get; set; }

        /// <summary>
        /// Gets or sets the subscriber email.
        /// </summary>
        [Required, StringLength(255)]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether subscription is active.
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets the date and time when subscription was created.
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the store identifier.
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
		/// Gets or sets the language identifier.
		/// </summary>
        public int WorkingLanguageId { get; set; }
    }
}
