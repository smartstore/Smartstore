using Smartstore.Core.Logging;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<Log> Logs { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<ActivityLogType> ActivityLogTypes { get; set; }
    }
}
