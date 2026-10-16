using System.Data;
using FluentMigrator;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations;

[MigrationVersion("2026-10-16 13:00:00", "Core: Checkout attribute rules")]
internal class CheckoutAttributeRules : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
{
    const string TableName = "RuleSet_CheckoutAttribute_Mapping";
    const string CheckoutAttributeColumn = "CheckoutAttribute_Id";
    const string RuleSetColumn = "RuleSetEntity_Id";
    const string CheckoutAttributeTable = nameof(CheckoutAttribute);
    const string RuleSetTable = "RuleSet";
    const string PrimaryKeyName = "PK_RuleSet_CheckoutAttribute_Mapping";
    const string CheckoutAttributeIndex = "IX_RuleSet_CheckoutAttribute_Mapping_CheckoutAttribute_Id";
    const string CheckoutAttributeForeignKey = "FK_RuleSet_CheckoutAttribute_CheckoutAttribute_Id";
    const string RuleSetForeignKey = "FK_RuleSet_CheckoutAttribute_RuleSetEntity_Id";

    public override void Up()
    {
        if (!Schema.Table(TableName).Exists())
        {
            Create.Table(TableName)
                .WithColumn(CheckoutAttributeColumn).AsInt32().NotNullable()
                .WithColumn(RuleSetColumn).AsInt32().NotNullable();
        }

        var mapping = Schema.Table(TableName);

        if (!mapping.Constraint(PrimaryKeyName).Exists())
        {
            Create.PrimaryKey(PrimaryKeyName)
                .OnTable(TableName)
                .Columns(CheckoutAttributeColumn, RuleSetColumn);
        }

        if (!mapping.Index(CheckoutAttributeIndex).Exists())
        {
            Create.Index(CheckoutAttributeIndex)
                .OnTable(TableName)
                .OnColumn(CheckoutAttributeColumn).Ascending();
        }

        if (!mapping.Constraint(CheckoutAttributeForeignKey).Exists())
        {
            Create.ForeignKey(CheckoutAttributeForeignKey)
                .FromTable(TableName).ForeignColumn(CheckoutAttributeColumn)
                .ToTable(CheckoutAttributeTable).PrimaryColumn(nameof(BaseEntity.Id))
                .OnDelete(Rule.Cascade);
        }

        if (!mapping.Constraint(RuleSetForeignKey).Exists())
        {
            Create.ForeignKey(RuleSetForeignKey)
                .FromTable(TableName).ForeignColumn(RuleSetColumn)
                .ToTable(RuleSetTable).PrimaryColumn(nameof(BaseEntity.Id))
                .OnDelete(Rule.Cascade);
        }
    }

    public override void Down()
    {
        if (Schema.Table(TableName).Exists())
        {
            Delete.Table(TableName);
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
        builder.AddOrUpdate("Admin.Catalog.Attributes.CheckoutAttributes.Fields.Requirements",
            "Requirements",
            "Voraussetzungen",
            "Select cart rule sets that determine when the checkout attribute is available. The attribute is available when at least one rule set matches.",
            "Wählen Sie Warenkorb-Regelsätze aus, die festlegen, wann das Checkout-Attribut verfügbar ist. Das Attribut ist verfügbar, sobald einer der ausgewählten Regelsätze zutrifft.");
    }
}
