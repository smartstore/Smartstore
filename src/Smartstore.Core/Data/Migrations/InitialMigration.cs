using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2021-01-01 00:00:00", "Smartstore Core database schema.")]
    public class InitialMigration : DatabaseMigrationBase
    {
        public override void Up()
        {
            if (!DbMigrationManager.Instance.SuppressInitialCreate<SmartDbContext>())
            {
                "!! Initial migration: Smartstore Core database schema !!".Dump();

                //this.ExecuteEmbeddedScripts(
                //    "Smartstore.Core.Data.Sql.SqlServer.Initial.sql",
                //    "Smartstore.Core.Data.Sql.MySql.Initial.sql");
            }
        }

        public override void Down()
        {
        }
    }
}
