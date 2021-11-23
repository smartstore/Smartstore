using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using Smartstore.IO;
using Smartstore.Utilities;

namespace Smartstore
{
    public static partial class IFileSystemExtensions
    {
        #region Enumerate

        /// <summary>
        /// Enumerates the content (files and directories) in a given directory.
        /// </summary>
        /// <param name="subpath">The path of the directory to enumerate, or <c>null</c> to enumerate the root of the file store.</param>
        /// <param name="pattern">The file pattern to match.</param>
        /// <param name="deep">A flag to indicate whether to get the contents from just the top directory or from all sub-directories as well.</param>
        /// <returns>The list of files and directories in the given directory.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if the specified directory does not exist.</exception>
        /// <remarks>
        /// Results are grouped by entry type, where directories are followed by files.
        /// </remarks>
        public static IEnumerable<IFileEntry> EnumerateEntries(this IFileSystem fs, string subpath = null, string pattern = "*", bool deep = false)
        {
            Guard.NotNull(fs, nameof(fs));
            return fs.GetDirectory(subpath).EnumerateEntries(pattern, deep);
        }

        /// <summary>
        /// Enumerates the content (files and directories) in a given directory.
        /// </summary>
        /// <param name="subpath">The path of the directory to enumerate, or <c>null</c> to enumerate the root of the file store.</param>
        /// <param name="pattern">The file pattern to match.</param>
        /// <param name="deep">A flag to indicate whether to get the contents from just the top directory or from all sub-directories as well.</param>
        /// <returns>The list of files and directories in the given directory.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if the specified directory does not exist.</exception>
        /// <remarks>
        /// Results are grouped by entry type, where directories are followed by files.
        /// </remarks>
        public static IAsyncEnumerable<IFileEntry> EnumerateEntriesAsync(this IFileSystem fs, string subpath = null, string pattern = "*", bool deep = false)
        {
            Guard.NotNull(fs, nameof(fs));
            return fs.GetDirectory(subpath).EnumerateEntriesAsync(pattern, deep);
        }

        /// <summary>
        /// Enumerates the files in a given directory.
        /// </summary>
        /// <param name="subpath">The path of the directory to enumerate, or <c>null</c> to enumerate the root of the file store.</param>
        /// <param name="pattern">The file pattern to match.</param>
        /// <param name="deep">A flag to indicate whether to get the files from just the top directory or from all sub-directories as well.</param>
        /// <returns>The list of files in the given directory.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if the specified directory does not exist.</exception>
        public static IEnumerable<IFile> EnumerateFiles(this IFileSystem fs, string subpath = null, string pattern = "*", bool deep = false)
        {
            Guard.NotNull(fs, nameof(fs));
            return fs.GetDirectory(subpath).EnumerateFiles(pattern, deep);
        }

        /// <summary>
        /// Enumerates the files in a given directory.
        /// </summary>
        /// <param name="subpath">The path of the directory to enumerate, or <c>null</c> to enumerate the root of the file store.</param>
        /// <param name="pattern">The file pattern to match.</param>
        /// <param name="deep">A flag to indicate whether to get the files from just the top directory or from all sub-directories as well.</param>
        /// <returns>The list of files in the given directory.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if the specified directory does not exist.</exception>
        public static IAsyncEnumerable<IFile> EnumerateFilesAsync(this IFileSystem fs, string subpath = null, string pattern = "*", bool deep = false)
        {
            Guard.NotNull(fs, nameof(fs));
            return fs.GetDirectory(subpath).EnumerateFilesAsync(pattern, deep);
        }

        /// <summary>
        /// Enumerates the directories in a given directory.
        /// </summary>
        /// <param name="subpath">The path of the directory to enumerate, or <c>null</c> to enumerate the root of the file store.</param>
        /// <param name="pattern">The directory pattern to match.</param>
        /// <param name="deep">A flag to indicate whether to get the directories from just the top directory or from all sub-directories as well.</param>
        /// <returns>The list of directories in the given directory.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if the specified directory does not exist.</exception>
        public static IEnumerable<IDirectory> EnumerateDirectories(this IFileSystem fs, string subpath = null, string pattern = "*", bool deep = false)
        {
            Guard.NotNull(fs, nameof(fs));
            return fs.GetDirectory(subpath).EnumerateDirectories(pattern, deep);
        }

