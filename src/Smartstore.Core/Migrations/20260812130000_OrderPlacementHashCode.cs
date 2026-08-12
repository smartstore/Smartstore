using FluentMigrator;
using FluentMigrator.SqlServer;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.Core.Migrations;

[MigrationVersion("2026-08-12 13:00:00", "Core: Order placement hash code")]
internal class OrderPlacementHashCode : Migration
{
    const string TableName = nameof(Order);
    const string ColumnName = nameof(Order.OrderPlacementHashCode);
    const string IndexName = "IX_Order_OrderPlacementHashCode";

    public override void Up()
    {
        var order = Schema.Table(TableName);

        if (!order.Column(ColumnName).Exists())
        {
            Create.Column(ColumnName)
                .OnTable(TableName)
                .AsInt32()
                .Nullable();
        }

        if (!order.Index(IndexName).Exists())
        {
            Create.Index(IndexName)
                .OnTable(TableName)
                .OnColumn(ColumnName).Ascending()
                .WithOptions().Unique()
                .WithOptions().Filter($"([{ColumnName}] IS NOT NULL)");
        }
    }

    public override void Down()
    {
        var order = Schema.Table(TableName);

        if (order.Index(IndexName).Exists())
        {
            Delete.Index(IndexName).OnTable(TableName);
        }

        if (order.Column(ColumnName).Exists())
        {
            Delete.Column(ColumnName).FromTable(TableName);
        }
    }
}
