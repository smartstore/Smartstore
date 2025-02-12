using FluentMigrator;
using Smartstore.Core.Logging;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2025-01-23 22:00:00", "Core: Log.UserAgent")]
    internal class LogUserAgent : Migration
    {
        const string TableName = nameof(Log);
        const string ColumnName = nameof(Log.UserAgent);

        public override void Up()
        {
            if (!Schema.Table(TableName).Column(ColumnName).Exists())
            {
                Create.Column(ColumnName).OnTable(TableName).AsString(450).Nullable();
            }
        }

        public override void Down()
        {
            if (Schema.Table(TableName).Column(ColumnName).Exists())
            {
                Delete.Column(ColumnName).FromTable(TableName);
            }
        }
    }
}
