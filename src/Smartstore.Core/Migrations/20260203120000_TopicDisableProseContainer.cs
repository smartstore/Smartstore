using FluentMigrator;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2026-02-03 12:00:00", "Core: Topic DisableProseContainer")]
    internal class TopicDisableProseContainer : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("Topic").Column("DisableProseContainer").Exists())
            {
                Create.Column("DisableProseContainer").OnTable("Topic")
                    .AsBoolean()
                    .NotNullable()
                    .WithDefaultValue(false);
            }
        }

        public override void Down()
        {
            if (Schema.Table("Topic").Column("DisableProseContainer").Exists())
            {
                Delete.Column("DisableProseContainer").FromTable("Topic");
            }
        }
    }
}