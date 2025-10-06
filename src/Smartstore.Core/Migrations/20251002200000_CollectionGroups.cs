using FluentMigrator;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2025-10-02 20:00:00", "Core: Collection groups")]
    internal class CollectionGroups : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        public override void Up()
        {
            const string tableName = "CollectionGroup";

            if (!Schema.Table(tableName).Exists())
            {
                Create.Table(tableName)
                    .WithIdColumn()
                    .WithColumn(nameof(CollectionGroup.EntityName)).AsString(100).NotNullable()
                    .WithColumn(nameof(CollectionGroup.EntityId)).AsInt32().NotNullable()
                    .WithColumn(nameof(CollectionGroup.Name)).AsString(400).NotNullable()
                    .WithColumn(nameof(CollectionGroup.Published)).AsBoolean().NotNullable()
                    .WithColumn(nameof(CollectionGroup.DisplayOrder)).AsInt32().NotNullable()
                        .Indexed();

                Create.Index()
                    .OnTable(tableName)
                    .OnColumn(nameof(CollectionGroup.EntityId)).Ascending()
                    .OnColumn(nameof(CollectionGroup.EntityName)).Ascending()
                    .WithOptions()
                    .NonClustered();
            }
        }

        public override void Down()
        {
        }

        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Permissions.DisplayName.CollectionGroup", "Collection Groups", "Gruppierungen");
            builder.AddOrUpdate("Admin.Configuration.CollectionGroups", "Collection Groups", "Gruppierungen");

            builder.AddOrUpdate("Admin.Common.EntityId", "Object ID", "Objekt-ID");

            builder.AddOrUpdate("Admin.Configuration.CollectionGroup.Name",
                "Name",
                "Name",
                "Specifies the name of the collection group.",
                "Legt den Namen der Gruppierung fest.");
        }
    }
}
