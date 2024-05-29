using FluentMigrator;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2024-05-27 20:15:00", "Core: ClientIdent")]
    internal class ClientIdent : Migration
    {
        const string CustomerTable = nameof(Customer);
        const string ClientIdentIndexName = "IX_Customer_ClientIdent";
        const string ClientIdentColumn = nameof(Customer.ClientIdent);
        const string LastVisitedPageColumn = nameof(Customer.LastVisitedPage);

        public override void Up()
        {
            if (!Schema.Table(CustomerTable).Column(ClientIdentColumn).Exists())
            {
                Create.Column(ClientIdentColumn).OnTable(CustomerTable).AsString(32).Nullable()
                    .Indexed(ClientIdentIndexName);
            }

            if (!Schema.Table(CustomerTable).Column(LastVisitedPageColumn).Exists())
            {
                Create.Column(LastVisitedPageColumn).OnTable(CustomerTable).AsString(2048).Nullable();
            }
        }

        public override void Down()
        {
            if (Schema.Table(CustomerTable).Index(ClientIdentIndexName).Exists())
            {
                Delete.Index(ClientIdentIndexName).OnTable(CustomerTable);
            }

            if (Schema.Table(CustomerTable).Column(ClientIdentColumn).Exists())
            {
                Delete.Column(ClientIdentColumn).FromTable(CustomerTable);
            }

            if (Schema.Table(CustomerTable).Column(LastVisitedPageColumn).Exists())
            {
                Delete.Column(LastVisitedPageColumn).FromTable(CustomerTable);
            }
        }
    }
}
