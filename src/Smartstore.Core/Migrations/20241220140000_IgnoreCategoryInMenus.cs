using FluentMigrator;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2024-12-20 14:00:00", "Core: Ignore category in menus")]
    internal class IgnoreCategoryInMenus : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        const string TableName = nameof(Category);
        const string IgnoreInMenus = nameof(Category.IgnoreInMenus);
        const string IndexName = "IX_Category_IgnoreInMenus";

        public override void Up()
        {
            if (!Schema.Table(TableName).Column(IgnoreInMenus).Exists())
            {
                Create.Column(IgnoreInMenus).OnTable(TableName).AsBoolean().NotNullable().WithDefaultValue(false);

                Create.Index(IndexName)
                    .OnTable(TableName)
                    .OnColumn(IgnoreInMenus)
                    .Ascending()
                    .WithOptions()
                    .NonClustered();
            }
        }

        public override void Down()
        {
            var table = Schema.Table(TableName);

            if (table.Index(IndexName).Exists())
            {
                Delete.Index(IndexName).OnTable(TableName);
            }

            if (table.Column(IgnoreInMenus).Exists())
            {
                Delete.Column(IgnoreInMenus).FromTable(TableName);
            }
        }

        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Admin.Catalog.Categories.Fields.IgnoreInMenus",
                "Ignore in menus",
                "In Menüs ignorieren",
                "Specifies whether the category is ignored in menus.",
                "Legt fest, ob die Warengruppe in Menüs ignoriert wird.");
        }
    }
}
