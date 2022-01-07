using Smartstore.Core.Stores;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<Store> Stores { get; set; }
        public DbSet<StoreMapping> StoreMappings { get; set; }
    }
}
