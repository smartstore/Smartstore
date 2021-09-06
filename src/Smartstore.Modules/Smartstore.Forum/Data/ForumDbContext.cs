using Microsoft.EntityFrameworkCore;
using Smartstore.Data;

namespace Smartstore.Forum.Data
{
    //public class ForumDbContext : HookingDbContext
    //{
    //    public ForumDbContext(DbContextOptions<ForumDbContext> options)
    //        : base(options)
    //    {
    //    }

    //    protected ForumDbContext(DbContextOptions options)
    //        : base(options)
    //    {
    //    }

    //    public DbSet<Forum> Forums { get; set; }
    //    public DbSet<ForumGroup> ForumGroups { get; set; }
    //    public DbSet<ForumPost> ForumPosts { get; set; }
    //    public DbSet<ForumPostVote> ForumPostVotes { get; set; }
    //    public DbSet<ForumSubscription> ForumSubscriptions { get; set; }
    //    public DbSet<ForumTopic> ForumTopics { get; set; }
    //    public DbSet<PrivateMessage> PrivateMessages { get; set; }

    //    protected override void OnModelCreating(ModelBuilder modelBuilder)
    //    {
    //        CreateModel(modelBuilder, typeof(ForumDbContext).Assembly);

    //        base.OnModelCreating(modelBuilder);
    //    }
    //}
}
