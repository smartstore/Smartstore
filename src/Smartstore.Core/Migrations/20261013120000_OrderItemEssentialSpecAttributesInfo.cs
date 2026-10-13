using FluentMigrator;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.Core.Migrations;

[MigrationVersion("2026-10-13 12:00:00", "Core: Essential specification attributes info")]
internal class OrderItemEssentialSpecAttributesInfo : Migration
{
    const string TableName = nameof(OrderItem);
    const string ColumnName = nameof(OrderItem.EssentialSpecificationAttributesInfo);

    public override void Up()
    {
        if (!Schema.Table(TableName).Column(ColumnName).Exists())
        {
            Create.Column(ColumnName)
                .OnTable(TableName)
                .AsMaxString()
                .Nullable();
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
