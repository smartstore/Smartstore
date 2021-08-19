using Smartstore.Core.Data.Migrations;

namespace Smartstore.DevTools.Data.Migrations
{
    [MigrationVersion("2021-08-19 14:17:55", "Update DevTools test entity.")]
    public class UpdateTestEntityMigration : DataMigrationBase
    {
        public override void Up()
        {
            Create.Column("IsActive").OnTable("DevToolsTestEntity").AsBoolean().NotNullable();
            Create.Column("Notes").OnTable("DevToolsTestEntity").AsString(400).Nullable();
        }

        public override void Down()
        {
            Delete.Column("Notes").FromTable("DevToolsTestEntity");
            Delete.Column("IsActive").FromTable("DevToolsTestEntity");
        }
    }
}
