using FluentMigrator;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2022-09-10 16:00:00", "Core: LocalizationMetadata")]
    internal class LocalizationMetadata : AutoReversingMigration
    {
        public override void Up()
        {
            var propTableName = nameof(LocalizedProperty);
            Create.Column(nameof(LocalizedProperty.IsHidden)).OnTable(propTableName).AsBoolean().NotNullable().WithDefaultValue(false);
            Create.Column(nameof(LocalizedProperty.CreatedOnUtc)).OnTable(propTableName).AsDateTime2().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);
            Create.Column(nameof(LocalizedProperty.UpdatedOnUtc)).OnTable(propTableName).AsDateTime2().Nullable();
            Create.Column(nameof(LocalizedProperty.CreatedBy)).OnTable(propTableName).AsString(100).Nullable();
            Create.Column(nameof(LocalizedProperty.UpdatedBy)).OnTable(propTableName).AsString(100).Nullable();
            Create.Column(nameof(LocalizedProperty.TranslatedOnUtc)).OnTable(propTableName).AsDateTime2().Nullable();
            Create.Column(nameof(LocalizedProperty.MasterChecksum)).OnTable(propTableName).AsString(64).Nullable();

            Create.Index("IX_TranslatedOn")
                .OnTable(propTableName)
                .OnColumn(nameof(LocalizedProperty.TranslatedOnUtc)).Ascending()
                .WithOptions()
                .NonClustered();

            var resTableName = nameof(LocaleStringResource);
            Create.Column(nameof(LocaleStringResource.CreatedOnUtc)).OnTable(resTableName).AsDateTime2().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);
            Create.Column(nameof(LocaleStringResource.UpdatedOnUtc)).OnTable(resTableName).AsDateTime2().Nullable();
            Create.Column(nameof(LocaleStringResource.CreatedBy)).OnTable(resTableName).AsString(100).Nullable();
            Create.Column(nameof(LocaleStringResource.UpdatedBy)).OnTable(resTableName).AsString(100).Nullable();
            Create.Column(nameof(LocaleStringResource.MasterChecksum)).OnTable(resTableName).AsString(64).Nullable();
        }
    }
}
