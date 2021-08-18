using System;
using FluentMigrator;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.DevTools.Data.Migrations
{
    [MigrationVersion("2021-08-18 15:51:30", "Add DevTools test entity.")]
    public class TestEntityMigration : Migration
    {
        public override void Up()
        {
            IfDatabase("SqlServer").Execute.EmbeddedScript("Smartstore.DevTools.Data.SqlServer.Sql.AddTestEntity.sql");
            IfDatabase("MySql").Execute.EmbeddedScript("Smartstore.DevTools.Data.MySql.Sql.AddTestEntity.sql");
        }

        public override void Down()
        {
            throw new NotImplementedException();
        }
    }
}
