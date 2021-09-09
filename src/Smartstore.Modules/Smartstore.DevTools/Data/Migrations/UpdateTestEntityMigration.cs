using FluentMigrator;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data;

namespace Smartstore.DevTools.Data.Migrations
{
    [MigrationVersion("2021-08-19 14:17:55", "DevTools: update test entity")]
    public class UpdateTestEntityMigration : Migration
    {
        private const string TABLE_NAME = "DevToolsTestEntity";

        public override void Up()
        {
            var dbSystem = DataSettings.Instance.DbFactory.DbSystem.ToString();

            if (!IfDatabase(dbSystem).Schema.Table(TABLE_NAME).Column("IsActive").Exists())
            {
                Create.Column("IsActive").OnTable(TABLE_NAME).AsBoolean().NotNullable();
            }

            if (!IfDatabase(dbSystem).Schema.Table(TABLE_NAME).Column("Notes").Exists())
            {
                Create.Column("Notes").OnTable(TABLE_NAME).AsString(400).Nullable();
            }
        }

        public override void Down()
        {
            var dbSystem = DataSettings.Instance.DbFactory.DbSystem.ToString();

            if (IfDatabase(dbSystem).Schema.Table(TABLE_NAME).Column("IsActive").Exists())
            {
                Delete.Column("IsActive");
            }

            if (IfDatabase(dbSystem).Schema.Table(TABLE_NAME).Column("Notes").Exists())
            {
                Delete.Column("Notes");
            }
        }
    }
}
