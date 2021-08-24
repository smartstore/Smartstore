using FluentMigrator;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data;

namespace Smartstore.DevTools.Data.Migrations
{
    [MigrationVersion("2021-08-19 14:17:55", "Update DevTools test entity.")]
    public class UpdateTestEntityMigration : Migration
    {
        public override void Up()
        {
            var dbSystemName = DataSettings.Instance.DbFactory.DbSystem.ToString();

            this.CreateColumn(dbSystemName, "DevToolsTestEntity", "IsActive")?.AsBoolean()?.NotNullable();
            this.CreateColumn(dbSystemName, "DevToolsTestEntity", "Notes")?.AsString(400)?.Nullable();
        }

        public override void Down()
        {
            var dbSystemName = DataSettings.Instance.DbFactory.DbSystem.ToString();

            this.DeleteColumns(dbSystemName, "DevToolsTestEntity", "Notes", "IsActive");
        }
    }
}
