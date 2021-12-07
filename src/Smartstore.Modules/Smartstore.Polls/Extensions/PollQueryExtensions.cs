using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;

namespace Smartstore.Polls.Extensions
{
    public static partial class PollQueryExtensions
    {
        /// <summary>
        /// Applies standard filter.
        /// </summary>
        /// <param name="storeId">Store identifier to apply filter by store restriction.</param>
        /// <param name="languageId">Language identifier to apply filter by <see cref="Language.Id"/>.</param>
        /// <param name="includeHidden">Applies filter by <see cref="Poll.Published"/>.</param>
        /// <returns>Poll query.</returns>
        public static IQueryable<Poll> ApplyStandardFilter(
            this IQueryable<Poll> query,
            int storeId,
            int languageId = 0,
            bool includeHidden = false)
        {
            Guard.NotNull(query, nameof(query));

            if (languageId != 0)
            {
                query = query.Where(b => b.LanguageId == languageId);
            }

            if (!includeHidden)
            {
                var utcNow = DateTime.UtcNow;
                query = query.Where(p => p.Published);
                query = query.Where(p => !p.StartDateUtc.HasValue || p.StartDateUtc <= utcNow);
                query = query.Where(p => !p.EndDateUtc.HasValue || p.EndDateUtc >= utcNow);
            }

            if (storeId > 0)
            {
                query = query.ApplyStoreFilter(storeId);
            }

            return query;
        }

        /// <summary>
        /// Applies standard filter.
        /// </summary>
        /// <param name="storeId">Store identifier to apply filter by store restriction.</param>
        /// <param name="languageId">Language identifier to apply filter by <see cref="Language.Id"/>.</param>
        /// <param name="includeHidden">Applies filter by <see cref="Poll.Published"/>.</param>
        /// <returns>Poll query.</returns>
        public static IQueryable<PollVotingRecord> ApplyPollFilter(this IQueryable<PollVotingRecord> query, int pollId)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotZero(pollId, nameof(pollId));

            var db = query.GetDbContext<SmartDbContext>();

            query =
                from pa in db.PollAnswers().AsNoTracking()
                join pvr in db.PollVotingRecords().AsNoTracking() on pa.Id equals pvr.PollAnswerId
                where pa.PollId == pollId
                orderby pa.DisplayOrder, pvr.CreatedOnUtc descending
                select pvr;

            return query;
        }

        /// <summary>
        /// Gets a value indicating whether given <paramref name="customerId"/> has already voted for <paramref name="pollId"/>.
        /// </summary>
        public static async Task<bool> GetAlreadyVotedAsync(this DbSet<PollAnswer> pollAnswers, int pollId, int customerId)
        {
            Guard.NotZero(pollId, nameof(pollId));
            Guard.NotZero(customerId, nameof(customerId));

            var alreadyVoted = await pollAnswers.AnyAsync(x => x.PollId == pollId && x.PollVotingRecords.Any(y => y.CustomerId == customerId));

            return alreadyVoted;
        }
    }
}
