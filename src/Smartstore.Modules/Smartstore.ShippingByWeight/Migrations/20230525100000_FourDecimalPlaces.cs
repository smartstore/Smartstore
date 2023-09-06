using FluentMigrator;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.ShippingByWeight.Migrations
{
    // INFO: AutoReversingMigration not possible. Throws NotSupportedException "The AlterColumnExpression cannot be automatically reversed".
    [MigrationVersion("2023-05-25 10:00:00", "ShippingByWeight: four decimal places")]
    internal class FourDecimalPlaces : Migration
    {
        const string shippingByWeightTable = "ShippingByWeight";

        static readonly string[] _columns = new[]
        {
            nameof(ShippingRateByWeight.From),
            nameof(ShippingRateByWeight.To),
            nameof(ShippingRateByWeight.ShippingChargePercentage),
            nameof(ShippingRateByWeight.ShippingChargeAmount),
            nameof(ShippingRateByWeight.SmallQuantitySurcharge),
            nameof(ShippingRateByWeight.SmallQuantityThreshold)
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
            Alter.Column(nameof(ShippingRateByWeight.From)).OnTable(shippingByWeightTable).AsDecimal(18, precision).NotNullable();
            Alter.Column(nameof(ShippingRateByWeight.To)).OnTable(shippingByWeightTable).AsDecimal(18, precision).NotNullable();
            Alter.Column(nameof(ShippingRateByWeight.ShippingChargePercentage)).OnTable(shippingByWeightTable).AsDecimal(18, precision).NotNullable();
            Alter.Column(nameof(ShippingRateByWeight.ShippingChargeAmount)).OnTable(shippingByWeightTable).AsDecimal(18, precision).NotNullable();
            Alter.Column(nameof(ShippingRateByWeight.SmallQuantitySurcharge)).OnTable(shippingByWeightTable).AsDecimal(18, precision).NotNullable();
            Alter.Column(nameof(ShippingRateByWeight.SmallQuantityThreshold)).OnTable(shippingByWeightTable).AsDecimal(18, precision).NotNullable();
        }

        private void FixArithmeticOverflow(string column)
        {
            try
            {
                IfDatabase("sqlserver").Execute.Sql($"UPDATE [dbo].[{shippingByWeightTable}] SET [{column}] = 99999999999999 WHERE [{column}] > 99999999999999");
            }
            catch
            {
            }
        }
    }
}
