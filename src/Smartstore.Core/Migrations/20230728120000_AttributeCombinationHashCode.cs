using FluentMigrator;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2023-07-28 12:00:00", "Core: attribute combination hash code")]
    internal class AttributeCombinationHashCode : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        const string AttributeCombinationTable = nameof(ProductVariantAttributeCombination);
        const string HashCodeColumn = nameof(ProductVariantAttributeCombination.HashCode);
        const string HashCodeIndex = "IX_HashCode";

        private readonly SmartDbContext _db;
        private readonly ILogger _logger;

        public AttributeCombinationHashCode(SmartDbContext db, ILogger logger)
        {
            _db = db;
            _logger = logger;
        }

        public override void Up()
        {
            if (!Schema.Table(AttributeCombinationTable).Column(HashCodeColumn).Exists())
            {
                Create.Column(HashCodeColumn).OnTable(AttributeCombinationTable)
                    .AsInt32()
                    .NotNullable()
                    .WithDefaultValue(0)
                    .Indexed(HashCodeIndex);
            }
        }

        public override void Down()
        {
            var attributeCombinations = Schema.Table(AttributeCombinationTable);

            if (attributeCombinations.Index(HashCodeIndex).Exists())
            {
                Delete.Index(HashCodeIndex).OnTable(AttributeCombinationTable);
            }

            if (attributeCombinations.Column(HashCodeColumn).Exists())
            {
                Delete.Column(HashCodeColumn).FromTable(AttributeCombinationTable);
            }
        }

        public DataSeederStage Stage => DataSeederStage.Late;
        public bool AbortOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);

            try
            {
                var migrator = new AttributesMigrator(_db, _logger);
                var numUpdated = await migrator.CreateAttributeCombinationHashCodesAsync(cancelToken);

                _logger.Debug($"Created hash codes for {numUpdated} attribute combinations.");
            }
            catch (Exception ex)
            {
                // Do not break any other data seeding. Hash code creation can be restarted on maintenance page.
                _logger.Error(ex);
            }
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Admin.System.Warnings.AttributeCombinationHashCodes.OK",
                "All hash codes of attribute combinations have been set.",
                "Alle Hash-Codes von Attribut-Kombinationen wurden festgelegt.");

            builder.AddOrUpdate("Admin.System.Warnings.AttributeCombinationHashCodes.Missing",
                "The hash code is missing for {0} attribute combination(s). <a href=\"{1}\">Fix now.</a>",
                "Bei {0} Attribut-Kombination(en) fehlt der Hash-Code. <a href=\"{1}\">Jetzt beheben.</a>");
        }
    }
}
