using FluentMigrator;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations;

[MigrationVersion("2026-04-29 14:00:00", "Core: Warranty")]
internal class Warranty : Migration
{
    const string ProductTable = nameof(Product);
    const string DurabilityGuaranteeDurationYearsColumn = nameof(Product.DurabilityGuaranteeDurationYears);

    public override void Up()
    {
        if (!Schema.Table(ProductTable).Column(DurabilityGuaranteeDurationYearsColumn).Exists())
        {
            Create.Column(DurabilityGuaranteeDurationYearsColumn).OnTable(ProductTable)
                .AsInt32()
                .Nullable();
                // TODO: (mh) (eu) Why was this field indexed? Did I overlook something?
                //.Indexed();
        }
    }

    public override void Down()
    {
        if (Schema.Table(ProductTable).Column(DurabilityGuaranteeDurationYearsColumn).Exists())
        {
            Delete.Column(DurabilityGuaranteeDurationYearsColumn).FromTable(ProductTable);
        }
    }

    public DataSeederStage Stage => DataSeederStage.Early;
    public bool AbortOnFailure => false;
}
