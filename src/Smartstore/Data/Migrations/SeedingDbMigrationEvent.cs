using Microsoft.EntityFrameworkCore;
using Smartstore.Events;

namespace Smartstore.Data.Migrations
{
    public sealed class SeedingDbMigrationEvent : IEventMessage
    {
        public long MigrationVersion { get; internal set; }
        public string MigrationDescription { get; internal set; }

        public DbContext DbContext { get; internal set; }
    }

    public sealed class SeededDbMigrationEvent : IEventMessage
    {
        public long MigrationVersion { get; internal set; }
        public string MigrationDescription { get; internal set; }

        public DbContext DbContext { get; internal set; }
    }
}
