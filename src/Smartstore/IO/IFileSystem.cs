using Microsoft.Extensions.FileProviders;
using Smartstore.Threading;

namespace Smartstore.IO
{
    /// <summary>
    /// Represents a generic abstraction over a virtual file system.
    /// </summary>
    /// <remarks>
    /// The virtual file system uses forward slash (/) as the path delimiter, and has no concept of
    /// volumes or drives. All paths are specified and returned as relative to the root of the virtual
    /// file system. Absolute paths using a leading slash or leading period, and parent traversal
    /// using "../", are not supported.
    /// </remarks>
    public interface IFileSystem : IFileProvider
    {
        /// <summary>
        /// Gets the storage root path
        /// </summary>
        string Root { get; }

        /// <summary>
        /// Maps a given subpath to a physical full storage specific path. 
        /// </summary>
        /// <param name="subpath">The relative path to the entry within the storage.</param>
        /// <returns>
        ///     The full physical path, 
        ///     or <c>null</c> if <paramref name="subpath"/> has invalid chars, is rooted or navigates above root.
        /// </returns>
        string MapPath(string subpath);

        /// <summary>
        /// Checks if the given file exists within the storage provider.
        /// </summary>
        /// <param name="subpath">The relative path within the storage provider.</param>
        /// <returns>True if the file exists; False otherwise.</returns>
        bool FileExists(string subpath);

        /// <summary>
        /// Checks if the given file exists within the storage provider.
        /// </summary>
        /// <param name="subpath">The relative path within the storage provider.</param>
        /// <returns>True if the file exists; False otherwise.</returns>
        Task<bool> FileExistsAsync(string subpath);

        /// <summary>
        /// Checks if the given directory exists within the storage provider.
        /// </summary>
        /// <param name="subpath">The relative path within the storage provider.</param>
        /// <returns>True if the folder exists; False otherwise.</returns>
        bool DirectoryExists(string subpath);

        /// <summary>
        /// Checks if the given directory exists within the storage provider.
        /// </summary>
        /// <param name="subpath">The relative path within the storage provider.</param>
        /// <returns>True if the folder exists; False otherwise.</returns>
        Task<bool> DirectoryExistsAsync(string subpath);

        /// <summary>
        /// Retrieves a file.
        /// </summary>
        /// <param name="subpath">The relative path to the file within the storage.</param>
        /// <returns>
        ///     A <see cref="IFile"/> object representing the file, 
        ///     or <see cref="NotFoundFile"/> if <paramref name="subpath"/> has invalid chars, is rooted or navigates above root.
        /// </returns>
        IFile GetFile(string subpath);

        /// <summary>
        /// Retrieves a file.
        /// </summary>
        /// <param name="subpath">The relative path to the file within the storage.</param>
        /// <returns>
        ///     A <see cref="IFile"/> object representing the file, 
        ///     or <see cref="NotFoundFile"/> if <paramref name="subpath"/> has invalid chars, is rooted or navigates above root.
        /// </returns>
        Task<IFile> GetFileAsync(string subpath);

        /// <summary>
        /// Retrieves a directory.
        /// </summary>
        /// <param name="subpath">The relative path to the directory within the storage.</param>
        /// <returns>
        ///     A <see cref="IDirectory"/> object representing the directory, 
        ///     or <see cref="NotFoundDirectory"/> if <paramref name="subpath"/> has invalid chars, is rooted or navigates above root.
        /// </returns>
        IDirectory GetDirectory(string subpath);

        /// <summary>
        /// Retrieves a directory.
        /// </summary>
        /// <param name="subpath">The relative path to the directory within the storage.</param>
        /// <returns>
        ///     A <see cref="IDirectory"/> object representing the directory, 
        ///     or <see cref="NotFoundDirectory"/> if <paramref name="subpath"/> has invalid chars, is rooted or navigates above root.
        /// </returns>
        Task<IDirectory> GetDirectoryAsync(string subpath);

        /// <summary>
        /// Checks whether the name of the file is unique within its directory.
        /// When given file exists, this method appends [1...n] to the file title until
        /// the check returns false.
        /// </summary>
        /// <param name="subpath">The relative path of file to check</param>
        /// <param name="success">If <paramref name="subpath"/> existed, calls this action passing the new unique path.</param>
        /// <returns>
        /// <c>true</c> if a new unique file name was resolved; <c>false</c> if the file name was unique already.
        /// </returns>
        bool CheckUniqueFileName(string subpath, out string newPath);

        /// <summary>
        /// Checks whether the name of the file is unique within its directory.
        /// When given file exists, this method appends [1...n] to the file title until
        /// the check returns false.
        /// </summary>
        /// <param name="subpath">The relative path of file to check</param>
        /// <returns>
        /// <c>true</c> and new path if a new unique file name was resolved; <c>false</c> and <c>null</c> if the file name was unique already.
        /// </returns>
        Task<AsyncOut<string>> CheckUniqueFileNameAsync(string subpath);
    }
}