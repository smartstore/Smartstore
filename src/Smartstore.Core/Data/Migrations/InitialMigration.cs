using FluentMigrator;

namespace Smartstore.Core.Data.Migrations
{
    // TODO: (mg) (core) remove test migration later.
    [MigrationVersion("2021-01-01 00:00:00", "Smartstore Core test migration")]
    public class InitialMigration : Migration
    {
        public override void Up()
        {
            "!! Smartstore Core test migration !!".Dump();
        }

        public override void Down()
        {
        }
    }
}
