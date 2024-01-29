using System.Data;
using FluentMigrator;
using FluentMigrator.SqlServer;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2023-12-22 10:00:00", "Core: conditional attributes")]
    internal class ConditionalAttributes : Migration
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
                    .ForeignKey(FkAttributeId, "Product_ProductAttribute_Mapping", nameof(BaseEntity.Id))
                    .OnDelete(Rule.Cascade);
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
    }
}
