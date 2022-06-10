using FluentMigrator.Runner.VersionTableInfo;

namespace Smartstore.Core.Data.Migrations
{
    /// <summary>
    /// Represents the migration database table. It contains version information about applied migrations.
    /// </summary>
    [VersionTableMetaData]
    public class MigrationHistory : IVersionTableMetaData
    {
        public long Version { get; set; }

        /// <summary>
        /// Maximum length of 1024 characters.
        /// </summary>
        public string Description { get; set; }

        public DateTime AppliedOn { get; set; }

        #region IVersionTableMetaData

        public string SchemaName => string.Empty;

        public string TableName => "__MigrationVersionInfo";

        public string ColumnName => nameof(Version);

        public string DescriptionColumnName => nameof(Description);

        public string AppliedOnColumnName => nameof(AppliedOn);

        public string UniqueIndexName => "UC_Version";

        public bool OwnsSchema => true;

        public object ApplicationContext { get; set; }

        #endregion
    }
}
