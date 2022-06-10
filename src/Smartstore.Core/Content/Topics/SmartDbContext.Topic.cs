using Smartstore.Core.Content.Topics;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<Topic> Topics { get; set; }
    }
}