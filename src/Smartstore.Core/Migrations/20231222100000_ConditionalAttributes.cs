using System.Data;
using FluentMigrator;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Rules;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2023-12-22 10:00:00", "Core: conditional attributes")]
    internal class ConditionalAttributes : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        const string RuleSetName = "RuleSet";
        const string IxPvaIdName = "IX_RuleSet_ProductVariantAttributeId";
        const string FkPvaIdName = "FK_RuleSet_Product_ProductAttribute_Mapping_ProductVariantAttributeId";

        public override void Up()
        {
            if (!Schema.Table(RuleSetName).Column(nameof(RuleSetEntity.ProductVariantAttributeId)).Exists())
            {
                // INFO: actually Unique(IxPvaIdName) must be used instead of Indexed(IxPvaIdName) but that requires a filter
                // "([ProductVariantAttributeId] IS NOT NULL)" which cannot be created using fluent migrator.
                Create.Column(nameof(RuleSetEntity.ProductVariantAttributeId)).OnTable(RuleSetName)
                    .AsInt32()
                    .Nullable()
                    .Indexed(IxPvaIdName)
                    .ForeignKey(FkPvaIdName, "Product_ProductAttribute_Mapping", nameof(BaseEntity.Id))
                    .OnDelete(Rule.SetNull);
            }
        }

        public override void Down()
        {
            var ruleSet = Schema.Table(RuleSetName);

            if (ruleSet.Index(IxPvaIdName).Exists())
            {
                Delete.Index(IxPvaIdName).OnTable(RuleSetName);
            }

            if (ruleSet.Constraint(FkPvaIdName).Exists())
            {
                Delete.ForeignKey(FkPvaIdName).OnTable(RuleSetName);
            }

            if (ruleSet.Column(nameof(RuleSetEntity.ProductVariantAttributeId)).Exists())
            {
                Delete.Column(nameof(RuleSetEntity.ProductVariantAttributeId)).FromTable(RuleSetName);
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
                "<span>Display the attribute if at least</span> {0} <span>of the following conditions is true.</span>",
                "<span>Das Attribut anzeigen, wenn mindestens</span> {0} <span>der folgenden Bedingungen zutrifft.</span>");

            builder.AddOrUpdate("Admin.Rules.ProductAttribute.AllConditions",
                "<span>Display the attribute if</span> {0} <span>of the following conditions are true.</span>",
                "<span>Das Attribut anzeigen, wenn</span> {0} <span>der folgenden Bedingungen erfüllt sind.</span>");


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
        }
    }
}
