using System;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.DevTools.Data.Migrations
{
    [MigrationVersion("2021-08-18 15:51:30", "Add DevTools test entity.")]
    public class TestEntityMigration : DataMigrationBase
    {
        public override void Up()
        {
            ExecuteEmbeddedScripts("Smartstore.DevTools.Data.{0}.Sql.AddTestEntity.sql");
        }

        public override void Down()
        {
            throw new NotImplementedException();
        }
    }
}
