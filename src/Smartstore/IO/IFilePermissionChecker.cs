namespace Smartstore.IO
{
    [Flags]
    public enum FileEntryRights
    {
        Read = 1 << 0,
        Delete = 1 << 1,
        Modify = 1 << 2,
        Write = 1 << 3
    }

    /// <summary>
    /// Checks file system access rights
    /// </summary>
    public interface IFilePermissionChecker
    {
        /// <summary>
        /// Checks whether current user has permission to access given file entry.
        /// </summary>
        /// <param name="entry">File entry to check (file or directory)</param>
        /// <param name="rights">The rights to check.</param>
        bool CanAccess(FileSystemInfo entry, FileEntryRights rights);
    }

    public static class IFilePermissionCheckerExtensions
    {
        /// <summary>
        /// Checks whether current user has permission to access given file entry.
        /// </summary>
        /// <param name="entry">File entry to check (file or directory)</param>
        /// <param name="rights">The rights to check.</param>
        public static bool CanAccess(this IFilePermissionChecker checker, IFileEntry entry, FileEntryRights rights)
            => checker.CanAccess(ToFsInfo(entry), rights);

        /// <summary>
        /// Checks whether current user has permission to read given file entry.
        /// </summary>
        /// <param name="entry">File entry to check (file or directory)</param>
        public static bool CanRead(this IFilePermissionChecker checker, FileSystemInfo entry)
            => checker.CanAccess(entry, FileEntryRights.Read);

        /// <summary>
        /// Checks whether current user has permission to read given file entry.
        /// </summary>
        /// <param name="entry">File entry to check (file or directory)</param>
        public static bool CanRead(this IFilePermissionChecker checker, IFileEntry entry)
            => checker.CanAccess(entry, FileEntryRights.Read);

        /// <summary>
        /// Checks whether current user has permission to write to given file entry.
        /// </summary>
        /// <param name="entry">File entry to check (file or directory)</param>
        public static bool CanWrite(this IFilePermissionChecker checker, FileSystemInfo entry)
            => checker.CanAccess(entry, FileEntryRights.Write);

        /// <summary>
        /// Checks whether current user has permission to write to given file entry.
        /// </summary>
        /// <param name="entry">File entry to check (file or directory)</param>
        public static bool CanWrite(this IFilePermissionChecker checker, IFileEntry entry)
            => checker.CanAccess(entry, FileEntryRights.Write);

        /// <summary>
        /// Checks whether current user has permission to modify given file entry.
        /// </summary>
        /// <param name="entry">File entry to check (file or directory)</param>
        public static bool CanModify(this IFilePermissionChecker checker, FileSystemInfo entry)
            => checker.CanAccess(entry, FileEntryRights.Modify);

        /// <summary>
        /// Checks whether current user has permission to modify given file entry.
        /// </summary>
        /// <param name="entry">File entry to check (file or directory)</param>
        public static bool CanModify(this IFilePermissionChecker checker, IFileEntry entry)
            => checker.CanAccess(entry, FileEntryRights.Modify);

        /// <summary>
        /// Checks whether current user has permission to delete given file entry.
        /// </summary>
        /// <param name="entry">File entry to check (file or directory)</param>
        public static bool CanDelete(this IFilePermissionChecker checker, FileSystemInfo entry)
            => checker.CanAccess(entry, FileEntryRights.Delete);

        /// <summary>
        /// Checks whether current user has permission to delete given file entry.
        /// </summary>
        /// <param name="entry">File entry to check (file or directory)</param>
        public static bool CanDelete(this IFilePermissionChecker checker, IFileEntry entry)
            => checker.CanAccess(entry, FileEntryRights.Delete);

        private static FileSystemInfo ToFsInfo(IFileEntry entry)
        {
            Guard.NotNull(entry, nameof(entry));

            if (entry is LocalDirectory dir)
            {
                return dir.AsDirectoryInfo();
            }
            else if (entry is LocalFile file)
            {
                return file.AsFileInfo();
            }

            throw new InvalidOperationException($"For file permission checks given entry must be an existing local/physical file or directory. Entry: '{entry.SubPath}'");
        }
    }
}
