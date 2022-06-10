using FluentMigrator;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.Shipping.Migrations
{
    [MigrationVersion("2021-11-11 13:30:00", "Shipping: Initial")]
    internal class Initial : Migration
    {
        public override void Up()
        {
            const string ShippingByTotal = "ShippingByTotal";

            if (!Schema.Table(ShippingByTotal).Exists())
            {
                Create.Table(ShippingByTotal)
                    .WithIdColumn()
                    .WithColumn(nameof(ShippingRateByTotal.ShippingMethodId)).AsInt32().NotNullable()
                    .WithColumn(nameof(ShippingRateByTotal.StoreId)).AsInt32().NotNullable()
                    .WithColumn(nameof(ShippingRateByTotal.CountryId)).AsInt32().Nullable()
                    .WithColumn(nameof(ShippingRateByTotal.StateProvinceId)).AsInt32().Nullable()
                    .WithColumn(nameof(ShippingRateByTotal.Zip)).AsString(100).Nullable()
                    .WithColumn(nameof(ShippingRateByTotal.From)).AsDecimal(18, 2).NotNullable()
                    .WithColumn(nameof(ShippingRateByTotal.To)).AsDecimal(18, 2).Nullable()
                    .WithColumn(nameof(ShippingRateByTotal.UsePercentage)).AsBoolean().NotNullable()
                    .WithColumn(nameof(ShippingRateByTotal.ShippingChargePercentage)).AsDecimal(18, 2).NotNullable()
                    .WithColumn(nameof(ShippingRateByTotal.ShippingChargeAmount)).AsDecimal(18, 2).NotNullable()
                    .WithColumn(nameof(ShippingRateByTotal.BaseCharge)).AsDecimal(18, 2).NotNullable()
                    .WithColumn(nameof(ShippingRateByTotal.MaxCharge)).AsDecimal(18, 2).Nullable();
            }
        }

        public override void Down()
        {
            // INFO: no down initial migration. Leave Shipping rate schema as it is or ask merchant to delete it.
        }
    }
}
