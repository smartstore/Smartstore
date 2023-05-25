using FluentMigrator;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.Shipping.Migrations
{
    // INFO: AutoReversingMigration not possible. Throws NotSupportedException "The AlterColumnExpression cannot be automatically reversed".
    [MigrationVersion("2023-05-25 12:30:00", "ShippingByTotal: four decimal places")]
    internal class FourDecimalPlaces : Migration
    {
        const string shippingByTotalTable = "ShippingByTotal";

        public override void Up()
        {
            MigrateInternal(4);
        }

        public override void Down()
        {
            MigrateInternal(2);
        }

        private void MigrateInternal(int precision)
        {
            Alter.Column(nameof(ShippingRateByTotal.From)).OnTable(shippingByTotalTable).AsDecimal(18, precision).NotNullable();
            Alter.Column(nameof(ShippingRateByTotal.To)).OnTable(shippingByTotalTable).AsDecimal(18, precision).Nullable();
            Alter.Column(nameof(ShippingRateByTotal.ShippingChargePercentage)).OnTable(shippingByTotalTable).AsDecimal(18, precision).NotNullable();
            Alter.Column(nameof(ShippingRateByTotal.ShippingChargeAmount)).OnTable(shippingByTotalTable).AsDecimal(18, precision).NotNullable();
            Alter.Column(nameof(ShippingRateByTotal.BaseCharge)).OnTable(shippingByTotalTable).AsDecimal(18, precision).NotNullable();
            Alter.Column(nameof(ShippingRateByTotal.MaxCharge)).OnTable(shippingByTotalTable).AsDecimal(18, precision).Nullable();
        }
    }
}
