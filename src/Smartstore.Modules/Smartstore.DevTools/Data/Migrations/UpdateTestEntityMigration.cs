using FluentMigrator;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.DevTools.Data.Migrations
{
    [MigrationVersion("2021-08-19 14:17:55", "Update DevTools test entity.")]
    public class UpdateTestEntityMigration : Migration
    {
        public override void Up()
        {
            this.CreateColumn("DevToolsTestEntity", "IsActive")?.AsBoolean()?.NotNullable();
            this.CreateColumn("DevToolsTestEntity", "Notes")?.AsString(400)?.Nullable();
        }

        public override void Down()
        {
            this.DeleteColumns("DevToolsTestEntity", "Notes", "IsActive");
        }
    }
}
