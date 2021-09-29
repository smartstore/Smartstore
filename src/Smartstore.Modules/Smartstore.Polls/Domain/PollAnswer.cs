using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Domain;

namespace Smartstore.Polls.Domain
{
    internal class PollAnswerMap : IEntityTypeConfiguration<PollAnswer>
    {
        public void Configure(EntityTypeBuilder<PollAnswer> builder)
        {
            builder.HasOne(c => c.Poll)
                .WithMany(c => c.PollAnswers)
                .HasForeignKey(c => c.PollId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents a poll answer.
    /// </summary>
    [Table("PollAnswer")] // Enables EF TPT inheritance
    public partial class PollAnswer : BaseEntity
    {
        public PollAnswer()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private PollAnswer(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the poll identifier.
        /// </summary>
        public int PollId { get; set; }

        /// <summary>
        /// Gets or sets the poll answer name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the current number of votes.
        /// </summary>
        public int NumberOfVotes { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int DisplayOrder { get; set; }

        private Poll _poll;
        /// <summary>
        /// Gets or sets the poll.
        /// </summary>
        [NotMapped]
        public Poll Poll
        {
            get => _poll ?? LazyLoader.Load(this, ref _poll);
            set => _poll = value;
        }

        private ICollection<PollVotingRecord> _pollVotingRecords;
        /// <summary>
        /// Gets or sets the poll voting records.
        /// </summary>
        [NotMapped]
        public virtual ICollection<PollVotingRecord> PollVotingRecords
        {
            get => _pollVotingRecords ?? LazyLoader.Load(this, ref _pollVotingRecords) ?? (_pollVotingRecords ??= new HashSet<PollVotingRecord>());
            protected set => _pollVotingRecords = value;
        }
    }
}
