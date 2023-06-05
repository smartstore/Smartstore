using FluentMigrator;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2023-06-05 09:30:00", "Core: OrderItem SKU")]
    internal class OrderItemSku : Migration
    {
        const string OrderItemTable = nameof(OrderItem);
        const string SkuColumn = nameof(OrderItem.Sku);

        public override void Up()
        {
            if (!Schema.Table(OrderItemTable).Column(SkuColumn).Exists())
            {
                Create.Column(SkuColumn).OnTable(OrderItemTable).AsString(400).Nullable();
            }
        }

        public override void Down()
        {
            if (Schema.Table(OrderItemTable).Column(SkuColumn).Exists())
            {
                Delete.Column(SkuColumn).FromTable(OrderItemTable);
            }
        }
    }
}
