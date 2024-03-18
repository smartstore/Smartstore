using FluentMigrator;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2024-03-01 11:00:00", "Core: retail")]
    internal class Retail : Migration
    {
        public override void Up()
        {
            Alter.Column(nameof(Order.BillingAddressId)).OnTable(nameof(Order)).AsInt32().Nullable();
        }

        public override void Down()
        {
            Alter.Column(nameof(Order.BillingAddressId)).OnTable(nameof(Order)).AsInt32().NotNullable();
        }
    }
}
