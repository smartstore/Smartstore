using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Smartstore.IO;

namespace Smartstore
{
    public static class IFileSystemExtensions
    {
        /// <summary>
        /// Retrieves a directory or a file.
        /// </summary>
        /// <param name="subpath">The relative path to the file or directory within the storage.</param>
        /// <returns>
        ///     A <see cref="IFileEntry"/> object representing either a file or a directory, 
        ///     or <see cref="NotFoundFile"/> if <paramref name="subpath"/> has invalid chars, is rooted or navigates above root.
        /// </returns>
        public static IFileEntry GetEntry(this IFileSystem fs, string subpath)
        {
            Guard.NotEmpty(subpath, nameof(subpath));

            var entry = fs.GetDirectory(subpath);
            if (entry.Exists || entry is NotFoundDirectory)
            {
                return entry;
            }

            return fs.GetFile(subpath);
        }

        /// <summary>
        /// Retrieves a directory or a file.
        /// </summary>
        /// <param name="subpath">The relative path to the file or directory within the storage.</param>
        /// <returns>
        ///     A <see cref="IFileEntry"/> object representing either a file or a directory, 
        ///     or <see cref="NotFoundFile"/> if <paramref name="subpath"/> has invalid chars, is rooted or navigates above root.
        /// </returns>
        public static async Task<IFileEntry> GetEntryAsync(this IFileSystem fs, string subpath)
        {
            Guard.NotEmpty(subpath, nameof(subpath));

            var entry = await fs.GetDirectoryAsync(subpath);
            if (entry.Exists || entry is NotFoundDirectory)
            {
                return entry;
            }

            return await fs.GetFileAsync(subpath);
        }

        #region Read

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadAllText(this IFileSystem fs, string subpath, Encoding encoding = null)
        {
            return fs.GetFile(subpath).ReadAllText(encoding);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<string> ReadAllTextAsync(this IFileSystem fs, string subpath, Encoding encoding = null)
        {
            return await (await fs.GetFileAsync(subpath)).ReadAllTextAsync(encoding);
        }

        public static string ReadAllText(this IFile file, Encoding encoding = null)
        {
            Guard.NotNull(file, nameof(file));

            if (!file.Exists)
            {
                return null;
            }

            using (var stream = file.OpenRead())
            using (var streamReader = new StreamReader(stream, encoding ?? Encoding.UTF8))
            {
                return streamReader.ReadToEnd();
            }
        }

        public static async Task<string> ReadAllTextAsync(this IFile file, Encoding encoding = null)
        {
            Guard.NotNull(file, nameof(file));

            if (!file.Exists)
            {
                return null;
            }

            using (var stream = file.OpenRead())
            using (var streamReader = new StreamReader(stream, encoding ?? Encoding.UTF8))
            {
                return await streamReader.ReadToEndAsync();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ReadAllBytes(this IFileSystem fs, string subpath)
        {
            return fs.GetFile(subpath).ReadAllBytes();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<byte[]> ReadAllBytesAsync(this IFileSystem fs, string subpath)
        {
            return await (await fs.GetFileAsync(subpath)).ReadAllBytesAsync();
        }

        public static byte[] ReadAllBytes(this IFile file)
        {
            Guard.NotNull(file, nameof(file));

            if (!file.Exists)
            {
                return null;
            }

            using (var stream = file.OpenRead())
            {
                return stream.ToByteArray();
            }
        }

        public static async Task<byte[]> ReadAllBytesAsync(this IFile file)
        {
            Guard.NotNull(file, nameof(file));

            if (!file.Exists)
            {
                return null;
            }

            using (var stream = file.OpenRead())
            {
                return await stream.ToByteArrayAsync();
            }
        }

        #endregion

        #region Write

        public static void WriteAllText(this IFileSystem fs, string subpath, string contents, Encoding encoding = null)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            Guard.NotEmpty(contents, nameof(contents));

            var file = fs.GetFile(subpath);

            using (var stream = file.OpenWrite())
            using (var streamWriter = new StreamWriter(stream, encoding ?? new UTF8Encoding(false, true)))
            {
                streamWriter.Write(contents);
            }
        }

        public static async Task WriteAllTextAsync(this IFileSystem fs, string subpath, string contents, Encoding encoding = null)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            Guard.NotEmpty(contents, nameof(contents));

            var file = await fs.GetFileAsync(subpath);

            using (var stream = file.OpenWrite())
            using (var streamWriter = new StreamWriter(stream, encoding ?? new UTF8Encoding(false, true)))
            {
                await streamWriter.WriteAsync(contents);
            }
        }

        public static void WriteAllBytes(this IFileSystem fs, string subpath, byte[] contents)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            Guard.NotEmpty(contents, nameof(contents));

            var file = fs.GetFile(subpath);

            using (var stream = file.OpenWrite())
            {
                stream.Write(contents, 0, contents.Length);
            }
        }

        public static async Task WriteAllBytesAsync(this IFileSystem fs, string subpath, byte[] contents)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            Guard.NotEmpty(contents, nameof(contents));

            var file = await fs.GetFileAsync(subpath);

            using (var stream = file.OpenWrite())
            {
                await stream.WriteAsync(contents.AsMemory(0, contents.Length));
            }
        }

