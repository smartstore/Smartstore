using System;
using Microsoft.EntityFrameworkCore;

namespace Smartstore.Core.Data.Migrations
{
    // TODO: (mg) (core) remove obsolete MigrationId and MigrationName from seeding events.
    public sealed class SeedingDbMigrationEvent
    {
        public string MigrationId { get; internal set; }
        public string MigrationName { get; internal set; }
        public long Version { get; internal set; }
        public string Description { get; internal set; }

        public DbContext DbContext { get; internal set; }
    }

    public sealed class SeededDbMigrationEvent
    {
        public string MigrationId { get; internal set; }
        public string MigrationName { get; internal set; }
        public long Version { get; internal set; }
        public string Description { get; internal set; }

        public DbContext DbContext { get; internal set; }
    }
}
