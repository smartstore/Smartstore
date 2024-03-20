using FluentMigrator;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2024-03-19 13:00:00", "Core: essential attributes")]
    internal class EssentialAttributes : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        const string AttributesTableName = nameof(SpecificationAttribute);
        const string EssentialColumn = nameof(SpecificationAttribute.Essential);
        const string EssentialIndex = "IX_EssentialAttribute";

        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public override void Up()
        {
            if (!Schema.Table(AttributesTableName).Column(EssentialColumn).Exists())
            {
                Create.Column(EssentialColumn).OnTable(AttributesTableName).AsBoolean().NotNullable().WithDefaultValue(false);

                Create.Index(EssentialIndex)
                    .OnTable(AttributesTableName)
                    .OnColumn(EssentialColumn)
                    .Ascending()
                    .WithOptions()
                    .NonClustered();
            }
        }

        public override void Down()
        {
            var table = Schema.Table(AttributesTableName);

            if (table.Index(EssentialIndex).Exists())
            {
                Delete.Index(EssentialIndex).OnTable(AttributesTableName);
            }

            if (table.Column(EssentialColumn).Exists())
            {
                Delete.Column(EssentialColumn).FromTable(AttributesTableName);
            }
        }

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Admin.Catalog.Attributes.SpecificationAttributes.Fields.Essential",
                "Essential feature",
                "Wesentliches Merkmal",
                "Specifies whether the attribute is an essential feature. Essential features are displayed in the checkout (e.g. on the order confirmation page).",
                "Legt fest, ob es sich um eine wesentliches Merkmal handelt. Wesentliche Merkmale werden im Checkout angezeigt (z.B. auf der Bestellbestätigungsseite).");

            builder.AddOrUpdate("Admin.Catalog.Attributes.SpecificationAttributes.List.SearchEssential",
                "Essential feature",
                "Wesentliches Merkmal");
        }
    }
}
