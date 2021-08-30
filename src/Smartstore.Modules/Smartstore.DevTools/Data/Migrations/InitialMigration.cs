using FluentMigrator;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data;

namespace Smartstore.DevTools.Data.Migrations
{
    [MigrationVersion("2021-08-18 15:51:30", "Add DevTools test entity")]
    public class InitialMigration : Migration
    {
        private const string TABLE_NAME = "DevToolsTestEntity";

        public override void Up()
        {
            var dbSystem = DataSettings.Instance.DbFactory.DbSystem.ToString();

            if (!IfDatabase(dbSystem).Schema.Table(TABLE_NAME).Exists())
            {
                Create.Table(TABLE_NAME)
                    .WithColumn("Id").AsInt32().PrimaryKey().Identity().NotNullable()
                    .WithColumn("Name").AsString(400).NotNullable()
                    .WithColumn("Description").AsString(int.MaxValue).Nullable()
                    .WithColumn("PageSize").AsInt32().Nullable()
                    .WithColumn("LimitedToStores").AsBoolean().NotNullable()
                    .WithColumn("SubjectToAcl").AsBoolean().NotNullable()
                    .WithColumn("Published").AsBoolean().NotNullable()
                    .WithColumn("Deleted").AsBoolean().NotNullable()
                    .WithColumn("DisplayOrder").AsInt32().NotNullable()
                    .WithColumn("CreatedOnUtc").AsDateTime2().NotNullable()
                    .WithColumn("UpdatedOnUtc").AsDateTime2().NotNullable();
            }

            if (!IfDatabase(dbSystem).Schema.Table(TABLE_NAME).Index("IX_Deleted").Exists())
            {
                Create.Index("IX_Deleted").OnTable(TABLE_NAME).OnColumn("Deleted").Ascending().WithOptions().NonClustered();
            }

            if (!IfDatabase(dbSystem).Schema.Table(TABLE_NAME).Index("IX_DisplayOrder").Exists())
            {
                Create.Index("IX_DisplayOrder").OnTable(TABLE_NAME).OnColumn("DisplayOrder").Ascending().WithOptions().NonClustered();
            }

            if (!IfDatabase(dbSystem).Schema.Table(TABLE_NAME).Index("IX_LimitedToStores").Exists())
            {
                Create.Index("IX_LimitedToStores").OnTable(TABLE_NAME).OnColumn("LimitedToStores").Ascending().WithOptions().NonClustered();
            }

            if (!IfDatabase(dbSystem).Schema.Table(TABLE_NAME).Index("IX_SubjectToAcl").Exists())
            {
                Create.Index("IX_SubjectToAcl").OnTable(TABLE_NAME).OnColumn("SubjectToAcl").Ascending().WithOptions().NonClustered();
            }
        }

        public override void Down()
        {
            var dbSystem = DataSettings.Instance.DbFactory.DbSystem.ToString();

            if (IfDatabase(dbSystem).Schema.Table(TABLE_NAME).Exists())
            {
                Delete.Table(TABLE_NAME);
            }
        }
    }
}
