using Smartstore.IO;

namespace Smartstore
{
    public static partial class IFileSystemExtensions
    {
        /// <summary>
        /// Retrieves a file system entry.
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
        /// Retrieves a file system entry.
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

        /// <summary>
        /// Renames/moves a file or a directory.
        /// </summary>
        /// <param name="subpath">The relative path to the file or the directory to be renamed/moved.</param>
        /// <param name="newPath">The new path after entry was moved/renamed.</param>
        /// <exception cref="FileSystemException">Thrown if source does not exist or if <paramref name="newPath"/> already exists.</exception>
        public static void MoveEntry(this IFileSystem fs, string subpath, string newPath)
        {
            Guard.NotNull(newPath, nameof(newPath));
            fs.GetEntry(subpath).MoveTo(newPath);
        }

        /// <summary>
        /// Renames/moves a file or a directory.
        /// </summary>
        /// <param name="subpath">The relative path to the file or the directory to be renamed/moved.</param>
        /// <param name="newPath">The new path after entry was moved/renamed.</param>
        /// <exception cref="FileSystemException">Thrown if source does not exist or if <paramref name="newPath"/> already exists.</exception>
        public static async Task MoveEntryAsync(this IFileSystem fs, string subpath, string newPath)
        {
            Guard.NotNull(newPath, nameof(newPath));
            await (await fs.GetEntryAsync(subpath)).MoveToAsync(newPath);
        }

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

            var sourceType = source.FileSystem.GetType();
            var thisType = fs.GetType();
            if (sourceType != thisType && (sourceType.BaseType == null || sourceType.BaseType != thisType))
            {
                throw new FileSystemException("While attaching entries, both file system implementations must be of same type.");
            }

            if (!source.FileSystem.Root.StartsWith(fs.Root))
            {
                throw new FileSystemException($"The root of the source entry must be underneath the target file system root. Source: {source.FileSystem.Root}, Target: {fs.Root}");
            }

            var subRoot = PathUtility.NormalizeRelativePath(source.FileSystem.Root[fs.Root.Length..]);
            var expandedPath = PathUtility.Join(subRoot, source.SubPath);

            if (source.IsDirectory)
            {
                return (TEntry)fs.GetDirectory(expandedPath);
            }
            else
            {
                return (TEntry)fs.GetFile(expandedPath);
            }
        }
    }
}