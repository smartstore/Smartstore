using FluentMigrator;
using FluentMigrator.SqlServer;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.Core.Migrations;

[MigrationVersion("2026-08-12 13:00:00", "Core: Payment reference hash code")]
internal class PaymentReferenceHashCode : Migration
{
    const string TableName = nameof(Order);
    const string ColumnName = nameof(Order.PaymentReferenceHashCode);
    const string IndexName = "IX_Order_PaymentReferenceHashCode";

    public override void Up()
    {
        var order = Schema.Table(TableName);

        if (!order.Column(ColumnName).Exists())
        {
            Create.Column(ColumnName)
                .OnTable(TableName)
                .AsInt64()
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
