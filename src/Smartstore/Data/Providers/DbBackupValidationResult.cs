namespace Smartstore.Data.Providers
{
    /// <summary>
    /// Represents the result of a database backup name validation.
    /// </summary>
    public class DbBackupValidationResult
    {
        public DbBackupValidationResult(string name)
        {
            Name = name;
        }

        /// <summary>
        /// A value indicating whether the backup is valid.
        /// </summary>
        public bool IsValid { get; init; }

        /// <summary>
        /// The file name of the database backup.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The Smartstore version under which the backup was created.
        /// </summary>
        public Version Version { get; init; }

        /// <summary>
        /// Timestamp representing the date (in local time) when the backup was created.
        /// </summary>
        public DateTime Timestamp { get; init; }

        /// <summary>
        /// A value indicating whether the backup version matches current Smartstore version.
        /// </summary>
        public bool MatchesCurrentVersion
            => Version != null && Version == SmartstoreVersion.Version;
    }
}
