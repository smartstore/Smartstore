using FluentMigrator;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.DevTools.Data.Migrations
{
    [MigrationVersion("2021-08-19 14:17:55", "DevTools: update test entity")]
    internal class UpdateTestEntity : Migration
    {
        private const string TABLE_NAME = "DevToolsTestEntity";

        public override void Up()
        {
            if (!Schema.Table(TABLE_NAME).Column("IsActive").Exists())
            {
                Create.Column("IsActive").OnTable(TABLE_NAME).AsBoolean().NotNullable();
            }

            if (!Schema.Table(TABLE_NAME).Column("Notes").Exists())
            {
                Create.Column("Notes").OnTable(TABLE_NAME).AsString(400).Nullable();
            }
        }

        public override void Down()
        {
            if (Schema.Table(TABLE_NAME).Column("IsActive").Exists())
            {
                Delete.Column("IsActive");
            }

            if (Schema.Table(TABLE_NAME).Column("Notes").Exists())
            {
                Delete.Column("Notes");
            }
        }
    }
}
