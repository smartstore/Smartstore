using FluentMigrator;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2023-06-02 20:30:00", "Core: StoreSslPort")]
    internal class StoreSslPortMigration : Migration
    {
        public override void Up()
        {
            Create.Column(nameof(Store.SslPort)).OnTable(nameof(Store)).AsInt32().Nullable();
        }

        public override void Down()
        {
        }
    }
}
