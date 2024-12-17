using FluentMigrator;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2024-12-17 09:30:00", "Core: Renew attribute combination hash codes")]
    internal class RenewAttributeHashCodes : Migration, IDataSeeder<SmartDbContext>
    {
        private readonly SmartDbContext _db;
        private readonly ILogger _logger;

        public RenewAttributeHashCodes(SmartDbContext db, ILogger logger)
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
                var migrator = new AttributesMigrator(_db, _logger);
                var numUpdated = await migrator.CreateAttributeCombinationHashCodesAsync(true, cancelToken);

                _logger.Debug($"Renewed hash codes for {numUpdated} attribute combinations.");
            }
            catch (Exception ex)
            {
                // Do not break any other data seeding. Hash code creation can be restarted on maintenance page.
                _logger.Error(ex);
            }
        }
    }
}
