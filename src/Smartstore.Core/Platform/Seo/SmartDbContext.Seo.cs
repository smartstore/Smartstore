using Smartstore.Core.Seo;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<UrlRecord> UrlRecords { get; set; }
    }
}