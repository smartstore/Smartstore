using Smartstore.Core.Data;

namespace Smartstore.News
{
    public static class SmartDbContextExtensions
    {
        public static DbSet<NewsItem> NewsItems(this SmartDbContext db)
            => db.Set<NewsItem>();

        public static DbSet<NewsComment> NewsComments(this SmartDbContext db)
            => db.Set<NewsComment>();
    }
}
