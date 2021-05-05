using Microsoft.EntityFrameworkCore;
using Smartstore.Core.DataExchange;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<SyncMapping> SyncMappings { get; set; }
    }
}
