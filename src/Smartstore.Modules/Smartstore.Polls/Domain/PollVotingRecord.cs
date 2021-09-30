using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Identity;

namespace Smartstore.Polls.Domain
{
    internal class PollVotingMap : IEntityTypeConfiguration<PollVotingRecord>
    {
        public void Configure(EntityTypeBuilder<PollVotingRecord> builder)
        {
            builder.HasOne(c => c.PollAnswer)
                .WithMany(c => c.PollVotingRecords)
                .HasForeignKey(c => c.PollAnswerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents a poll voting record.
    /// </summary>
    [Table("PollVotingRecord")] // Enables EF TPT inheritance
    public partial class PollVotingRecord : CustomerContent
    {
        public PollVotingRecord()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private PollVotingRecord(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the poll answer identifier.
        /// </summary>
        public int PollAnswerId { get; set; }

        private PollAnswer _pollAnswer;
        /// <summary>
        /// Gets or sets the poll answer.
        /// </summary>
        public PollAnswer PollAnswer
        {
            get => _pollAnswer ?? LazyLoader.Load(this, ref _pollAnswer);
            set => _pollAnswer = value;
        }
    }
}
