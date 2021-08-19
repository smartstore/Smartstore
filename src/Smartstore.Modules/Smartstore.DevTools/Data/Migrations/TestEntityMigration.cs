using FluentMigrator;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.DevTools.Data.Migrations
{
    [MigrationVersion("2021-08-18 15:51:30", "Add DevTools test entity.")]
    public class TestEntityMigration : Migration
    {
        public override void Up()
        {
            this.ExecuteEmbeddedScripts(
                "Smartstore.DevTools.Data.SqlServer.Sql.AddTestEntity.sql",
                "Smartstore.DevTools.Data.MySql.Sql.AddTestEntity.sql");
        }

        public override void Down()
        {
            this.DeleteTables("DevToolsTestEntity");
        }
    }
}
