using System.Data;
using FluentMigrator;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2023-12-22 10:00:00", "Core: conditional attributes")]
    internal class ConditionalAttributes : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        const string PvaName = "Product_ProductAttribute_Mapping";
        const string IxRuleSetIdName = "IX_Product_ProductAttribute_Mapping_RuleSetId";
        const string FkRuleSetIdName = "FK_Product_ProductAttribute_Mapping_RuleSet_RuleSetId";

        public override void Up()
        {
            if (!Schema.Table(PvaName).Column(nameof(ProductVariantAttribute.RuleSetId)).Exists())
            {
                Create.Column(nameof(ProductVariantAttribute.RuleSetId)).OnTable(PvaName)
                    .AsInt32()
                    .Nullable()
                    .Indexed(IxRuleSetIdName)
                    .ForeignKey(FkRuleSetIdName, "RuleSet", nameof(BaseEntity.Id))
                    .OnDelete(Rule.SetNull);
            }
        }

        public override void Down()
        {
            var pva = Schema.Table(PvaName);
            
            if (pva.Index(IxRuleSetIdName).Exists())
            {
                Delete.Index(IxRuleSetIdName).OnTable(PvaName);
            }

            if (pva.Constraint(FkRuleSetIdName).Exists())
            {
                Delete.ForeignKey(FkRuleSetIdName).OnTable(PvaName);
            }

            if (pva.Column(nameof(ProductVariantAttribute.RuleSetId)).Exists())
            {
                Delete.Column(nameof(ProductVariantAttribute.RuleSetId)).FromTable(PvaName);
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

            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.EditOptionsAndRules",
                "Edit {0} options and {1} rules",
                "{0} Optionen und {1} Bedingungen bearbeiten");

            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.EditRules",
                "Edit {0} rules",
                "{0} Bedingungen bearbeiten");

            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.OptionsAndRules", "Options and rules", "Optionen und Bedingungen");
            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.Rules", "Rules", "Bedingungen");

            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.OptionsAndRulesForProduct",
                "Options and rules for attribute \"{0}\". Product: {1}",
                "Optionen und Bedingungen für Attribut \"{0}\". Produkt: {1}");

            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.RulesForProduct",
                "Rules for attribute \"{0}\". Product: {1}",
                "Bedingungen für Attribut \"{0}\". Produkt: {1}");
        }
    }
}
