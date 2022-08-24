using System.Runtime.CompilerServices;
using Smartstore.IO;

namespace Smartstore
{
    public static partial class IFileSystemExtensions
    {
        /// <summary>
        /// Retrieves a directory for file path within the storage provider.
        /// </summary>
        /// <param name="subpath">The relative path to the file within the storage provider.</param>
        /// <returns>The directory of the file.</returns>
        /// <exception cref="ArgumentException">Throws if the file or the directory does not exist.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDirectory GetDirectoryForFile(this IFileSystem fs, string subpath)
            => GetDirectoryForFileInternal(fs, subpath, false).Await();

        /// <summary>
        /// Retrieves a directory for file path within the storage provider.
        /// </summary>
        /// <param name="subpath">The relative path to the file within the storage provider.</param>
        /// <returns>The directory of the file.</returns>
        /// <exception cref="ArgumentException">Throws if the file or the directory does not exist.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<IDirectory> GetDirectoryForFileAsync(this IFileSystem fs, string subpath)
            => GetDirectoryForFileInternal(fs, subpath, true);

        public static async Task<IDirectory> GetDirectoryForFileInternal(IFileSystem fs, string subpath, bool async)
        {
            Guard.NotNull(fs, nameof(fs));
            Guard.NotNull(subpath, nameof(subpath));

            var file = async ? await fs.GetFileAsync(subpath) : fs.GetFile(subpath);
            if (!file.Exists)
            {
                throw new FileNotFoundException("File " + subpath + " does not exist.");
            }

            var dir = async ? await fs.GetDirectoryAsync(file.Directory) : fs.GetDirectory(file.Directory);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException("Directory " + subpath + " does not exist.");
            }

            return dir;
        }


        /// <summary>
        /// Deletes a file if it exists.
        /// </summary>
        /// <param name="subpath">The relative path of the file to be deleted.</param>
        /// <returns><c>true</c> if the file was deleted; <c>false</c> if the file did not exist.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryDeleteFile(this IFileSystem fs, string subpath)
            => TryDeleteFileInternal(fs, fs.GetFile(subpath), false).Await();

        /// <summary>
        /// Deletes a file if it exists.
        /// </summary>
        /// <param name="file">The file to delete.</param>
        /// <returns><c>true</c> if the file was deleted; <c>false</c> if the file did not exist.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryDeleteFile(this IFileSystem fs, IFile file)
            => TryDeleteFileInternal(fs, file, false).Await();

        /// <summary>
        /// Deletes a file if it exists.
        /// </summary>
        /// <param name="subpath">The relative path of the file to be deleted.</param>
        /// <returns><c>true</c> if the file was deleted; <c>false</c> if the file did not exist.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<bool> TryDeleteFileAsync(this IFileSystem fs, string subpath)
            => await TryDeleteFileInternal(fs, await fs.GetFileAsync(subpath), true);

        /// <summary>
        /// Deletes a file if it exists.
        /// </summary>
        /// <param name="file">The file to deleted</param>
        /// <returns><c>true</c> if the file was deleted; <c>false</c> if the file did not exist.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<bool> TryDeleteFileAsync(this IFileSystem fs, IFile file)
            => TryDeleteFileInternal(fs, file, true);

