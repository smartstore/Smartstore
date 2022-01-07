using Smartstore.Core.Configuration;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<Setting> Settings { get; set; }
    }
}
