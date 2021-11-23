using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Domain;

namespace Smartstore.Polls.Domain
{
    /// <summary>
    /// Represents a poll.
    /// </summary>
    [Table("Poll")]
    [Index(nameof(Name), Name = "IX_Title")]
    [Index(nameof(SystemKeyword), Name = "IX_SystemKeyword")]
    public partial class Poll : BaseEntity, IStoreRestricted
    {
        public Poll()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private Poll(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the language identifier.
        /// </summary>
        public int LanguageId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [StringLength(450), Required]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the system keyword.
        /// </summary>
        [StringLength(200)]
        public string SystemKeyword { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is published.
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity should be shown on homepage.
        /// </summary>
        public bool ShowOnHomePage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the anonymous votes are allowed.
        /// </summary>
        public bool AllowGuestsToVote { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the poll start date and time.
        /// </summary>
        public DateTime? StartDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the poll end date and time.
        /// </summary>
        public DateTime? EndDateUtc { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores.
        /// </summary>
        public bool LimitedToStores { get; set; }

        private ICollection<PollAnswer> _pollAnswers;
        /// <summary>
        /// Gets or sets the poll answers.
        /// </summary>
        public ICollection<PollAnswer> PollAnswers
        {
            get => LazyLoader?.Load(this, ref _pollAnswers) ?? (_pollAnswers ??= new HashSet<PollAnswer>());
            protected set => _pollAnswers = value;
        }

        private Language _language;
        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        public Language Language
        {
            get => _language ?? LazyLoader.Load(this, ref _language);
            set => _language = value;
        }
    }
}
