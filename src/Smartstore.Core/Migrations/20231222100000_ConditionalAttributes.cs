using FluentMigrator;
using FluentMigrator.SqlServer;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Rules;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2023-12-22 10:00:00", "Core: conditional attributes")]
    internal class ConditionalAttributes : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        const string RuleSetTable = "RuleSet";
        const string AttributeIdColumn = nameof(RuleSetEntity.ProductVariantAttributeId);
        const string IxAttributeId = "IX_RuleSet_ProductVariantAttributeId";
        // INFO: EF creates a key "FK_RuleSet_Product_ProductAttribute_Mapping_ProductVariantAttributeId" which is too long for MySQL.
        // Produces MySqlException: Identifier name 'xyz' is too long.
        const string FkAttributeId = "FK_RuleSet_ProductVariantAttributeId";

        public override void Up()
        {
            var ruleSet = Schema.Table(RuleSetTable);

            if (!ruleSet.Column(AttributeIdColumn).Exists())
            {
                Create.Column(AttributeIdColumn).OnTable(RuleSetTable)
                    .AsInt32()
                    .Nullable()
                    .ForeignKey(FkAttributeId, "Product_ProductAttribute_Mapping", nameof(BaseEntity.Id));
            }

            if (!ruleSet.Index(IxAttributeId).Exists())
            {
                // We want exactly what EF specifies during an installation.
                // https://fluentmigrator.github.io/articles/extensions/sql-server-extensions.html#create-a-unique-constraint-on-nullable-columns-using-null-value-filter
                Create.Index(IxAttributeId)
                    .OnTable(RuleSetTable)
                    .OnColumn(AttributeIdColumn).Ascending()
                    .WithOptions().Unique()
                    .WithOptions().Filter($"([{AttributeIdColumn}] IS NOT NULL)");
            }
        }

        public override void Down()
        {
            var ruleSet = Schema.Table(RuleSetTable);

            if (ruleSet.Index(IxAttributeId).Exists())
            {
                Delete.Index(IxAttributeId).OnTable(RuleSetTable);
            }

            if (ruleSet.Constraint(FkAttributeId).Exists())
            {
                Delete.ForeignKey(FkAttributeId).OnTable(RuleSetTable);
            }

            if (ruleSet.Column(AttributeIdColumn).Exists())
            {
                Delete.Column(AttributeIdColumn).FromTable(RuleSetTable);
            }
        }

        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => true;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.Delete(
                "Admin.Catalog.Products.ProductVariantAttributes.Attributes.Values.ViewLink",
                "Admin.Catalog.Products.ProductVariantAttributes.Attributes.Values.EditAttributeDetails");


            builder.AddOrUpdate("Admin.Rules.ProductAttribute.OneCondition",
                "<span>Only show the attribute if at least</span> {0} <span>of the following conditions is true.</span>",
                "<span>Das Attribut nur anzeigen, wenn mindestens</span> {0} <span>der folgenden Bedingungen zutrifft.</span>");

            builder.AddOrUpdate("Admin.Rules.ProductAttribute.AllConditions",
                "<span>Only show the attribute if</span> {0} <span>of the following conditions are true.</span>",
                "<span>Das Attribut nur anzeigen, wenn</span> {0} <span>der folgenden Bedingungen erfüllt sind.</span>");


            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.OptionsAndRulesForProduct",
                "Options and rules for attribute \"{0}\". Product: {1}",
                "Optionen und Bedingungen für Attribut \"{0}\". Produkt: {1}");

            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.RulesForProduct",
                "Rules for attribute \"{0}\". Product: {1}",
                "Bedingungen für Attribut \"{0}\". Produkt: {1}");

            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.EditOptionsAndRules",
                "Edit {0} options and {1} rules",
                "{0} Optionen und {1} Bedingungen bearbeiten");

            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.EditRules",
                "Edit {0} rules",
                "{0} Bedingungen bearbeiten");

            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.OptionsAndRules", "Options and rules", "Optionen und Bedingungen");
            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.Rules", "Rules", "Bedingungen");

            builder.AddOrUpdate("Admin.Rules.AddRuleWarning", "Please add a rule first.", "Bitte zuerst eine Bedingung hinzufügen.");

            builder.AddOrUpdate("Admin.Rules.FilterDescriptor.AttributePriceAdjustment", "Price adjustment", "Mehr-/Minderpreis");
            builder.AddOrUpdate("Admin.Rules.FilterDescriptor.ProductWeight", "Weight", "Gewicht");
        }
    }
}
