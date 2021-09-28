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
    }
}
