using FluentMigrator;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.ShippingByWeight.Migrations
{
    [MigrationVersion("2021-11-29 11:30:00", "ShippingByWeight: Initial")]
    internal class Initial : Migration
    {
        public override void Up()
        {
            const string ShippingByWeight = "ShippingByWeight";

            if (!Schema.Table(ShippingByWeight).Exists())
            {
                Create.Table(ShippingByWeight)
                    .WithIdColumn()
                    .WithColumn(nameof(ShippingRateByWeight.ShippingMethodId)).AsInt32().NotNullable()
                    .WithColumn(nameof(ShippingRateByWeight.StoreId)).AsInt32().NotNullable()
                    .WithColumn(nameof(ShippingRateByWeight.CountryId)).AsInt32().NotNullable()
                    .WithColumn(nameof(ShippingRateByWeight.Zip)).AsString(100).Nullable()
                    .WithColumn(nameof(ShippingRateByWeight.From)).AsDecimal(18, 2).NotNullable()
                    .WithColumn(nameof(ShippingRateByWeight.To)).AsDecimal(18, 2).NotNullable()
                    .WithColumn(nameof(ShippingRateByWeight.UsePercentage)).AsBoolean().NotNullable()
                    .WithColumn(nameof(ShippingRateByWeight.ShippingChargePercentage)).AsDecimal(18, 2).NotNullable()
                    .WithColumn(nameof(ShippingRateByWeight.ShippingChargeAmount)).AsDecimal(18, 2).NotNullable()
                    .WithColumn(nameof(ShippingRateByWeight.SmallQuantitySurcharge)).AsDecimal(18, 2).NotNullable();
            }
        }

        public override void Down()
        {
            // INFO: no down initial migration. Leave Shipping rate schema as it is or ask merchant to delete it.
        }
    }
}
