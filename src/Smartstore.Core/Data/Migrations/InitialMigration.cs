using FluentMigrator;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2021-01-01 00:00:00", "Smartstore Core database schema.")]
    public class InitialMigration : Migration
    {
        public override void Up()
        {
            "!! Initial migration: Smartstore Core database schema !!".Dump();

            //this.ExecuteEmbeddedScripts(
            //    "Smartstore.Core.Data.Sql.SqlServer.Initial.sql",
            //    "Smartstore.Core.Data.Sql.MySql.Initial.sql");
        }

        public override void Down()
        {
        }
    }
}
