using Microsoft.EntityFrameworkCore;
using Smartstore.Polls.Domain;
using Smartstore.Core.Data;

namespace Smartstore.Polls
{
    public static class SmartDbContextExtensions
    {
        public static DbSet<Poll> Polls(this SmartDbContext db)
            => db.Set<Poll>();
        public static DbSet<PollAnswer> PollAnswers(this SmartDbContext db)
            => db.Set<PollAnswer>();
        public static DbSet<PollVotingRecord> PollVotingRecords(this SmartDbContext db)
            => db.Set<PollVotingRecord>();
    }
}
