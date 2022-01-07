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
        public static IDirectory GetDirectoryForFile(this IFileSystem fs, string subpath)
        {
            Guard.NotNull(fs, nameof(fs));
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

        /// <summary>
        /// Retrieves a directory for file path within the storage provider.
        /// </summary>
        /// <param name="subpath">The relative path to the file within the storage provider.</param>
        /// <returns>The directory of the file.</returns>
        /// <exception cref="ArgumentException">Throws if the file or the directory does not exist.</exception>
        public static async Task<IDirectory> GetDirectoryForFileAsync(this IFileSystem fs, string subpath)
        {
            Guard.NotNull(fs, nameof(fs));
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


        /// <summary>
        /// Deletes a file if it exists.
        /// </summary>
        /// <param name="subpath">The relative path of the file to be deleted.</param>
        /// <returns><c>true</c> if the file was deleted; <c>false</c> if the file did not exist.</returns>
        public static bool TryDeleteFile(this IFileSystem fs, string subpath)
            => TryDeleteFile(fs, fs.GetFile(subpath));

        /// <summary>
        /// Deletes a file if it exists.
        /// </summary>
        /// <param name="file">The file to delete.</param>
        /// <returns><c>true</c> if the file was deleted; <c>false</c> if the file did not exist.</returns>
        public static bool TryDeleteFile(this IFileSystem fs, IFile file)
        {
            Guard.NotNull(fs, nameof(fs));
            Guard.NotNull(file, nameof(file));

            if (!file.Exists)
            {
                return false;
            }

            try
            {
                file.Delete();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Deletes a file if it exists.
        /// </summary>
        /// <param name="subpath">The relative path of the file to be deleted.</param>
        /// <returns><c>true</c> if the file was deleted; <c>false</c> if the file did not exist.</returns>
        public static async Task<bool> TryDeleteFileAsync(this IFileSystem fs, string subpath)
            => await TryDeleteFileAsync(fs, await fs.GetFileAsync(subpath));

        /// <summary>
        /// Deletes a file if it exists.
        /// </summary>
        /// <param name="file">The file to deleted</param>
        /// <returns><c>true</c> if the file was deleted; <c>false</c> if the file did not exist.</returns>
        public static async Task<bool> TryDeleteFileAsync(this IFileSystem fs, IFile file)
        {
            Guard.NotNull(fs, nameof(fs));
            Guard.NotNull(file, nameof(file));

            if (!file.Exists)
            {
                return false;
            }

            try
            {
                await file.DeleteAsync();
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
        public static IFile CreateFile(this IFileSystem fs, string subpath, Stream inStream = null, bool overwrite = false)
        {
            Guard.NotNull(fs, nameof(fs));

            if (fs.DirectoryExists(subpath))
            {
                throw new FileSystemException($"Cannot create file '{subpath}' because it already exists as a directory.");
            }

            var file = fs.GetFile(subpath);
            if (!overwrite && file.Exists)
            {
                throw new FileSystemException($"Cannot create file '{subpath}' because it already exists.");
            }

            // Create directory path if it doesn't exist.
            fs.TryCreateDirectory(file.Directory);

            file.Create(inStream, overwrite);
            return file;
        }

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
        public static async Task<IFile> CreateFileAsync(this IFileSystem fs, string subpath, Stream inStream, bool overwrite = false)
        {
            Guard.NotNull(fs, nameof(fs));

            if (await fs.DirectoryExistsAsync(subpath))
            {
                throw new FileSystemException($"Cannot create file '{subpath}' because it already exists as a directory.");
            }

            var file = await fs.GetFileAsync(subpath);
            if (!overwrite && file.Exists)
            {
                throw new FileSystemException($"Cannot create file '{subpath}' because it already exists.");
            }

            // Create directory path if it doesn't exist.
            await fs.TryCreateDirectoryAsync(file.Directory);

            await file.CreateAsync(inStream, overwrite);
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