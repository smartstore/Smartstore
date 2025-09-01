using FluentMigrator;
using Smartstore.Core.Data.Migrations;
using Smartstore.Google.MerchantCenter.Domain;

namespace Smartstore.Google.MerchantCenter.Migrations
{
    [MigrationVersion("2025-08-29 15:30:00", "GoogleMerchantCenter: Media file IDs")]
    internal class MediaFileIds : Migration
    {
        const string GmcProductTable = nameof(GoogleProduct);
        const string MediaFileIdsColumn = nameof(GoogleProduct.MediaFileIds);

        public override void Up()
        {
            if (!Schema.Table(GmcProductTable).Column(MediaFileIdsColumn).Exists())
            {
                Create.Column(MediaFileIdsColumn).OnTable(GmcProductTable).AsString(1000).Nullable();
            }
        }

        public override void Down()
        {
            if (Schema.Table(GmcProductTable).Column(MediaFileIdsColumn).Exists())
            {
                Delete.Column(MediaFileIdsColumn).FromTable(GmcProductTable);
            }
        }
    }
}
