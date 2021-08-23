using FluentMigrator;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.DevTools.Data.Migrations
{
    [MigrationVersion("2021-08-18 15:51:30", "Add DevTools test entity.")]
    public class InitialMigration : Migration
    {
        public override void Up()
        {
            this.ExecuteEmbeddedScripts(
                "Smartstore.DevTools.Data.Sql.SqlServer.Initial.sql",
                "Smartstore.DevTools.Data.Sql.MySql.Initial.sql");
        }

        public override void Down()
        {
            this.DeleteTables("DevToolsTestEntity");
        }
    }
}
