using FluentMigrator;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations;

[MigrationVersion("2026-10-06 09:30:00", "Core: Allow withdrawal before shipping")]
internal class AllowWithdrawalBeforeShipping : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
{
    public override void Up()
    {
        if (!Schema.Table(nameof(Product)).Column(nameof(Product.AllowWithdrawalBeforeShipping)).Exists())
        {
            Create.Column(nameof(Product.AllowWithdrawalBeforeShipping)).OnTable(nameof(Product))
                .AsBoolean()
                .Nullable();
        }

        if (!Schema.Table(nameof(Category)).Column(nameof(Category.AllowWithdrawalBeforeShipping)).Exists())
        {
            Create.Column(nameof(Category.AllowWithdrawalBeforeShipping)).OnTable(nameof(Category))
                .AsBoolean()
                .Nullable();
        }
    }

    public override void Down()
    {
        if (Schema.Table(nameof(Product)).Column(nameof(Product.AllowWithdrawalBeforeShipping)).Exists())
        {
            Delete.Column(nameof(Product.AllowWithdrawalBeforeShipping)).FromTable(nameof(Product));
        }

        if (Schema.Table(nameof(Category)).Column(nameof(Category.AllowWithdrawalBeforeShipping)).Exists())
        {
            Delete.Column(nameof(Category.AllowWithdrawalBeforeShipping)).FromTable(nameof(Category));
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
        builder.AddOrUpdate("Admin.Catalog.Products.Fields.AllowWithdrawalBeforeShipping",
            "Allow withdrawal before shipping",
            "Widerruf vor Versand erlauben",
            "Specifies whether this product can still be withdrawn before it has been shipped if it is otherwise excluded from withdrawal (withdrawal period 0)."
            + " This setting applies only to items eligible for shipping.",
            "Legt fest, ob dieser Artikel noch widerrufen werden kann, bevor er versendet wurde, obwohl er ansonsten vom Widerruf ausgeschlossen ist (Widerrufsfrist 0)."
            + " Diese Einstellung gilt ausschließlich für versandfähige Artikel.");
        
        builder.AddOrUpdate("Admin.Catalog.Categories.Fields.AllowWithdrawalBeforeShipping",
            "Allow withdrawal before shipping",
            "Widerruf vor Versand erlauben",
            "Specifies whether products in this category can still be withdrawn before they have been shipped if they are otherwise excluded from withdrawal (withdrawal period 0)."
            + " A product setting takes precedence. For multiple categories, \"No\" takes precedence over \"Yes\"."
            + " This setting applies only to items eligible for shipping.",
            "Legt fest, ob Artikel dieser Warengruppe noch widerrufen werden können, bevor sie versendet wurden, obwohl sie ansonsten vom Widerruf ausgeschlossen sind (Widerrufsfrist 0)."
            + " Eine Einstellung am Artikel hat Vorrang. Bei mehreren Warengruppen gilt \"Nein\" vor \"Ja\"."
            + " Diese Einstellung gilt ausschließlich für versandfähige Artikel.");
    }
}
