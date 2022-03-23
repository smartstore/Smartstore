namespace Smartstore.Data.Providers
{
    /// <summary>
    /// Information about a database backup extracted from its file name.
    /// </summary>
    public class DbBackupInfo
    {
        public DbBackupInfo(string name)
        {
            Name = name;
        }

        /// <summary>
        /// A value indicating whether the backup is valid.
        /// </summary>
        public bool Valid { get; init; }

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
        /// A value indicating whether the backup version equals current Smartstore version.
        /// </summary>
        public bool IsCurrentVersion
            => Version != null && Version == SmartstoreVersion.Version;
    }
}
