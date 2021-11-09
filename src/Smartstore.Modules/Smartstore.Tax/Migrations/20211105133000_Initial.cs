using FluentMigrator;
using Smartstore.Core.Data.Migrations;
using Smartstore.Domain;
using Smartstore.Tax.Domain;

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
                    .WithColumn(id).AsInt32().PrimaryKey().Identity().NotNullable()
                    .WithColumn(nameof(TaxRateEntity.TaxCategoryId)).AsInt32().NotNullable()
                    .WithColumn(nameof(TaxRateEntity.CountryId)).AsInt32().NotNullable()
                    .WithColumn(nameof(TaxRateEntity.StateProvinceId)).AsInt32().NotNullable()
                    .WithColumn(nameof(TaxRateEntity.Zip)).AsString(10).NotNullable()
                    .WithColumn(nameof(TaxRateEntity.Percentage)).AsDecimal(4, 18).NotNullable();
            }
        }

        public override void Down()
        {
            // INFO: no down initial migration. Leave tax rate schema as it is or ask merchant to delete it.
        }
    }
}