        /// <summary>
        /// Saves a stream in the storage. If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="subpath">The relative path to the file to be saved.</param>
        /// <param name="inStream">The stream to be saved.</param>
        /// <exception cref="FileSystemException">If the stream can't be saved due to access permissions.</exception>
        public static void SaveStream(this IFileSystem fs, string subpath, Stream inStream, bool leaveOpen = true)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            Guard.NotNull(inStream, nameof(inStream));

            var file = fs.GetFile(subpath);

            using (var stream = file.OpenWrite())
            {
                inStream.CopyTo(stream);
            }

            if (!leaveOpen)
            {
                inStream.Dispose();
            }
        }

        /// <summary>
        /// Saves a stream in the storage. If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="subpath">The relative path to the file to be saved.</param>
        /// <param name="inStream">The stream to be saved.</param>
        /// <exception cref="FileSystemException">If the stream can't be saved due to access permissions.</exception>
        public static async Task SaveStreamAsync(this IFileSystem fs, string subpath, Stream inStream, bool leaveOpen = true)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            Guard.NotNull(inStream, nameof(inStream));

            var file = await fs.GetFileAsync(subpath);

            using (var stream = file.OpenWrite())
            {
                await inStream.CopyToAsync(stream);
            }

            if (!leaveOpen)
            {
                inStream.Dispose();
            }
        }

        #endregion

        #region Copy / Move / Delete

        /// <summary>
        /// Renames/moves a file or a directory.
        /// </summary>
        /// <param name="subpath">The relative path to the file or the directory to be renamed/moved.</param>
        /// <param name="newPath">The new path after entry was moved/renamed.</param>
        /// <exception cref="FileSystemException">Thrown if source does not exist or if <paramref name="newPath"/> already exists.</exception>
        public static void MoveEntry(this IFileSystem fs, string subpath, string newPath)
        {
            fs.MoveEntry(fs.GetEntry(subpath), newPath);
        }

        /// <summary>
        /// Renames/moves a file or a directory.
        /// </summary>
        /// <param name="subpath">The relative path to the file or the directory to be renamed/moved.</param>
        /// <param name="newPath">The new path after entry was moved/renamed.</param>
        /// <exception cref="FileSystemException">Thrown if source does not exist or if <paramref name="newPath"/> already exists.</exception>
        public static async Task MoveEntryAsync(this IFileSystem fs, string subpath, string newPath)
        {
            await fs.MoveEntryAsync(await fs.GetEntryAsync(subpath), newPath);
        }

        public static bool CopyFileAndDeleteSource(this IFileSystem fs, string subpath, string newPath, bool overwrite = false)
        {
            try
            {
                fs.CopyFile(subpath, newPath, overwrite);
                return fs.TryDeleteFile(subpath);
            }
            catch (Exception exc)
            {
                exc.Dump();
                return false;
            }
        }

        public static async Task<bool> CopyFileAndDeleteSourceAsync(this IFileSystem fs, string subpath, string newPath, bool overwrite = false)
        {
            var result = false;
            try
            {
                await fs.CopyFileAsync(subpath, newPath, overwrite);
                result = await fs.TryDeleteFileAsync(subpath);
            }
            catch (Exception exc)
            {
                exc.Dump();
            }

            return result;
        }

        /// <summary>
        /// Copies a directory and all its content to another directory.
        /// </summary>
        /// <param name="subpath">The relative path of source directory</param>
        /// <param name="destinationPath">The relative Path of destination directory</param>
        /// <param name="overwrite">Whether to overwrite existing files</param>
        /// <returns>The destination directory.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if source directory does not exist.</exception>
        /// <exception cref="FileSystemException">Thrown if destination directory exists and <paramref name="overwrite"/> is <c>false</c>.</exception>
        public static IDirectory CopyDirectory(this IFileSystem fs, string subpath, string destinationPath, bool overwrite = true)
        {
            var sourceDirectory = fs.GetDirectory(subpath);
            if (!sourceDirectory.Exists)
            {
                throw new DirectoryNotFoundException("Directory " + subpath + "does not exist.");
            }

            var targetPath = fs.PathCombine(destinationPath, sourceDirectory.Name);
            var destDirectory = fs.GetDirectory(targetPath);
            if (!overwrite && destDirectory.Exists)
            {
                throw new FileSystemException("Directory " + destinationPath + " already exists.");
            }

            if (destDirectory.SubPath.EnsureEndsWith('/').StartsWith(sourceDirectory.SubPath.EnsureEndsWith('/'), StringComparison.CurrentCultureIgnoreCase))
            {
                throw new FileSystemException($"Cannot copy a directory '{subpath}' into itself.");
            }

            CopyDirectoryInternal(fs, subpath, destinationPath, overwrite);
            return fs.GetDirectory(destinationPath);
        }

        /// <summary>
        /// Copies a directory and all its content to another directory.
        /// </summary>
        /// <param name="subpath">The relative path of source directory</param>
        /// <param name="destinationPath">The relative Path of destination directory</param>
        /// <param name="overwrite">Whether to overwrite existing files</param>
        /// <returns>The destination directory.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if source directory does not exist.</exception>
        /// <exception cref="FileSystemException">Thrown if destination directory exists and <paramref name="overwrite"/> is <c>false</c>.</exception>
        public static async Task<IDirectory> CopyDirectoryAsync(this IFileSystem fs, string subpath, string destinationPath, bool overwrite = true)
        {
            var sourceDirectory = await fs.GetDirectoryAsync(subpath);
            if (!sourceDirectory.Exists)
            {
                throw new DirectoryNotFoundException("Directory " + subpath + "does not exist.");
            }

            var targetPath = fs.PathCombine(destinationPath, sourceDirectory.Name);
            var destDirectory = await fs.GetDirectoryAsync(targetPath);
            if (!overwrite && destDirectory.Exists)
            {
                throw new FileSystemException("Directory " + destinationPath + " already exists.");
            }

            if (destDirectory.SubPath.EnsureEndsWith('/').StartsWith(sourceDirectory.SubPath.EnsureEndsWith('/'), StringComparison.CurrentCultureIgnoreCase))
            {
                throw new FileSystemException($"Cannot copy a directory '{subpath}' into itself.");
            }

            await CopyDirectoryInternalAsync(fs, subpath, destinationPath, overwrite);
            return await fs.GetDirectoryAsync(destinationPath);
        }

        private static void CopyDirectoryInternal(IFileSystem fs, string subpath, string destinationPath, bool overwrite = true)
        {
            fs.TryCreateDirectory(destinationPath);

            foreach (var entry in fs.EnumerateFiles(subpath))
            {
                var newPath = fs.PathCombine(destinationPath, entry.Name);
                fs.CopyFile(entry.SubPath, newPath, overwrite);
            }

            foreach (var entry in fs.EnumerateDirectories(subpath))
            {
                var newPath = fs.PathCombine(destinationPath, entry.Name);
                CopyDirectoryInternal(fs, entry.SubPath, newPath, overwrite);
            }
        }

        private static async Task CopyDirectoryInternalAsync(IFileSystem fs, string subpath, string destinationPath, bool overwrite = true)
        {
            await fs.TryCreateDirectoryAsync(destinationPath);

            await foreach (var entry in fs.EnumerateFilesAsync(subpath))
            {
                var newPath = fs.PathCombine(destinationPath, entry.Name);
                await fs.CopyFileAsync(entry.SubPath, newPath, overwrite);
            }

            await foreach (var entry in fs.EnumerateDirectoriesAsync(subpath))
            {
                var newPath = fs.PathCombine(destinationPath, entry.Name);
                await CopyDirectoryInternalAsync(fs, entry.SubPath, newPath, overwrite);
            }
        }

        /// <summary>
        /// Safe way to delete all directory content
        /// </summary>
        /// <param name="subpath">Directory path</param>
        /// <param name="deleteIfEmpfy">Delete dir too if it doesn't contain any entries after deletion anymore</param>
        /// <param name="olderThan">Delete only files older than this TimeSpan</param>
        /// <param name="exceptFileNames">Name of files not to be deleted</param>
        public static void ClearDirectory(this IFileSystem fs,
            IDirectory directory,
            bool deleteIfEmpfy,
            TimeSpan olderThan,
            params string[] exceptFileNames)
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

                            if (exceptFileNames.Any(x => x.EqualsNoCase(file.Name)))
                                continue;

                            if (file is LocalFile fi && fi.AsFileInfo().IsReadOnly)
                            {
                                fi.AsFileInfo().IsReadOnly = false;
                            }

                            fs.TryDeleteFile(file.SubPath);
                        }
                        else if (entry is IDirectory subDir)
                        {
                            ClearDirectory(fs, subDir, true, olderThan, exceptFileNames);
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

        #region Misc

        /// <summary>
        /// Attaches the given <paramref name="source"/> entry to <paramref name="fs"/> by checking
        /// whether source's origin file system root is underneath <paramref name="fs"/> and prepending
        /// the path difference to source's subpath.
        /// <para>
        /// Use this method if you - for example - want to copy directories across file system boundaries:
        /// </para>
        /// <para>
        /// <code>
        /// var sourceDir = _appContext.TenantRoot.GetDirectory("SourceDir");
        /// var targetDir = _appContext.WebRoot.GetDirectory("exchange");
        /// 
        /// var attachedSourceDir = _appContext.ContentRoot.AttachEntry(sourceDir);
        /// var attachedTargetDir = _appContext.ContentRoot.AttachEntry(targetDir);
        /// 
        /// _appContext.ContentRoot.CopyDirectory(attachedSourceDir.SubPath, attachedTargetDir.SubPath);
        /// </code>
        /// This method will throw if the involved file systems are of different type or if source file system is not underneath the target file system.
        /// </para>
        /// </summary>
        /// <typeparam name="TEntry">Entry type, either <see cref="IFile"/> or <see cref="IDirectory"/>.</typeparam>
        /// <param name="fs">The file system to attach the entry to.</param>
        /// <param name="source">The source entry to attach.</param>
        public static TEntry AttachEntry<TEntry>(this IFileSystem fs, TEntry source)
            where TEntry : IFileEntry
        {
            Guard.NotNull(fs, nameof(fs));
            Guard.NotNull(source, nameof(source));

            if (source.FileSystem == fs || source.FileSystem.Root.EqualsNoCase(fs.Root))
            {
                return source;
            }

            if (source.FileSystem.GetType() != fs.GetType())
            {
                throw new FileSystemException("While attaching entries, both file system implementations must be of same type.");
            }

            if (!source.FileSystem.Root.StartsWith(fs.Root))
            {
                throw new FileSystemException($"The root of the source entry must be underneath the target file system root. Source: {source.FileSystem.Root}, Target: {fs.Root}");
            }

            var subRoot = FileSystemBase.NormalizePath(source.FileSystem.Root[fs.Root.Length..]);
            var expandedPath = fs.PathCombine(subRoot, source.SubPath);

            if (source.IsDirectory)
            {
                return (TEntry)fs.GetDirectory(expandedPath);
            }
            else
            {
                return (TEntry)fs.GetFile(expandedPath);
            }
        }

        /// <summary>
        /// Retrieves a directory for file path within the storage provider.
        /// </summary>
        /// <param name="subpath">The relative path to the file within the storage provider.</param>
        /// <returns>The directory of the file.</returns>
        /// <exception cref="ArgumentException">Throws if the file or the directory does not exist.</exception>
        public static IDirectory GetDirectoryForFile(this IFileSystem fs, string subpath)
        {
            var file = fs.GetFile(subpath);
            if (!file.Exists)
            {
                throw new FileNotFoundException("File " + subpath + " does not exist.");
            }

            var dir = fs.GetDirectory(file.Directory);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException("Directory " + subpath + " does not exist.");
            }

            return dir;
        }

        /// <summary>
        /// Retrieves a directory for file path within the storage provider.
        /// </summary>
        /// <param name="subpath">The relative path to the file within the storage provider.</param>
        /// <returns>The directory of the file.</returns>
        /// <exception cref="ArgumentException">Throws if the file or the directory does not exist.</exception>
        public static async Task<IDirectory> GetDirectoryForFileAsync(this IFileSystem fs, string subpath)
        {
            var file = await fs.GetFileAsync(subpath);
            if (!file.Exists)
            {
                throw new FileNotFoundException("File " + subpath + " does not exist.");
            }

            var dir = await fs.GetDirectoryAsync(file.Directory);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException("Directory " + subpath + " does not exist.");
            }

            return dir;
        }

        /// <summary>
        /// Creates a unique (non-existing) directory name within a given path.
        /// </summary>
        /// <param name="subpath">Path to a directory</param>
        /// <param name="defaultName">Default name for directory. <c>null</c> to use a guid.</param>
        /// <returns>Unique directory name</returns>
        public static string CreateUniqueDirectoryName(this IFileSystem fs, string subpath, string defaultName)
        {
            if (defaultName.IsEmpty())
            {
                defaultName = Guid.NewGuid().ToString();
            }

            if (subpath.IsEmpty() || !fs.DirectoryExists(subpath))
            {
                return defaultName;
            }

            var newName = defaultName;

            for (int i = 1; i < 999999; ++i)
            {
                if (!fs.DirectoryExists(fs.PathCombine(subpath, newName)))
                    break;

                newName = defaultName + i.ToString();
            }

            return newName;
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
            Guard.NotEmpty(subpath, nameof(subpath));

            var entry = fs.GetEntry(subpath);
            if (entry is not IDirectory dir)
            {
                throw new FileSystemException($"Path '{subpath}' must point to a directory, not to a file.");
            }

            return new DirectoryHasher(dir, storageDir, searchPattern, deep);
        }

        #endregion
    }
}