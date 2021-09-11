using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Core.Seo;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Domain;

namespace Smartstore.Forums.Domain
{
    /// <summary>
    /// Represents a forum group.
    /// </summary>
    [Table("Forums_Group")]
    [Index(nameof(DisplayOrder), Name = "IX_DisplayOrder")]
    [Index(nameof(LimitedToStores), Name = "IX_LimitedToStores")]
    [Index(nameof(SubjectToAcl), Name = "IX_SubjectToAcl")]
    public partial class ForumGroup : BaseEntity, IAuditable, IStoreRestricted, IAclRestricted, ILocalizedEntity, ISlugSupported
    {
        public ForumGroup()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ForumGroup(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
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

        /// <inheritdoc/>
        public DateTime CreatedOnUtc { get; set; }

        /// <inheritdoc/>
        public DateTime UpdatedOnUtc { get; set; }

        /// <inheritdoc/>
        public bool LimitedToStores { get; set; }

        /// <inheritdoc/>
        public bool SubjectToAcl { get; set; }

        private ICollection<Forum> _forums;
        /// <summary>
        /// Gets or sets the collection of forums.
        /// </summary>
        public virtual ICollection<Forum> Forums
        {
            get => LazyLoader?.Load(this, ref _forums) ?? (_forums ??= new HashSet<Forum>());
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
