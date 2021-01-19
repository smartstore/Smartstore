using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Content.Blogs;
using Smartstore.Core.Content.Forums;
using Smartstore.Core.Content.Forums.Domain;
using Smartstore.Core.Content.News;
using Smartstore.Core.Content.Topics;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<Topic> Topics { get; set; }
        public DbSet<BlogPost> BlogPosts { get; set; }
        public DbSet<BlogComment> BlogComments { get; set; }
        public DbSet<NewsItem> NewsItems { get; set; }
        public DbSet<NewsComment> NewsComments { get; set; }
        public DbSet<ForumPostVote> ForumPostVotes { get; set; }
        public DbSet<Forum> Forums { get; set; }
        public DbSet<ForumGroup> ForumGroups { get; set; }
        public DbSet<ForumPost> ForumPosts { get; set; }
        public DbSet<PrivateMessage> PrivateMessages { get; set; }
        public DbSet<ForumSubscription> ForumSubscriptions { get; set; }
    }
}