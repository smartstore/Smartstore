using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Core.Content.Seo;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Domain;

namespace Smartstore.Core.Content.Forums.Domain
{
    /// <summary>
    /// Represents a forum group.
    /// </summary>
    [Table("Forums_Group")]
    // TODO: (mh) (core) Is this correct or can we get rid of one Index for DisplayOrder?
    [Index(nameof(DisplayOrder), Name = "IX_Forums_Group_DisplayOrder")]
    [Index(nameof(DisplayOrder), Name = "IX_LimitedToStores")]
    [Index(nameof(LimitedToStores), Name = "IX_LimitedToStores")]
    [Index(nameof(SubjectToAcl), Name = "IX_SubjectToAcl")]
    public partial class ForumGroup : BaseEntity, IAuditable, IStoreRestricted, IAclRestricted, ILocalizedEntity, ISlugSupported
    {
        private readonly ILazyLoader _lazyLoader;

        public ForumGroup()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ForumGroup(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Required, StringLength(200)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [MaxLength]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the date and time of instance creation.
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time of instance update.
        /// </summary>
        public DateTime UpdatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores.
        /// </summary>
        public bool LimitedToStores { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is subject to ACL.
        /// </summary>
        public bool SubjectToAcl { get; set; }

        private ICollection<Forum> _forums;
        /// <summary>
        /// Gets or sets the collection of Forums.
        /// </summary>
        public virtual ICollection<Forum> Forums
        {
            get => _lazyLoader?.Load(this, ref _forums) ?? _forums;
            protected set => _forums = value;
        }

        /// <inheritdoc/>
        public string GetDisplayName()
        {
            return Name;
        }

        /// <inheritdoc/>
        public string GetDisplayNameMemberName()
        {
            return nameof(Name);
        }
    }
}
