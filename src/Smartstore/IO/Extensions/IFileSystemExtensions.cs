using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Smartstore.IO;
using Smartstore.Utilities;

namespace Smartstore
{
    public static partial class IFileSystemExtensions
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
            Guard.NotNull(subpath, nameof(subpath));

            var entry = fs.GetDirectory(subpath);
            if (entry.Exists || entry is NotFoundDirectory)
            {
                return entry;
            }

            return fs.GetFile(subpath);
        }

        /// <summary>
        /// Retrieves a directory for file path within the storage provider.
        /// </summary>
        /// <param name="subpath">The relative path to the file within the storage provider.</param>
        /// <returns>The directory of the file.</returns>
        /// <exception cref="ArgumentException">Throws if the file or the directory does not exist.</exception>
        public static IDirectory GetDirectoryForFile(this IFileSystem fs, string subpath)
        {
            Guard.NotNull(subpath, nameof(subpath));

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

        #region Read

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadAllText(this IFileSystem fs, string subpath, Encoding encoding = null)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            return fs.GetFile(subpath).ReadAllText(encoding);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ReadAllBytes(this IFileSystem fs, string subpath)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            return fs.GetFile(subpath).ReadAllBytes();
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

        #endregion

        #region Write

        public static void WriteAllText(this IFileSystem fs, string subpath, string contents, Encoding encoding = null)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            Guard.NotNull(contents, nameof(contents));

            var file = fs.GetFile(subpath);

            using (var stream = file.OpenWrite())
            using (var streamWriter = new StreamWriter(stream, encoding ?? new UTF8Encoding(false, true)))
            {
                streamWriter.Write(contents);
            }
        }

        public static void WriteAllBytes(this IFileSystem fs, string subpath, byte[] contents)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            Guard.NotNull(contents, nameof(contents));

            var file = fs.GetFile(subpath);

            using (var stream = file.OpenWrite())
            {
                stream.Write(contents, 0, contents.Length);
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

        #endregion

        #region Copy / Move / Delete

        /// <summary>
        /// Renames/moves a file or a directory.
        /// </summary>
        /// <param name="subpath">The relative path to the file or the directory to be renamed/moved.</param>
        /// <param name="newPath">The new path after entry was moved/renamed.</param>
        /// <exception cref="FileSystemException">Thrown if source does not exist or if <paramref name="newPath"/> already exists.</exception>
        public static void MoveEntry(this IFileSystem fs, string subpath, string newPath, bool overwrite = false)
        {
            Guard.NotNull(newPath, nameof(newPath));

            var entry = fs.GetEntry(subpath);

            if (entry is IDirectory dir)
            {
                dir.MoveTo(newPath);
            }
            else if (entry is IFile file)
            {
                file.MoveTo(newPath, false);
            }
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
        public static IDirectory CopyDirectory(this IFileSystem fs, string sourcePath, string destinationPath, bool overwrite = true, string[] ignorePatterns = null)
        {
            Guard.NotNull(sourcePath, nameof(sourcePath));
            Guard.NotNull(destinationPath, nameof(destinationPath));

            return CopyDirectory(fs, fs.GetDirectory(sourcePath), fs.GetDirectory(destinationPath), overwrite, ignorePatterns);
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
        public static IDirectory CopyDirectory(this IFileSystem fs, IDirectory source, IDirectory destination, bool overwrite = true, string[] ignorePatterns = null)
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
                destination.SubPath,
                ignorePatterns?.Select(x => new Wildcard(x))?.ToArray() ?? Array.Empty<Wildcard>(), 
                overwrite);

            return fs.GetDirectory(destination.SubPath);
        }

        private static void InternalCopyDirectory(IFileSystem fs, 
            IDirectory source, 
            string destinationPath, 
            Wildcard[] ignores,
            bool overwrite)
        {
            fs.TryCreateDirectory(destinationPath);
            
            foreach (var entry in fs.EnumerateEntries(source.SubPath))
            {
                if (ignores.Any(w => w.IsMatch(entry.SubPath)))
                {
                    continue;
                }

                var newPath = fs.PathCombine(destinationPath, entry.Name);

                if (entry is IDirectory dir)
                {
                    InternalCopyDirectory(fs, dir, newPath, ignores, overwrite);
                }
                else
                {
                    fs.CopyFile(entry.SubPath, newPath, overwrite);
                }
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