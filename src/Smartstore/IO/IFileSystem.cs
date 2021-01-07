using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;

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

        //// TODO: (core) Move IFileSystem.GetPublicUrl() to IMediaFileSystem
        ///// <summary>
        ///// Retrieves the public URL for a given file within the storage provider.
        ///// </summary>
        ///// <param name="subpath">The relative path within the storage provider.</param>
        ///// <param name="forCloud">
        ///// If <c>true</c> and the storage is in the cloud, returns the actual remote cloud URL to the resource.
        ///// If <c>false</c>, retrieves an app relative URL to delegate further processing to the media middleware (which can handle remote files)
        ///// </param>
        ///// <returns>The public URL.</returns>
        //string GetPublicUrl(string subpath, bool forCloud = false);

        ///// <summary>
        ///// Retrieves the path within the storage provider for a given public url.
        ///// </summary>
        ///// <param name="url">The virtual or public url of a file.</param>
        ///// <returns>The storage path or <value>null</value> if the media is not in a correct format.</returns>
        //string GetStoragePath(string url);

        /// <summary>
        /// Combines multiple path parts using '/' as directory separator char.
        /// </summary>
        /// <param name="paths">Path parts.</param>
        /// <returns>Combined path</returns>
        /// <remarks>I don't like this name either :-) But changing Path.Combine to PathCombine is really neat.</remarks>
        string PathCombine(params string[] paths);

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
        /// Sums the total length of all files contained within a given directory.
        /// </summary>
        /// <param name="subpath">The relative path to the directory.</param>
        /// <param name="pattern">The file pattern to match</param>
        /// <param name="predicate">Optional. Files not matching the predicate are excluded.</param>
        /// <param name="deep">Whether to sum up length in all subdirectories also.</param>
        /// <returns>Total length of all files.</returns>
        long GetDirectorySize(string subpath, string pattern = "*", Func<string, bool> predicate = null, bool deep = true);

        /// <summary>
        /// Sums the total length of all files contained within a given directory.
        /// </summary>
        /// <param name="subpath">The relative path to the directory.</param>
        /// <param name="pattern">The file pattern to match</param>
        /// <param name="predicate">Optional. Files not matching the predicate are excluded.</param>
        /// <param name="deep">Whether to sum up length in all subdirectories also.</param>
        /// <returns>Total length of all files.</returns>
        Task<long> GetDirectorySizeAsync(string subpath, string pattern = "*", Func<string, bool> predicate = null, bool deep = true);

        /// <summary>
        /// Retrieves the count of files within a path.
        /// </summary>
        /// <param name="subpath">The relative path to the directory in which to retrieve file count.</param>
        /// <param name="pattern">The file pattern to match</param>
        /// <param name="predicate">Optional. Files not matching the predicate are excluded.</param>
        /// <param name="deep">Whether to count files in all subdirectories also</param>
        /// <returns>Total count of files.</returns>
        long CountFiles(string subpath, string pattern = "*", Func<string, bool> predicate = null, bool deep = true);

        /// <summary>
        /// Retrieves the count of files within a path.
        /// </summary>
        /// <param name="subpath">The relative path to the directory in which to retrieve file count.</param>
        /// <param name="pattern">The file pattern to match</param>
        /// <param name="predicate">Optional. Files not matching the predicate are excluded.</param>
        /// <param name="deep">Whether to count files in all subdirectories also</param>
        /// <returns>Total count of files.</returns>
        Task<long> CountFilesAsync(string subpath, string pattern = "*", Func<string, bool> predicate = null, bool deep = true);

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
        IEnumerable<IFileEntry> EnumerateEntries(string subpath = null, string pattern = "*", bool deep = false);

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
        IAsyncEnumerable<IFileEntry> EnumerateEntriesAsync(string subpath = null, string pattern = "*", bool deep = false);

        /// <summary>
        /// Creates all directories and subdirectories in the specified target unless they already exist.
        /// </summary>
        /// <param name="subpath">The path of the directory to be created.</param>
        /// <returns><c>true</c> if the directory was created; <c>false</c> if the directory already existed.</returns>
        /// <exception cref="FileSystemException">Thrown if the specified path exists but is not a directory.</exception>
        bool TryCreateDirectory(string subpath);

        /// <summary>
        /// Creates all directories and subdirectories in the specified target unless they already exist.
        /// </summary>
        /// <param name="subpath">The path of the directory to be created.</param>
        /// <returns><c>true</c> if the directory was created; <c>false</c> if the directory already existed.</returns>
        /// <exception cref="FileSystemException">Thrown if the specified path exists but is not a directory.</exception>
        Task<bool> TryCreateDirectoryAsync(string subpath);

        /// <summary>
        /// Deletes a directory if it exists.
        /// </summary>
        /// <param name="subpath">The path of the directory to be deleted.</param>
        /// <returns><c>true</c> if the directory was deleted; <c>false</c> if the directory did not exist.</returns>
        bool TryDeleteDirectory(string subpath);

        /// <summary>
        /// Deletes a directory if it exists.
        /// </summary>
        /// <param name="subpath">The path of the directory to be deleted.</param>
        /// <returns><c>true</c> if the directory was deleted; <c>false</c> if the directory did not exist.</returns>
        Task<bool> TryDeleteDirectoryAsync(string subpath);

        /// <summary>
        /// Renames/moves a file or a directory.
        /// </summary>
        /// <param name="entry">The entry (file or directory) to move or rename.</param>
        /// <param name="newPath">The new path after entry was moved/renamed.</param>
        /// <exception cref="FileSystemException">Thrown if source does not exist or if <paramref name="newPath"/> already exists.</exception>
        void MoveEntry(IFileEntry entry, string newPath);

        /// <summary>
        /// Renames/moves a file or directory.
        /// </summary>
        /// <param name="subpath">The relative path to the file or directory to be renamed/moved.</param>
        /// <param name="entry">The entry (file or directory) to move or rename.</param>
        /// <param name="newPath">The new path after entry was moved/renamed.</param>
        /// <exception cref="FileSystemException">Thrown if source does not exist or if <paramref name="newPath"/> already exists.</exception>
        Task MoveEntryAsync(IFileEntry entry, string newPath);

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
        /// <param name="success">If <paramref name="subpath"/> existed, calls this action passing the new unique path.</param>
        /// <returns>
        /// <c>true</c> if a new unique file name was resolved; <c>false</c> if the file name was unique already.
        /// </returns>
        Task<bool> CheckUniqueFileNameAsync(string subpath, Action<string> success);

        /// <summary>
        /// Deletes a file if it exists.
        /// </summary>
        /// <param name="subpath">The relative path of the file to be deleted.</param>
        /// <returns><c>true</c> if the file was deleted; <c>false</c> if the file did not exist.</returns>
        bool TryDeleteFile(string subpath);

        /// <summary>
        /// Deletes a file if it exists.
        /// </summary>
        /// <param name="subpath">The relative path of the file to be deleted.</param>
        /// <returns><c>true</c> if the file was deleted; <c>false</c> if the file did not exist.</returns>
        Task<bool> TryDeleteFileAsync(string subpath);

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
        IFile CreateFile(string subpath, Stream inStream = null, bool overwrite = false);

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
        Task<IFile> CreateFileAsync(string subpath, Stream inStream, bool overwrite = false);

        /// <summary>
        /// Creates a copy of a file.
        /// </summary>
        /// <param name="subpath">The path of the source file to be copied.</param>
        /// <param name="newPath">The path of the destination file to be created.</param>
        /// <param name="overwrite">Whether to overwrite a file with same name.</param>
        /// <exception cref="FileSystemException">
        /// Thrown if the specified file already exists and <paramref name="overwrite"/> was not set to <c>true</c>, or if the specified path exists but is not a file.
        /// </exception>
        void CopyFile(string subpath, string newPath, bool overwrite = false);

        /// <summary>
        /// Creates a copy of a file.
        /// </summary>
        /// <param name="subpath">The path of the source file to be copied.</param>
        /// <param name="newPath">The path of the destination file to be created.</param>
        /// <param name="overwrite">Whether to overwrite a file with same name.</param>
        /// <exception cref="FileSystemException">
        /// Thrown if the specified file already exists and <paramref name="overwrite"/> was not set to <c>true</c>, or if the specified path exists but is not a file.
        /// </exception>
        Task CopyFileAsync(string subpath, string newPath, bool overwrite = false);
    }
}