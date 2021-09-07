using Microsoft.EntityFrameworkCore;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Data.Migrations;

namespace Smartstore.Blog.Data
{
    //[CheckTables("BlogPost", "BlogComment")]
    //public partial class BlogDbContext : HookingDbContext
    //{
    //    public BlogDbContext(DbContextOptions<BlogDbContext> options)
    //        : base(options)
    //    {
    //    }

    //    protected BlogDbContext(DbContextOptions options)
    //        : base(options)
    //    {
    //    }

    //    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //    {
    //        // ???
    //    }

    //    protected override void OnModelCreating(ModelBuilder modelBuilder)
    //    {
    //        CreateModel(
    //            modelBuilder,
    //            // Contains all entities
    //            typeof(BlogDbContext).Assembly);

    //        base.OnModelCreating(modelBuilder);
    //    }

    //    public DbSet<BlogPost> BlogPosts { get; set; }

    //    public DbSet<BlogComment> BlogComments { get; set; }
    //}
}
