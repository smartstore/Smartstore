using FluentMigrator;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data;

namespace Smartstore.DevTools.Data.Migrations
{
    [MigrationVersion("2021-08-18 15:51:30", "DevTools: add test entity")]
    public class InitialMigration : Migration
    {
        private const string TABLE_NAME = "DevToolsTestEntity";

        public override void Up()
        {
            var dbSystem = DataSettings.Instance.DbFactory.DbSystem.ToString();

            if (!IfDatabase(dbSystem).Schema.Table(TABLE_NAME).Exists())
            {
                Create.Table(TABLE_NAME)
                    .WithColumn("Id").AsInt32().PrimaryKey().Identity().NotNullable()
                    .WithColumn("Name").AsString(400).NotNullable()
                    .WithColumn("Description").AsString(int.MaxValue).Nullable()
                    .WithColumn("PageSize").AsInt32().Nullable()
                    .WithColumn("LimitedToStores").AsBoolean().NotNullable().Indexed("IX_LimitedToStores")
                    .WithColumn("SubjectToAcl").AsBoolean().NotNullable().Indexed("IX_SubjectToAcl")
                    .WithColumn("Published").AsBoolean().NotNullable()
                    .WithColumn("Deleted").AsBoolean().NotNullable().Indexed("IX_Deleted")
                    .WithColumn("DisplayOrder").AsInt32().NotNullable().Indexed("IX_DisplayOrder")
                    .WithColumn("CreatedOnUtc").AsDateTime().NotNullable()
                    .WithColumn("UpdatedOnUtc").AsDateTime().NotNullable();
            }
        }

        public override void Down()
        {
            var dbSystem = DataSettings.Instance.DbFactory.DbSystem.ToString();

            if (IfDatabase(dbSystem).Schema.Table(TABLE_NAME).Exists())
            {
                Delete.Table(TABLE_NAME);
            }
        }
    }
}
