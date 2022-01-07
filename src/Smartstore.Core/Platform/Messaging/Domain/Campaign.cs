using System.ComponentModel.DataAnnotations;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Messaging
{
    /// <summary>
    /// Represents a campaign.
    /// </summary>
	public partial class Campaign : EntityWithAttributes, IStoreRestricted, IAclRestricted
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the subject.
        /// </summary>
        [Required]
        public string Subject { get; set; }

        /// <summary>
        /// Gets or sets the body.
        /// </summary>
        [Required, MaxLength]
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the date and time of entity creation.
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores.
        /// </summary>
        public bool LimitedToStores { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is subject to ACL.
        /// </summary>
        public bool SubjectToAcl { get; set; }
    }
}
