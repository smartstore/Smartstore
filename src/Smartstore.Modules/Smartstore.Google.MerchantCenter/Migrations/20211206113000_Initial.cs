using FluentMigrator;
using Smartstore.Core.Data.Migrations;
using Smartstore.Google.MerchantCenter.Domain;

namespace Smartstore.Google.MerchantCenter.Migrations
{
    [MigrationVersion("2021-12-06 11:30:00", "GoogleMerchantCenter: Initial")]
    internal class Initial : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("GoogleProduct").Exists())
            {
                Create.Table("GoogleProduct")
                    .WithIdColumn()
                    .WithColumn(nameof(GoogleProduct.ProductId)).AsInt32().NotNullable()
                        .Indexed("IX_ProductId")
                    .WithColumn(nameof(GoogleProduct.Taxonomy)).AsString(4000).Nullable()
                    .WithColumn(nameof(GoogleProduct.Gender)).AsString(100).Nullable()
                    .WithColumn(nameof(GoogleProduct.AgeGroup)).AsString(100).Nullable()
                    .WithColumn(nameof(GoogleProduct.Color)).AsString(100).Nullable()
                    .WithColumn(nameof(GoogleProduct.Size)).AsString(100).Nullable()
                    .WithColumn(nameof(GoogleProduct.Material)).AsString(100).Nullable()
                    .WithColumn(nameof(GoogleProduct.Pattern)).AsString(100).Nullable()
                    .WithColumn(nameof(GoogleProduct.ItemGroupId)).AsString(4000).Nullable()
                    .WithColumn(nameof(GoogleProduct.IsTouched)).AsBoolean().NotNullable()
                        .Indexed("IX_IsTouched")
                    .WithColumn(nameof(GoogleProduct.CreatedOnUtc)).AsDateTime2().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
                    .WithColumn(nameof(GoogleProduct.UpdatedOnUtc)).AsDateTime2().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
                    .WithColumn(nameof(GoogleProduct.Export)).AsBoolean().NotNullable().WithDefaultValue(true)
                        .Indexed("IX_Export")
                    .WithColumn(nameof(GoogleProduct.Multipack)).AsInt32().NotNullable()
                    .WithColumn(nameof(GoogleProduct.IsBundle)).AsBoolean().Nullable()
                    .WithColumn(nameof(GoogleProduct.IsAdult)).AsBoolean().Nullable()
                    .WithColumn(nameof(GoogleProduct.EnergyEfficiencyClass)).AsString(50).Nullable()
                    .WithColumn(nameof(GoogleProduct.CustomLabel0)).AsString(100).Nullable()
                    .WithColumn(nameof(GoogleProduct.CustomLabel1)).AsString(100).Nullable()
                    .WithColumn(nameof(GoogleProduct.CustomLabel2)).AsString(100).Nullable()
                    .WithColumn(nameof(GoogleProduct.CustomLabel3)).AsString(100).Nullable()
                    .WithColumn(nameof(GoogleProduct.CustomLabel4)).AsString(100).Nullable();
            }
        }

        public override void Down()
        {
            // INFO: no down initial migration. Leave schema as it is or ask merchant to delete it.
        }
    }
}
