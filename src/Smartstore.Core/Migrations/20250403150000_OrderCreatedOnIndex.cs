using FluentMigrator;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2025-04-03 15:00:00", "Core: Order created on index")]
    internal class OrderCreatedOnIndex : Migration
    {
        const string OrderTable = nameof(Order);
        const string CreatedOnColumn = nameof(Order.CreatedOnUtc);
        const string CreatedOnIndex = "IX_Order_CreatedOnUtc";

        public override void Up()
        {
            if (!Schema.Table(OrderTable).Index(CreatedOnIndex).Exists())
            {
                Create.Index(CreatedOnIndex)
                    .OnTable(OrderTable)
                    .OnColumn(CreatedOnColumn)
                    .Ascending()
                    .WithOptions()
                    .NonClustered();
            }
        }

        public override void Down()
        {
            if (Schema.Table(OrderTable).Index(CreatedOnIndex).Exists())
            {
                Delete.Index(CreatedOnIndex).OnTable(OrderTable);
            }
        }
    }
}
