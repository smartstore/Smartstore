using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums
{
    internal static class SmartDbContextExtensions
    {
        internal static DbSet<ForumGroup> ForumGroups(this SmartDbContext db)
            => db.Set<ForumGroup>();

        internal static DbSet<ForumTopic> ForumTopics(this SmartDbContext db)
            => db.Set<ForumTopic>();

        internal static DbSet<Forum> Forums(this SmartDbContext db)
            => db.Set<Forum>();

        internal static DbSet<ForumPost> ForumPosts(this SmartDbContext db)
            => db.Set<ForumPost>();

        internal static DbSet<ForumPostVote> ForumPostVotes(this SmartDbContext db)
            => db.Set<ForumPostVote>();

        internal static DbSet<ForumSubscription> ForumSubscriptions(this SmartDbContext db)
            => db.Set<ForumSubscription>();

        internal static DbSet<PrivateMessage> PrivateMessages(this SmartDbContext db)
            => db.Set<PrivateMessage>();
    }
}
