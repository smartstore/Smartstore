using FluentMigrator;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.Shipping.Migrations
{
    // INFO: AutoReversingMigration not possible. Throws NotSupportedException "The AlterColumnExpression cannot be automatically reversed".
    [MigrationVersion("2023-05-25 12:30:00", "ShippingByTotal: four decimal places")]
    internal class FourDecimalPlaces : Migration
    {
        const string shippingByTotalTable = "ShippingByTotal";

        static readonly string[] _columns = new[]
        {
            nameof(ShippingRateByTotal.From),
            nameof(ShippingRateByTotal.To),
            nameof(ShippingRateByTotal.ShippingChargePercentage),
            nameof(ShippingRateByTotal.ShippingChargeAmount),
            nameof(ShippingRateByTotal.BaseCharge),
            nameof(ShippingRateByTotal.MaxCharge)
        };

        public override void Up()
        {
            foreach (var column in _columns)
            {
                FixArithmeticOverflow(column);
            }

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

        private void FixArithmeticOverflow(string column)
        {
            try
            {
                IfDatabase("sqlserver").Execute.Sql($"UPDATE [dbo].[{shippingByTotalTable}] SET [{column}] = 99999999999999 WHERE [{column}] > 99999999999999");
            }
            catch
            {
            }
        }
    }
}
