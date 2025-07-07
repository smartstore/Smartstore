using FluentMigrator;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2025-07-07 10:30:00", "Core: Duplicate activity log types")]
    internal class DuplicateActivityLogTypes : Migration, IDataSeeder<SmartDbContext>
    {
        private readonly SmartDbContext _db;
        private readonly ILogger _logger;

        public DuplicateActivityLogTypes(SmartDbContext db, ILogger logger)
        {
            _db = db;
            _logger = logger;
        }

        public override void Up()
        {
        }

        public override void Down()
        {
        }

        public DataSeederStage Stage => DataSeederStage.Late;
        public bool AbortOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            try
            {
                var migrator = new ActivityLogTypeMigrator(_db);
                var numDeleted = await migrator.DeleteDuplicateActivityLogTypeAsync(cancelToken);

                _logger.Debug($"Deleted {numDeleted} duplicate activity log types.");
            }
            catch (Exception ex)
            {
                // Do not break any other data seeding.
                _logger.Error(ex);
            }
        }
    }
}
