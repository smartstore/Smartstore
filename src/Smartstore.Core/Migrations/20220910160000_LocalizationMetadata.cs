using FluentMigrator;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;

namespace Smartstore.OutputCache.Migrations
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
            Create.Column(nameof(LocalizedProperty.MasterChecksum)).OnTable(propTableName).AsString(50).Nullable();

            var resTableName = nameof(LocaleStringResource);
            Create.Column(nameof(LocaleStringResource.CreatedOnUtc)).OnTable(resTableName).AsDateTime2().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);
            Create.Column(nameof(LocaleStringResource.UpdatedOnUtc)).OnTable(resTableName).AsDateTime2().Nullable();
            Create.Column(nameof(LocaleStringResource.CreatedBy)).OnTable(resTableName).AsString(100).Nullable();
            Create.Column(nameof(LocaleStringResource.UpdatedBy)).OnTable(resTableName).AsString(100).Nullable();
            Create.Column(nameof(LocaleStringResource.MasterChecksum)).OnTable(resTableName).AsString(50).Nullable();
        }
    }
}