        private static async Task<bool> TryDeleteFileInternal(IFileSystem fs, IFile file, bool async)
        {
            Guard.NotNull(fs, nameof(fs));
            Guard.NotNull(file, nameof(file));

            if (!file.Exists)
            {
                return false;
            }

            try
            {
                if (async)
                {
                    await file.DeleteAsync();
                }
                else
                {
                    file.Delete();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Creates a new file from the contents of an input stream.
        /// </summary>
        /// <param name="subpath">The path of the file to be created.</param>
        /// <param name="inStream">The stream whose contents to write to the new file. If <c>null</c>, an empty file will be created.</param>
        /// <param name="overwrite"><c>true</c> to overwrite if a file already exists; <c>false</c> to throw an exception if the file already exists.</param>
        /// <exception cref="FileSystemException">
        /// Thrown if the specified file already exists and <paramref name="overwrite"/> was not set to <c>true</c>, or if the specified path exists but is not a file.
        /// </exception>
        /// <remarks>
        /// If the specified path contains one or more directories, then those directories are created if they do not already exist.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IFile CreateFile(this IFileSystem fs, string subpath, Stream inStream = null, bool overwrite = false)
            => CreateFileInternal(fs, subpath, inStream, overwrite, false).Await();

        /// <summary>
        /// Creates a new file from the contents of an input stream.
        /// </summary>
        /// <param name="subpath">The path of the file to be created.</param>
        /// <param name="inStream">The stream whose contents to write to the new file.</param>
        /// <param name="overwrite"><c>true</c> to overwrite if a file already exists; <c>false</c> to throw an exception if the file already exists.</param>
        /// <exception cref="FileSystemException">
        /// Thrown if the specified file already exists and <paramref name="overwrite"/> was not set to <c>true</c>, or if the specified path exists but is not a file.
        /// </exception>
        /// <remarks>
        /// If the specified path contains one or more directories, then those directories are created if they do not already exist.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<IFile> CreateFileAsync(this IFileSystem fs, string subpath, Stream inStream, bool overwrite = false, CancellationToken cancelToken = default)
            => CreateFileInternal(fs, subpath, inStream, overwrite, true, cancelToken);

        private static async Task<IFile> CreateFileInternal(IFileSystem fs, string subpath, Stream inStream, bool overwrite, bool async, CancellationToken cancelToken = default)
        {
            Guard.NotNull(fs, nameof(fs));

            var dirExists = async ? await fs.DirectoryExistsAsync(subpath) : fs.DirectoryExists(subpath);
            if (dirExists)
            {
                throw new FileSystemException($"Cannot create file '{subpath}' because it already exists as a directory.");
            }

            var file = async ? await fs.GetFileAsync(subpath) : fs.GetFile(subpath);
            if (!overwrite && file.Exists)
            {
                throw new FileSystemException($"Cannot create file '{subpath}' because it already exists.");
            }

            // Create directory path if it doesn't exist.
            if (async)
            {
                await fs.TryCreateDirectoryAsync(file.Directory);
                await file.CreateAsync(inStream, overwrite, cancelToken);
            }
            else
            {
                fs.TryCreateDirectory(file.Directory);
                file.Create(inStream, overwrite);
            }

            return file;
        }


        /// <summary>
        /// Creates a copy of a file.
        /// </summary>
        /// <param name="subpath">The path of the source file to be copied.</param>
        /// <param name="newPath">The path of the destination file to be created.</param>
        /// <param name="overwrite">Whether to overwrite a file with same name.</param>
        /// <exception cref="FileSystemException">
        /// Thrown if the specified file already exists and <paramref name="overwrite"/> was not set to <c>true</c>, or if the specified path exists but is not a file.
        /// </exception>
        public static void CopyFile(this IFileSystem fs, string subpath, string newPath, bool overwrite = false)
        {
            Guard.NotNull(fs, nameof(fs));
            fs.GetFile(subpath).CopyTo(newPath, overwrite);
        }

        /// <summary>
        /// Creates a copy of a file.
        /// </summary>
        /// <param name="subpath">The path of the source file to be copied.</param>
        /// <param name="newPath">The path of the destination file to be created.</param>
        /// <param name="overwrite">Whether to overwrite a file with same name.</param>
        /// <exception cref="FileSystemException">
        /// Thrown if the specified file already exists and <paramref name="overwrite"/> was not set to <c>true</c>, or if the specified path exists but is not a file.
        /// </exception>
        public static async Task CopyFileAsync(this IFileSystem fs, string subpath, string newPath, bool overwrite = false)
        {
            Guard.NotNull(fs, nameof(fs));
            await (await fs.GetFileAsync(subpath)).CopyToAsync(newPath, overwrite);
        }
    }
}