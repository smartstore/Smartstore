using FluentMigrator;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Stores;
using Smartstore.Domain;
using Smartstore.Shipping.Domain;
using System.Data;

namespace Smartstore.Shipping.Migrations
{
    [MigrationVersion("2021-11-11 13:30:00", "Shipping: Initial")]
    internal class Initial : Migration
    {
        public override void Up()
        {
            const string ShippingByTotal = "ShippingByTotal";
            const string id = nameof(BaseEntity.Id);

            if (!Schema.Table(ShippingByTotal).Exists())
            {
                Create.Table(ShippingByTotal)
                    .WithColumn(id).AsInt32().PrimaryKey().Identity().NotNullable()
                    .WithColumn(nameof(ShippingRateByTotal.ShippingMethodId)).AsInt32().NotNullable()
                        .Indexed("IX_ShippingMethodId")
                        .ForeignKey(nameof(ShippingMethod), id)
                        .OnDelete(Rule.Cascade)
                    .WithColumn(nameof(ShippingRateByTotal.StoreId)).AsInt32().NotNullable()
                        .Indexed("IX_StoreId")
                        .ForeignKey(nameof(Store), id)
                        .OnDelete(Rule.Cascade)
                    .WithColumn(nameof(ShippingRateByTotal.CountryId)).AsInt32().NotNullable()
                        .Indexed("IX_CountryId")
                        .ForeignKey(nameof(Country), id)
                        .OnDelete(Rule.Cascade)
                    .WithColumn(nameof(ShippingRateByTotal.StateProvinceId)).AsInt32().NotNullable()
                        .Indexed("IX_StateProvinceId")
                        .ForeignKey(nameof(StateProvince), id)
                        .OnDelete(Rule.Cascade)
                    .WithColumn(nameof(ShippingRateByTotal.Zip)).AsString(10).NotNullable()
                    .WithColumn(nameof(ShippingRateByTotal.From)).AsDecimal().NotNullable()
                    .WithColumn(nameof(ShippingRateByTotal.To)).AsDecimal().Nullable()
                    .WithColumn(nameof(ShippingRateByTotal.UsePercentage)).AsBoolean()
                    .WithColumn(nameof(ShippingRateByTotal.ShippingChargePercentage)).AsDecimal(4, 18).NotNullable()
                    .WithColumn(nameof(ShippingRateByTotal.ShippingChargeAmount)).AsDecimal().NotNullable()
                    .WithColumn(nameof(ShippingRateByTotal.BaseCharge)).AsDecimal().NotNullable()
                    .WithColumn(nameof(ShippingRateByTotal.MaxCharge)).AsDecimal().Nullable();
            }
        }

        public override void Down()
        {
            // INFO: no down initial migration. Leave Shipping rate schema as it is or ask merchant to delete it.
        }
    }
}