        /// <summary>
        /// Enumerates the directories in a given directory.
        /// </summary>
        /// <param name="subpath">The path of the directory to enumerate, or <c>null</c> to enumerate the root of the file store.</param>
        /// <param name="pattern">The directory pattern to match.</param>
        /// <param name="deep">A flag to indicate whether to get the directories from just the top directory or from all sub-directories as well.</param>
        /// <returns>The list of directories in the given directory.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if the specified directory does not exist.</exception>
        public static IAsyncEnumerable<IDirectory> EnumerateDirectoriesAsync(this IFileSystem fs, string subpath = null, string pattern = "*", bool deep = false)
        {
            Guard.NotNull(fs, nameof(fs));
            return fs.GetDirectory(subpath).EnumerateDirectoriesAsync(pattern, deep);
        }

        #endregion

        #region Create / Delete

        /// <summary>
        /// Creates all directories and subdirectories in the specified target unless they already exist.
        /// </summary>
        /// <param name="subpath">The path of the directory to be created.</param>
        /// <returns><c>true</c> if the directory was created; <c>false</c> if the directory already existed.</returns>
        /// <exception cref="FileSystemException">Thrown if the specified path exists but is not a directory.</exception>
        public static bool TryCreateDirectory(this IFileSystem fs, string subpath)
        {
            Guard.NotNull(fs, nameof(fs));

            if (fs.FileExists(subpath))
            {
                throw new FileSystemException($"Cannot create directory because the path '{subpath}' already exists and is a file.");
            }

            var dir = fs.GetDirectory(subpath);
            if (dir.Exists)
            {
                return false;
            }

            try
            {
                dir.Create();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates all directories and subdirectories in the specified target unless they already exist.
        /// </summary>
        /// <param name="subpath">The path of the directory to be created.</param>
        /// <returns><c>true</c> if the directory was created; <c>false</c> if the directory already existed.</returns>
        /// <exception cref="FileSystemException">Thrown if the specified path exists but is not a directory.</exception>
        public static async Task<bool> TryCreateDirectoryAsync(this IFileSystem fs, string subpath)
        {
            Guard.NotNull(fs, nameof(fs));

            if (await fs.FileExistsAsync(subpath))
            {
                throw new FileSystemException($"Cannot create directory because the path '{subpath}' already exists and is a file.");
            }

            var dir = await fs.GetDirectoryAsync(subpath);
            if (dir.Exists)
            {
                return false;
            }

            try
            {
                await dir.CreateAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Deletes a directory recursively if it exists.
        /// </summary>
        /// <param name="subpath">The path of the directory to be deleted.</param>
        /// <returns><c>true</c> if the directory was deleted; <c>false</c> if the directory did not exist.</returns>
        public static bool TryDeleteDirectory(this IFileSystem fs, string subpath)
            => TryDeleteDirectory(fs, fs.GetDirectory(subpath));

        /// <summary>
        /// Deletes a directory recursively if it exists.
        /// </summary>
        /// <param name="directory">The directory to delete.</param>
        /// <returns><c>true</c> if the directory was deleted; <c>false</c> if the directory did not exist.</returns>
        public static bool TryDeleteDirectory(this IFileSystem fs, IDirectory directory)
        {
            Guard.NotNull(fs, nameof(fs));
            Guard.NotNull(directory, nameof(directory));

            if (!directory.Exists)
            {
                return false;
            }

            try
            {
                directory.Delete();

                // Wait for deletion to complete
                var attempts = 0;
                while (directory.Exists)
                {
                    attempts += 1;
                    if (attempts > 10) return true;
                    Thread.Sleep(100);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Deletes a directory recursively if it exists.
        /// </summary>
        /// <param name="subpath">The path of the directory to be deleted.</param>
        /// <returns><c>true</c> if the directory was deleted; <c>false</c> if the directory did not exist.</returns>
        public static async Task<bool> TryDeleteDirectoryAsync(this IFileSystem fs, string subpath)
            => await TryDeleteDirectoryAsync(fs, await fs.GetDirectoryAsync(subpath));

        /// <summary>
        /// Deletes a directory recursively if it exists.
        /// </summary>
        /// <param name="directory">The directory to delete.</param>
        /// <returns><c>true</c> if the directory was deleted; <c>false</c> if the directory did not exist.</returns>
        public static async Task<bool> TryDeleteDirectoryAsync(this IFileSystem fs, IDirectory directory)
        {
            Guard.NotNull(fs, nameof(fs));
            Guard.NotNull(directory, nameof(directory));

            if (!directory.Exists)
            {
                return false;
            }

            try
            {
                await directory.DeleteAsync();

                // Wait for deletion to complete
                var attempts = 0;
                while (directory.Exists)
                {
                    attempts += 1;
                    if (attempts > 10) return true;
                    await Task.Delay(100);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Safe way to delete all directory content
        /// </summary>
        /// <param name="subpath">Directory path</param>
        /// <param name="deleteIfEmpfy">Delete dir too if it doesn't contain any entries after deletion anymore</param>
        /// <param name="olderThan">Delete only files older than this TimeSpan</param>
        /// <param name="ignoreFiles">Name of files to ignore (not to delete).</param>
        public static void ClearDirectory(this IFileSystem fs,
            IDirectory directory,
            bool deleteIfEmpfy,
            TimeSpan olderThan,
            params string[] ignoreFiles)
        {
            Guard.NotNull(directory, nameof(directory));

            if (!directory.Exists)
                return;

            var olderThanDate = DateTime.UtcNow.Subtract(olderThan);

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    foreach (var entry in fs.EnumerateEntries(directory.SubPath))
                    {
                        if (entry is IFile file)
                        {
                            if (file.LastModified >= olderThanDate)
                                continue;

                            if (ignoreFiles.Any(x => x.EqualsNoCase(file.Name)))
                                continue;

                            if (file is LocalFile fi && fi.AsFileInfo().IsReadOnly)
                            {
                                fi.AsFileInfo().IsReadOnly = false;
                            }

                            fs.TryDeleteFile(file.SubPath);
                        }
                        else if (entry is IDirectory subDir)
                        {
                            ClearDirectory(fs, subDir, true, olderThan, ignoreFiles);
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
                    if (!fs.EnumerateEntries(directory.SubPath).Any())
                    {
                        fs.TryDeleteDirectory(directory.SubPath);
                    }
                }
                catch (Exception ex)
                {
                    ex.Dump();
                }
            }
        }

        #endregion

        #region Move / Copy

        /// <summary>
        /// Copies a directory and all its content to another directory.
        /// </summary>
        /// <param name="sourcePath">The relative path of source directory</param>
        /// <param name="destinationPath">The relative path of destination directory</param>
        /// <param name="ignorePatterns">Path patterns to exclude from copy operation. Supports * and ? wildcards.</param>
        /// <param name="overwrite">Whether to overwrite existing files</param>
        /// <returns>The destination directory.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if source directory does not exist.</exception>
        public static IDirectory CopyDirectory(this IFileSystem fs, string sourcePath, string destinationPath, bool overwrite = true, string[] ignorePatterns = null)
        {
            Guard.NotNull(fs, nameof(fs));
            Guard.NotNull(sourcePath, nameof(sourcePath));
            Guard.NotNull(destinationPath, nameof(destinationPath));

            var destination = fs.GetDirectory(destinationPath);
            CopyDirectory(fs, fs.GetDirectory(sourcePath), destination, overwrite, ignorePatterns);
            return destination;
        }

        /// <summary>
        /// Copies a directory and all its content to another directory.
        /// </summary>
        /// <param name="source">The source directory</param>
        /// <param name="destination">The destination directory</param>
        /// <param name="ignorePatterns">Path patterns to exclude from copy operation. Supports * and ? wildcards.</param>
        /// <param name="overwrite">Whether to overwrite existing files</param>
        /// <returns>The destination directory.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if source directory does not exist.</exception>
        public static void CopyDirectory(this IFileSystem fs, IDirectory source, IDirectory destination, bool overwrite = true, string[] ignorePatterns = null)
        {
            Guard.NotNull(fs, nameof(fs));
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(destination, nameof(destination));

            if (!source.Exists)
            {
                throw new DirectoryNotFoundException("Directory " + source.SubPath + "does not exist.");
            }

            if (destination.SubPath.EnsureEndsWith('/').StartsWith(source.SubPath.EnsureEndsWith('/'), StringComparison.CurrentCultureIgnoreCase))
            {
                throw new FileSystemException($"Cannot copy a directory '{source.SubPath}' into itself.");
            }

            InternalCopyDirectory(
                fs,
                source,
                destination,
                ignorePatterns?.Select(x => new Wildcard(x))?.ToArray() ?? Array.Empty<Wildcard>(),
                overwrite);
        }

        /// <summary>
        /// Copies a directory and all its content to another directory.
        /// </summary>
        /// <param name="sourcePath">The relative path of source directory</param>
        /// <param name="destinationPath">The relative path of destination directory</param>
        /// <param name="ignorePatterns">Path patterns to exclude from copy operation. Supports * and ? wildcards.</param>
        /// <param name="overwrite">Whether to overwrite existing files</param>
        /// <returns>The destination directory.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if source directory does not exist.</exception>
        public static async Task<IDirectory> CopyDirectoryAsync(this IFileSystem fs, string sourcePath, string destinationPath, bool overwrite = true, string[] ignorePatterns = null)
        {
            Guard.NotEmpty(sourcePath, nameof(sourcePath));
            Guard.NotEmpty(destinationPath, nameof(destinationPath));

            var destination = fs.GetDirectory(destinationPath);
            await CopyDirectoryAsync(fs, fs.GetDirectory(sourcePath), destination, overwrite, ignorePatterns);
            return destination;
        }

        /// <summary>
        /// Copies a directory and all its content to another directory.
        /// </summary>
        /// <param name="source">The source directory</param>
        /// <param name="destination">The destination directory</param>
        /// <param name="ignorePatterns">Path patterns to exclude from copy operation. Supports * and ? wildcards.</param>
        /// <param name="overwrite">Whether to overwrite existing files</param>
        /// <returns>The destination directory.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if source directory does not exist.</exception>
        public static async Task CopyDirectoryAsync(this IFileSystem fs, IDirectory source, IDirectory destination, bool overwrite = true, string[] ignorePatterns = null)
        {
            Guard.NotNull(fs, nameof(fs));
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(destination, nameof(destination));

            if (!source.Exists)
            {
                throw new DirectoryNotFoundException("Directory " + source.SubPath + "does not exist.");
            }

            if (destination.SubPath.EnsureEndsWith('/').StartsWith(source.SubPath.EnsureEndsWith('/'), StringComparison.CurrentCultureIgnoreCase))
            {
                throw new FileSystemException($"Cannot copy a directory '{source.SubPath}' into itself.");
            }

            await InternalCopyDirectoryAsync(
                fs,
                source,
                destination,
                ignorePatterns?.Select(x => new Wildcard(x))?.ToArray() ?? Array.Empty<Wildcard>(),
                overwrite);
        }

        #endregion

        #region Size / Count

        /// <summary>
        /// Sums the total length of all files contained within a given directory.
        /// </summary>
        /// <param name="subpath">The relative path to the directory.</param>
        /// <param name="pattern">The file pattern to match</param>
        /// <param name="predicate">Optional. Files not matching the predicate are ignored.</param>
        /// <param name="deep">Whether to sum up length in all subdirectories also.</param>
        /// <returns>Total length of all files.</returns>
        public static long GetDirectorySize(this IFileSystem fs, string subpath, string pattern = "*", Func<string, bool> predicate = null, bool deep = true)
        {
            Guard.NotNull(fs, nameof(fs));

            var dir = fs.GetDirectory(subpath);

            if (predicate == null)
            {
                return dir.GetDirectorySize(pattern, deep);
            }
            else
            {
                return dir.EnumerateFiles(pattern, deep)
                    .AsParallel()
                    .Where(x => predicate(x.SubPath))
                    .Sum(x => x.Length);
            }
        }

        /// <summary>
        /// Sums the total length of all files contained within a given directory.
        /// </summary>
        /// <param name="subpath">The relative path to the directory.</param>
        /// <param name="pattern">The file pattern to match</param>
        /// <param name="predicate">Optional. Files not matching the predicate are ignored.</param>
        /// <param name="deep">Whether to sum up length in all subdirectories also.</param>
        /// <returns>Total length of all files.</returns>
        public static async Task<long> GetDirectorySizeAsync(this IFileSystem fs, string subpath, string pattern = "*", Func<string, bool> predicate = null, bool deep = true)
        {
            Guard.NotNull(fs, nameof(fs));

            var dir = await fs.GetDirectoryAsync(subpath);

            if (predicate == null)
            {
                return await dir.GetDirectorySizeAsync(pattern, deep);
            }
            else
            {
                return await dir.EnumerateFilesAsync(pattern, deep)
                    .Where(x => predicate(x.SubPath))
                    .SumAsync(x => x.Length);
            }
        }

        /// <summary>
        /// Retrieves the count of files within a path.
        /// </summary>
        /// <param name="subpath">The relative path to the directory in which to retrieve file count.</param>
        /// <param name="pattern">The file pattern to match</param>
        /// <param name="predicate">Optional. Files not matching the predicate are ignored.</param>
        /// <param name="deep">Whether to count files in all subdirectories also</param>
        /// <returns>Total count of files.</returns>
        public static long CountFiles(this IFileSystem fs, string subpath, string pattern = "*", Func<string, bool> predicate = null, bool deep = true)
        {
            Guard.NotNull(fs, nameof(fs));

            var dir = fs.GetDirectory(subpath);

            if (predicate == null)
            {
                return dir.CountFiles(pattern, deep);
            }
            else
            {
                return dir.EnumerateFiles(pattern, deep)
                    .AsParallel()
                    .Where(x => predicate(x.SubPath))
                    .Count();
            }
        }

        /// <summary>
        /// Retrieves the count of files within a path.
        /// </summary>
        /// <param name="subpath">The relative path to the directory in which to retrieve file count.</param>
        /// <param name="pattern">The file pattern to match</param>
        /// <param name="predicate">Optional. Files not matching the predicate are ignored.</param>
        /// <param name="deep">Whether to count files in all subdirectories also</param>
        /// <returns>Total count of files.</returns>
        public static async Task<long> CountFilesAsync(this IFileSystem fs, string subpath, string pattern = "*", Func<string, bool> predicate = null, bool deep = true)
        {
            Guard.NotNull(fs, nameof(fs));

            var dir = await fs.GetDirectoryAsync(subpath);

            if (predicate == null)
            {
                return await dir.CountFilesAsync(pattern, deep);
            }
            else
            {
                return await dir.EnumerateFilesAsync(pattern, deep)
                    .Where(x => predicate(x.SubPath))
                    .CountAsync();
            }
        }

        #endregion

        #region Utils

        /// <summary>
        /// Creates a unique (non-existing) directory name within a given path by appending an index.
        /// </summary>
        /// <param name="subpath">Path to a directory</param>
        /// <param name="defaultName">Default name for directory. <c>null</c> to use a guid.</param>
        /// <param name="maxAttempts">Maximum number of attempts/iterations</param>
        /// <returns>Unique directory name</returns>
        public static string CreateUniqueDirectoryName(this IFileSystem fs, string subpath, string defaultName, int maxAttempts = 999999)
        {
            Guard.NotNull(fs, nameof(fs));
            Guard.NotNull(subpath, nameof(subpath));

            if (defaultName.IsEmpty())
            {
                defaultName = Guid.NewGuid().ToString();
            }

            if (subpath.IsEmpty() || !fs.DirectoryExists(subpath))
            {
                return defaultName;
            }

            var newName = defaultName;

            for (int i = 1; i < maxAttempts; ++i)
            {
                if (!fs.DirectoryExists(fs.PathCombine(subpath, newName)))
                {
                    return newName;
                }

                newName = defaultName + i.ToString();
            }

            return null;
        }

        /// <summary>
        /// Gets a directory hasher utility instance used to generate a unique hash code for directory content.
        /// The default target directory for generated hash codes is 'App_Data/Tenants/[CurrentTenant]/Hash'.
        /// </summary>
        /// <param name="subpath">The relative path to the directory to generate hash code for.</param>
        /// <param name="searchPattern">The pattern for file to include in hash code generation.</param>
        /// <param name="deep">Whether to include files in subfolders also.</param>
        /// <returns>The hasher utility.</returns>
        /// <exception cref="FileSystemException">Throws if <paramref name="subpath"/> is a file.</exception>
        public static DirectoryHasher GetDirectoryHasher(this IFileSystem fs, string subpath, string searchPattern = "*", bool deep = false)
        {
            Guard.NotNull(fs, nameof(fs));
            Guard.NotNull(subpath, nameof(subpath));

            return GetDirectoryHasher(fs, subpath, null, searchPattern, deep);
        }

        /// <summary>
        /// Gets a directory hasher utility instance used to generate a unique hash code for directory content.
        /// </summary>
        /// <param name="subpath">The relative path to the directory to generate hash code for.</param>
        /// <param name="storageDir">The directory to save the generated hash code to.</param>
        /// <param name="searchPattern">The pattern for file to include in hash code generation.</param>
        /// <param name="deep">Whether to include files in subfolders also.</param>
        /// <returns>The hasher utility.</returns>
        /// <exception cref="FileSystemException">Throws if <paramref name="subpath"/> is a file.</exception>
        public static DirectoryHasher GetDirectoryHasher(this IFileSystem fs, string subpath, IDirectory storageDir, string searchPattern = "*", bool deep = false)
        {
            Guard.NotNull(fs, nameof(fs));
            Guard.NotEmpty(subpath, nameof(subpath));

            var entry = fs.GetDirectory(subpath);
            if (entry is not IDirectory dir)
            {
                throw new FileSystemException($"Path '{subpath}' must point to a directory, not to a file.");
            }

            return new DirectoryHasher(dir, storageDir, searchPattern, deep);
        }

        private static void InternalCopyDirectory(IFileSystem fs,
            IDirectory source,
            IDirectory destination,
            Wildcard[] ignores,
            bool overwrite)
        {
            if (!destination.Exists)
            {
                destination.Create();
            }

            foreach (var entry in fs.EnumerateEntries(source.SubPath))
            {
                if (ignores.Any(w => w.IsMatch(entry.SubPath)))
                {
                    continue;
                }

                var newPath = fs.PathCombine(destination.SubPath, entry.Name);

                if (entry is IDirectory dir)
                {
                    InternalCopyDirectory(fs, dir, fs.GetDirectory(newPath), ignores, overwrite);
                }
                else if (entry is IFile file)
                {
                    file.CopyTo(newPath, overwrite);
                }
            }
        }

        private static async Task InternalCopyDirectoryAsync(IFileSystem fs,
            IDirectory source,
            IDirectory destination,
            Wildcard[] ignores,
            bool overwrite)
        {
            if (!destination.Exists)
            {
                await destination.CreateAsync();
            }

            await foreach (var entry in fs.EnumerateEntriesAsync(source.SubPath))
            {
                if (ignores.Any(w => w.IsMatch(entry.SubPath)))
                {
                    continue;
                }

                var newPath = fs.PathCombine(destination.SubPath, entry.Name);

                if (entry is IDirectory dir)
                {
                    await InternalCopyDirectoryAsync(fs, dir, await fs.GetDirectoryAsync(newPath), ignores, overwrite);
                }
                else if (entry is IFile file)
                {
                    await file.CopyToAsync(newPath, overwrite);
                }
            }
        }

        #endregion
    }
}
