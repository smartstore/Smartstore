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
        /// Retrieves a directory for file path within the storage provider.
        /// </summary>
        /// <param name="subpath">The relative path to the file within the storage provider.</param>
        /// <returns>The directory of the file.</returns>
        /// <exception cref="ArgumentException">Throws if the file or the directory does not exist.</exception>
        public static async Task<IDirectory> GetDirectoryForFileAsync(this IFileSystem fs, string subpath)
        {
            Guard.NotNull(subpath, nameof(subpath));

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

        #region Read

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<string> ReadAllTextAsync(this IFileSystem fs, string subpath, Encoding encoding = null)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            return await (await fs.GetFileAsync(subpath)).ReadAllTextAsync(encoding);
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
        public static async Task<byte[]> ReadAllBytesAsync(this IFileSystem fs, string subpath)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            return await (await fs.GetFileAsync(subpath)).ReadAllBytesAsync();
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

        public static async Task WriteAllTextAsync(this IFileSystem fs, string subpath, string contents, Encoding encoding = null)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            Guard.NotNull(contents, nameof(contents));

            var file = await fs.GetFileAsync(subpath);

            using (var stream = file.OpenWrite())
            using (var streamWriter = new StreamWriter(stream, encoding ?? new UTF8Encoding(false, true)))
            {
                await streamWriter.WriteAsync(contents);
            }
        }

        public static async Task WriteAllBytesAsync(this IFileSystem fs, string subpath, byte[] contents)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            Guard.NotNull(contents, nameof(contents));

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
        public static async Task MoveEntryAsync(this IFileSystem fs, string subpath, string newPath)
        {
            Guard.NotNull(newPath, nameof(newPath));

            var entry = await fs.GetEntryAsync(subpath);

            if (entry is IDirectory dir)
            {
                await dir.MoveToAsync(newPath);
            }
            else if (entry is IFile file)
            {
                await file.MoveToAsync(newPath, false);
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
        public static Task<IDirectory> CopyDirectoryAsync(this IFileSystem fs, string sourcePath, string destinationPath, bool overwrite = true, string[] ignorePatterns = null)
        {
            Guard.NotEmpty(sourcePath, nameof(sourcePath));
            Guard.NotEmpty(destinationPath, nameof(destinationPath));

            return CopyDirectoryAsync(fs, fs.GetDirectory(sourcePath), fs.GetDirectory(destinationPath), overwrite, ignorePatterns);
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
        public static async Task<IDirectory> CopyDirectoryAsync(this IFileSystem fs, IDirectory source, IDirectory destination, bool overwrite = true, string[] ignorePatterns = null)
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
                destination.SubPath,
                ignorePatterns?.Select(x => new Wildcard(x))?.ToArray() ?? Array.Empty<Wildcard>(),
                overwrite);

            return fs.GetDirectory(destination.SubPath);
        }

        private static async Task InternalCopyDirectoryAsync(IFileSystem fs,
            IDirectory source,
            string destinationPath,
            Wildcard[] ignores,
            bool overwrite)
        {
            await fs.TryCreateDirectoryAsync(destinationPath);

            await foreach (var entry in fs.EnumerateEntriesAsync(source.SubPath))
            {
                if (ignores.Any(w => w.IsMatch(entry.SubPath)))
                {
                    continue;
                }

                var newPath = fs.PathCombine(destinationPath, entry.Name);

                if (entry is IDirectory dir)
                {
                    await InternalCopyDirectoryAsync(fs, dir, newPath, ignores, overwrite);
                }
                else
                {
                    await fs.CopyFileAsync(entry.SubPath, newPath, overwrite);
                }
            }
        }

        #endregion
    }
}
