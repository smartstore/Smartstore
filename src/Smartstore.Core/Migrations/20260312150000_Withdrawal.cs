using FluentMigrator;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data.Migrations;
using RcEntity = Smartstore.Core.Checkout.Orders.ReturnCase;

namespace Smartstore.Core.Migrations;

[MigrationVersion("2026-03-12 15:00:00", "Core: Withdrawal")]
internal class Withdrawal : Migration
{
    const string RcTable = nameof(Checkout.Orders.ReturnCase);
    const string WithdrawalIdColumn = nameof(RcEntity.WithdrawalId);

    const string OrderTable = nameof(Order);
    const string CompletedOnColumn = nameof(Order.CompletedOn);

    public override void Up()
    {
        if (!Schema.Table(RcTable).Column(WithdrawalIdColumn).Exists())
        {
            Create.Column(WithdrawalIdColumn).OnTable(RcTable)
                .AsInt32()
                .Nullable()
                .Indexed();
        }

        if (!Schema.Table(OrderTable).Column(CompletedOnColumn).Exists())
        {
            Create.Column(CompletedOnColumn).OnTable(OrderTable)
                .AsDateTime2()
                .Nullable();
        }

        // TODO: (mg) (w) Add the upcoming "Withdrawal period in days" properties for category/product here.
    }

    public override void Down()
    {
        if (Schema.Table(RcTable).Column(WithdrawalIdColumn).Exists())
        {
            Delete.Column(WithdrawalIdColumn).FromTable(RcTable);
        }

        if (Schema.Table(OrderTable).Column(CompletedOnColumn).Exists())
        {
            Delete.Column(CompletedOnColumn).FromTable(OrderTable);
        }
    }
}
