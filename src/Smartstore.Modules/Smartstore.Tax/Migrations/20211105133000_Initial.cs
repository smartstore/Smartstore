using FluentMigrator;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Data.Migrations;
using Smartstore.Domain;
using Smartstore.Tax.Domain;
using System.Data;

namespace Smartstore.Tax.Migrations
{
    [MigrationVersion("2021-11-05 13:30:00", "Tax: Initial")]
    internal class Initial : Migration
    {
        public override void Up()
        {
            const string taxRate = "TaxRate";
            const string id = nameof(BaseEntity.Id);

            if (!Schema.Table(taxRate).Exists())
            {
                Create.Table(taxRate)
                    .WithIdColumn()
                    .WithColumn(nameof(TaxRateEntity.TaxCategoryId)).AsInt32().NotNullable()
                        .Indexed("IX_TaxCategoryId")
                        .ForeignKey(nameof(TaxCategory), id).OnDelete(Rule.Cascade)
                    .WithColumn(nameof(TaxRateEntity.CountryId)).AsInt32().NotNullable()
                        .Indexed("IX_CountryId")
                        .ForeignKey(nameof(Country), id).OnDelete(Rule.Cascade)
                    .WithColumn(nameof(TaxRateEntity.StateProvinceId)).AsInt32().NotNullable()
                        .Indexed("IX_StateProvinceId")
                        .ForeignKey(nameof(StateProvince), id).OnDelete(Rule.Cascade)
                    .WithColumn(nameof(TaxRateEntity.Zip)).AsString(100).NotNullable()
                    .WithColumn(nameof(TaxRateEntity.Percentage)).AsDecimal(4, 18).NotNullable();
            }
            else
            {
                if (!Schema.Table(taxRate).Index("IX_TaxCategoryId").Exists())
                {
                    Create.Index("IX_ShippingMethodId")
                        .OnTable(taxRate)
                        .OnColumn(nameof(TaxRateEntity.TaxCategoryId));

                    Create.ForeignKey()
                        .FromTable(taxRate)
                        .ForeignColumn(id)
                        .ToTable(nameof(StateProvince))
                        .PrimaryColumn(id);
                }
                if (!Schema.Table(taxRate).Index("IX_CountryId").Exists())
                {
                    Create.Index("IX_CountryId")
                        .OnTable(taxRate)
                        .OnColumn(nameof(TaxRateEntity.CountryId));

                    Create.ForeignKey()
                        .FromTable(taxRate)
                        .ForeignColumn(id)
                        .ToTable(nameof(Country))
                        .PrimaryColumn(id);
                }
                if (!Schema.Table(taxRate).Index("IX_StateProvinceId").Exists())
                {
                    Create.Index("IX_StateProvinceId")
                        .OnTable(taxRate)
                        .OnColumn(nameof(TaxRateEntity.StateProvinceId));

                    Create.ForeignKey()
                        .FromTable(taxRate)
                        .ForeignColumn(id)
                        .ToTable(nameof(StateProvince))
                        .PrimaryColumn(id);
                }
            }
        }

        public override void Down()
        {
            // INFO: no down initial migration. Leave tax rate schema as it is or ask merchant to delete it.
        }
    }
}
