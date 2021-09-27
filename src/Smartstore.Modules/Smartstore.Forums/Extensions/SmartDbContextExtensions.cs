using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums
{
    public static class SmartDbContextExtensions
    {
        public static DbSet<ForumGroup> ForumGroups(this SmartDbContext db)
            => db.Set<ForumGroup>();

        public static DbSet<ForumTopic> ForumTopics(this SmartDbContext db)
            => db.Set<ForumTopic>();

        public static DbSet<Forum> Forums(this SmartDbContext db)
            => db.Set<Forum>();

        public static DbSet<ForumPost> ForumPosts(this SmartDbContext db)
            => db.Set<ForumPost>();

        public static DbSet<ForumPostVote> ForumPostVotes(this SmartDbContext db)
            => db.Set<ForumPostVote>();

        public static DbSet<ForumSubscription> ForumSubscriptions(this SmartDbContext db)
            => db.Set<ForumSubscription>();

        public static DbSet<PrivateMessage> PrivateMessages(this SmartDbContext db)
            => db.Set<PrivateMessage>();

        // TODO: (mg) (core) This should be an extension method for DbSet<ForumPost>
        public static async Task<Dictionary<int, ForumPost>> GetForumPostsByIdsAsync(this SmartDbContext db, IEnumerable<int> forumPostIds)
        {
            var ids = forumPostIds
                .Where(x => x != 0)
                .Distinct()
                .ToArray();

            if (ids.Any())
            {
                return await db.ForumPosts()
                    .Include(x => x.ForumTopic)
                    .IncludeCustomer()
                    .AsNoTracking()
                    .Where(x => ids.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id);
            }
            else
            {
                return new Dictionary<int, ForumPost>();
            }
        }
    }
}
