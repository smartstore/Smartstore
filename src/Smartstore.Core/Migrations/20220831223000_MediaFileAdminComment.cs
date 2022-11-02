using FluentMigrator;
using Smartstore.Core.Content.Media;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2022-08-31 22:30:00", "Core: MediaFileAdminComment")]
    internal class MediaFileAdminComment : Migration
    {
        public override void Up()
        {
            var tableName = nameof(MediaFile);
            var columnName = nameof(MediaFile.AdminComment);

            if (!Schema.Table(tableName).Column(columnName).Exists())
            {
                Create.Column(columnName).OnTable(tableName).AsString(400).Nullable();
            }
        }

        public override void Down()
        {
        }
    }
}
