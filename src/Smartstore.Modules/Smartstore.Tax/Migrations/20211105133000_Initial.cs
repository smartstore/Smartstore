using FluentMigrator;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.Tax.Migrations
{
    [MigrationVersion("2021-11-05 13:30:00", "Tax: Initial")]
    internal class Initial : Migration
    {
        public override void Up()
        {
            const string taxRate = "TaxRate";

            if (!Schema.Table(taxRate).Exists())
            {
                Create.Table(taxRate)
                    .WithIdColumn()
                    .WithColumn(nameof(TaxRateEntity.TaxCategoryId)).AsInt32().NotNullable()
                    .WithColumn(nameof(TaxRateEntity.CountryId)).AsInt32().NotNullable()
                    .WithColumn(nameof(TaxRateEntity.StateProvinceId)).AsInt32().NotNullable()
                    .WithColumn(nameof(TaxRateEntity.Zip)).AsString(100).Nullable()
                    .WithColumn(nameof(TaxRateEntity.Percentage)).AsDecimal(18, 4).NotNullable();
            }
        }

        public override void Down()
        {
            // INFO: no down initial migration. Leave tax rate schema as it is or ask merchant to delete it.
        }
    }
}
