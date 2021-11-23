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
                    .WithIdColumn()
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
                    .WithColumn(nameof(ShippingRateByTotal.Zip)).AsString(100).NotNullable()
                    .WithColumn(nameof(ShippingRateByTotal.From)).AsDecimal().NotNullable()
                    .WithColumn(nameof(ShippingRateByTotal.To)).AsDecimal().Nullable()
                    .WithColumn(nameof(ShippingRateByTotal.UsePercentage)).AsBoolean()
                    .WithColumn(nameof(ShippingRateByTotal.ShippingChargePercentage)).AsDecimal(4, 18).NotNullable()
                    .WithColumn(nameof(ShippingRateByTotal.ShippingChargeAmount)).AsDecimal().NotNullable()
                    .WithColumn(nameof(ShippingRateByTotal.BaseCharge)).AsDecimal().NotNullable()
                    .WithColumn(nameof(ShippingRateByTotal.MaxCharge)).AsDecimal().Nullable();
            }
            else
            {
                if (!Schema.Table(ShippingByTotal).Index("IX_ShippingMethodId").Exists())
                {
                    Create.Index("IX_ShippingMethodId")
                        .OnTable(ShippingByTotal)
                        .OnColumn(nameof(ShippingRateByTotal.ShippingMethodId));

                    Create.ForeignKey()
                        .FromTable(ShippingByTotal)
                        .ForeignColumn(id)
                        .ToTable(nameof(StateProvince))
                        .PrimaryColumn(id);
                }
                if (!Schema.Table(ShippingByTotal).Index("IX_StoreId").Exists())
                {
                    Create.Index("IX_StoreId")
                        .OnTable(ShippingByTotal)
                        .OnColumn(nameof(ShippingRateByTotal.StoreId));

                    Create.ForeignKey()
                        .FromTable(ShippingByTotal)
                        .ForeignColumn(id)
                        .ToTable(nameof(Store))
                        .PrimaryColumn(id);
                }
                if (!Schema.Table(ShippingByTotal).Index("IX_CountryId").Exists())
                {
                    Create.Index("IX_CountryId")
                        .OnTable(ShippingByTotal)
                        .OnColumn(nameof(ShippingRateByTotal.CountryId));

                    Create.ForeignKey()
                        .FromTable(ShippingByTotal)
                        .ForeignColumn(id)
                        .ToTable(nameof(Country))
                        .PrimaryColumn(id);
                }
                if (!Schema.Table(ShippingByTotal).Index("IX_StateProvinceId").Exists())
                {
                    Create.Index("IX_StateProvinceId")
                        .OnTable(ShippingByTotal)
                        .OnColumn(nameof(ShippingRateByTotal.StateProvinceId));

                    Create.ForeignKey()
                        .FromTable(ShippingByTotal)
                        .ForeignColumn(id)
                        .ToTable(nameof(StateProvince))
                        .PrimaryColumn(id);
                }
            }
        }

        public override void Down()
        {
            // INFO: no down initial migration. Leave Shipping rate schema as it is or ask merchant to delete it.
        }
    }
}
