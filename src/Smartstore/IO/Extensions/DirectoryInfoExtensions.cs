namespace Smartstore
{
    public static class DirectoryInfoExtensions
    {
        /// <summary>
        /// Safe way to delete all directory content
        /// </summary>
        /// <param name="dir">Directory info object</param>
        /// <param name="ignoreFiles">Name of files to ignore (not to delete).</param>
        public static void ClearDirectory(this DirectoryInfo dir, params string[] ignoreFiles)
            => ClearDirectory(dir, false, TimeSpan.Zero, ignoreFiles);

        /// <summary>
        /// Safe way to delete all directory content
        /// </summary>
        /// <param name="dir">Directory info object</param>
        /// <param name="olderThan">Delete only files older than this TimeSpan</param>
        /// <param name="ignoreFiles">Name of files to ignore (not to delete).</param>
        public static void ClearDirectory(this DirectoryInfo dir, TimeSpan olderThan, params string[] ignoreFiles)
            => ClearDirectory(dir, false, olderThan, ignoreFiles);

        /// <summary>
        /// Safe way to delete all directory content
        /// </summary>
        /// <param name="dir">Directory info object</param>
        /// <param name="deleteIfEmpfy">Delete dir too if it doesn't contain any entries after deletion anymore</param>
        /// <param name="ignoreFiles">Name of files to ignore (not to delete).</param>
        public static void ClearDirectory(this DirectoryInfo dir, bool deleteIfEmpfy, params string[] ignoreFiles)
            => ClearDirectory(dir, deleteIfEmpfy, TimeSpan.Zero, ignoreFiles);

        /// <summary>
        /// Safe way to delete all directory content
        /// </summary>
        /// <param name="dir">Directory info object</param>
        /// <param name="deleteIfEmpfy">Delete dir too if it doesn't contain any entries after deletion anymore</param>
        /// <param name="olderThan">Delete only files older than this TimeSpan</param>
        /// <param name="ignoreFiles">Name of files to ignore (not to delete).</param>
        public static void ClearDirectory(this DirectoryInfo dir, bool deleteIfEmpfy, TimeSpan olderThan, params string[] ignoreFiles)
        {
            Guard.NotNull(dir, nameof(dir));

            if (!dir.Exists)
                return;

            if (olderThan == TimeSpan.Zero && (ignoreFiles == null || ignoreFiles.Length == 0))
            {
                FastClearDirectory(dir, deleteIfEmpfy);
                return;
            }

            var olderThanDate = DateTime.UtcNow.Subtract(olderThan);

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    foreach (var fsi in dir.EnumerateFileSystemInfos())
                    {
                        if (fsi is FileInfo file)
                        {
                            if (file.LastWriteTimeUtc >= olderThanDate)
                            {
                                continue;
                            }

                            if (ignoreFiles.Any(x => x.EqualsNoCase(file.Name)))
                            {
                                continue;
                            }

                            if (file.IsReadOnly)
                            {
                                file.IsReadOnly = false;
                            }

                            file.WaitForUnlockAndExecute(x => x.Delete());
                        }
                        else if (fsi is DirectoryInfo subDir)
                        {
                            ClearDirectory(subDir, true, olderThan, ignoreFiles);
                        }

                    }

                    break;
                }
                catch (Exception ex)
                {
                    ex.Dump();
                }
            }

            if (deleteIfEmpfy)
            {
                try
                {
                    if (!dir.EnumerateFileSystemInfos().Any())
                    {
                        dir.Delete(true);
                    }
                }
                catch (Exception ex)
                {
                    ex.Dump();
                }
            }
        }

        private static void FastClearDirectory(DirectoryInfo dir, bool deleteIfEmpfy)
        {
            try
            {
                if (deleteIfEmpfy)
                {
                    dir.Delete(true);
                }
                else
                {
                    foreach (var fsi in dir.EnumerateFileSystemInfos())
                    {
                        if (fsi is FileInfo file)
                        {
                            if (file.IsReadOnly)
                            {
                                file.IsReadOnly = false;
                            }

                            file.WaitForUnlockAndExecute(x => x.Delete());
                        }
                        else if (fsi is DirectoryInfo subDir)
                        {
                            subDir.Delete(true);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                ex.Dump();
            }
        }
    }
}
