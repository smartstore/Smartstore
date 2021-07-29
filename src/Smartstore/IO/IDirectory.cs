using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;

namespace Smartstore.IO
{
    public interface IDirectory : IFileEntry
    {
        /// <summary>
        /// Determines whether the directory is the root directory of the file storage.
        /// </summary>
        bool IsRoot { get; }

        /// <summary>
        /// The parent directory.
        /// </summary>
        /// <exception cref="FileSystemException">Thrown if the directory does not exist.</exception>
        IDirectory Parent { get; }

        /// <summary>
        /// Creates all directories and subdirectories in the target unless they already exist.
        /// </summary>
        void Create() => throw new NotSupportedException();

        /// <summary>
        /// Creates all directories and subdirectories in the target unless they already exist.
        /// </summary>
        Task CreateAsync()
        {
            Create();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates a subdirectory or subdirectories on the specified path. The specified
        /// path is relative to this <see cref="IDirectory"/> instance.
        /// </summary>
        /// <param name="path">The specified path</param>
        /// <returns>The last directory specified in <paramref name="path"/>.</returns>
        IDirectory CreateSubdirectory(string path) => throw new NotSupportedException();

        /// <summary>
        /// Creates a subdirectory or subdirectories on the specified path. The specified
        /// path is relative to this <see cref="IDirectory"/> instance.
        /// </summary>
        /// <param name="path">The specified path</param>
        /// <returns>The last directory specified in <paramref name="path"/>.</returns>
        Task<IDirectory> CreateSubdirectoryAsync(string path) => Task.FromResult(CreateSubdirectory(path));

        /// <summary>
        /// Enumerates the content (files and directories) in the directory.
        /// </summary>
        /// <param name="pattern">The file pattern to match.</param>
        /// <param name="deep">A flag to indicate whether to get the contents from just the top directory or from all sub-directories as well.</param>
        /// <returns>The list of files and directories in the given directory.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if the specified directory does not exist.</exception>
        /// <remarks>
        /// Results are grouped by entry type, where directories are followed by files.
        /// </remarks>
        IEnumerable<IFileEntry> EnumerateEntries(string pattern = "*", bool deep = false) => throw new NotSupportedException();

        /// <summary>
        /// Enumerates the content (files and directories) in the directory.
        /// </summary>
        /// <param name="pattern">The file pattern to match.</param>
        /// <param name="deep">A flag to indicate whether to get the contents from just the top directory or from all sub-directories as well.</param>
        /// <returns>The list of files and directories in the given directory.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if the specified directory does not exist.</exception>
        /// <remarks>
        /// Results are grouped by entry type, where directories are followed by files.
        /// </remarks>
        IAsyncEnumerable<IFileEntry> EnumerateEntriesAsync(string pattern = "*", bool deep = false) => EnumerateEntries(pattern, deep).ToAsyncEnumerable();

        /// <summary>
        /// Enumerates the directories in the directory.
        /// </summary>
        /// <param name="pattern">The directory pattern to match.</param>
        /// <param name="deep">A flag to indicate whether to get the directories from just the top directory or from all sub-directories as well.</param>
        /// <returns>The list of directories in the given directory.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if the directory does not exist.</exception>
        IEnumerable<IDirectory> EnumerateDirectories(string pattern = "*", bool deep = false) => throw new NotSupportedException();

        /// <summary>
        /// Enumerates the directories in the directory.
        /// </summary>
        /// <param name="pattern">The directory pattern to match.</param>
        /// <param name="deep">A flag to indicate whether to get the directories from just the top directory or from all sub-directories as well.</param>
        /// <returns>The list of directories in the given directory.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if the directory does not exist.</exception>
        IAsyncEnumerable<IDirectory> EnumerateDirectoriesAsync(string pattern = "*", bool deep = false) => EnumerateDirectories(pattern, deep).ToAsyncEnumerable();

        /// <summary>
        /// Enumerates the files in the directory.
        /// </summary>
        /// <param name="pattern">The file pattern to match.</param>
        /// <param name="deep">A flag to indicate whether to get the files from just the top directory or from all sub-directories as well.</param>
        /// <returns>The list of files in the given directory.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if the directory does not exist.</exception>
        IEnumerable<IFile> EnumerateFiles(string pattern = "*", bool deep = false) => throw new NotSupportedException();

        /// <summary>
        /// Enumerates the files in the directory.
        /// </summary>
        /// <param name="pattern">The file pattern to match.</param>
        /// <param name="deep">A flag to indicate whether to get the files from just the top directory or from all sub-directories as well.</param>
        /// <returns>The list of files in the given directory.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if the directory does not exist.</exception>
        IAsyncEnumerable<IFile> EnumerateFilesAsync(string pattern = "*", bool deep = false) => EnumerateFiles(pattern, deep).ToAsyncEnumerable();

        /// <summary>
        /// Sums the total length of all files contained within the directory.
        /// </summary>
        /// <param name="pattern">The file pattern to match</param>
        /// <param name="deep">Whether to sum up length in all subdirectories also.</param>
        /// <returns>Total length of all files.</returns>
        long GetDirectorySize(string pattern = "*", bool deep = true)
        {
            return EnumerateFiles(pattern, deep).AsParallel().Sum(x => x.Length);
        }

        /// <summary>
        /// Sums the total length of all files contained within the directory.
        /// </summary>
        /// <param name="pattern">The file pattern to match</param>
        /// <param name="deep">Whether to sum up length in all subdirectories also.</param>
        /// <returns>Total length of all files.</returns>
        async Task<long> GetDirectorySizeAsync(string pattern = "*", bool deep = true)
        {
            return await EnumerateFilesAsync(pattern, deep).SumAsync(x => x.Length);
        }

        /// <summary>
        /// Retrieves the count of files within the directory.
        /// </summary>
        /// <param name="pattern">The file pattern to match</param>
        /// <param name="deep">Whether to count files in all subdirectories also</param>
        /// <returns>Total count of files.</returns>
        long CountFiles(string pattern = "*", bool deep = true)
        {
            return EnumerateFiles(pattern, deep).AsParallel().Count();
        }

        /// <summary>
        /// Retrieves the count of files within the directory.
        /// </summary>
        /// <param name="pattern">The file pattern to match</param>
        /// <param name="deep">Whether to count files in all subdirectories also</param>
        /// <returns>Total count of files.</returns>
        async Task<long> CountFilesAsync(string pattern = "*", bool deep = true)
        {
            return await EnumerateFilesAsync(pattern, deep).CountAsync();
        }
    }
}