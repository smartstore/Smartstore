using FluentMigrator;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2023-06-02 20:30:00", "Core: StoreSslPort")]
    internal class StoreSslPortMigration : Migration
    {
        const string StoreTable = nameof(Store);
        const string SslPortColumn = nameof(Store.SslPort);

        public override void Up()
        {
            if (!Schema.Table(StoreTable).Column(SslPortColumn).Exists())
            {
                Create.Column(nameof(Store.SslPort)).OnTable(nameof(Store)).AsInt32().Nullable();
            }
        }

        public override void Down()
        {
            if (Schema.Table(StoreTable).Column(SslPortColumn).Exists())
            {
                Delete.Column(SslPortColumn).FromTable(StoreTable);
            }
        }
    }
}
