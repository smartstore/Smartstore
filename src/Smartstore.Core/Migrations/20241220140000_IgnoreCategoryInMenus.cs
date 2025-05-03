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

        public override void Up()
        {
            if (!Schema.Table(TableName).Column(IgnoreInMenus).Exists())
            {
                Create.Column(IgnoreInMenus).OnTable(TableName).AsBoolean().NotNullable().WithDefaultValue(false);
            }
        }

        public override void Down()
        {
            if (Schema.Table(TableName).Column(IgnoreInMenus).Exists())
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
     "نادیده گرفتن در منوها", // Persian label
     "Specifies whether the category is ignored in menus. In comparison to unpublished categories, products remain visible if this option and \"Include products from subcategories\" are activated.",
     "Legt fest, ob die Warengruppe in Menüs ignoriert wird. Im Gegensatz zu unveröffentlichten Warengruppen bleiben Produkte sichtbar, wenn diese Option und \"Produkte von Unterkategorien einschließen\" aktiviert sind.",
     "مشخص می‌کند که آیا دسته‌بندی در منوها نادیده گرفته شود. در مقایسه با دسته‌بندی‌های منتشرنشده، اگر این گزینه و «شامل کردن محصولات از زیرمجموعه‌ها» فعال باشند، محصولات همچنان قابل مشاهده باقی می‌مانند." // Persian description
 );
        }
    }
}
