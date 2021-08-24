using Smartstore.Core.Data.Migrations;

namespace Smartstore.DevTools.Data.Migrations
{
    [MigrationVersion("2021-08-19 14:17:55", "Update DevTools test entity.")]
    public class UpdateTestEntityMigration : DatabaseMigrationBase
    {
        public override void Up()
        {
            CreateColumn("DevToolsTestEntity", "IsActive")?.AsBoolean()?.NotNullable();
            CreateColumn("DevToolsTestEntity", "Notes")?.AsString(400)?.Nullable();
        }

        public override void Down()
        {
            DeleteColumns("DevToolsTestEntity", "Notes", "IsActive");
        }
    }
}
