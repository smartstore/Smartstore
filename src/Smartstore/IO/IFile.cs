using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace Smartstore.IO
{
    public interface IFile : IFileEntry
    {
        /// <summary>
        /// The relative path without the file part, but with trailing slash
        /// </summary>
        string Directory { get; }

        /// <summary>
        /// File name excluding extension
        /// </summary>
        string NameWithoutExtension { get; }

        /// <summary>
        /// File extension including dot
        /// </summary>
        string Extension { get; }

        /// <summary>
        /// Dimensions, if the file is an image.
        /// </summary>
        Size Size { get; }

        /// <summary>
        /// Creates a stream for reading from the file.
        /// </summary>
        /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
        Stream OpenRead() => throw new NotSupportedException();

        /// <summary>
        /// Creates a stream for reading from the file.
        /// </summary>
        /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
        Task<Stream> OpenReadAsync() => throw new NotSupportedException();

        /// <summary>
        /// Creates a stream for writing to the file. If the directory does not exist, it will be created.
        /// </summary>
        /// <exception cref="FileSystemException">Thrown if the file does not exist and the directory could not be created.</exception>
        Stream OpenWrite() => throw new NotSupportedException();

        /// <summary>
        /// Copies an existing file to a new file.
        /// If <paramref name="overwrite"/> is false, an exception will be
        /// raised if the destination exists. Otherwise it will be overwritten.
        /// </summary>
        void CopyTo(string newPath, bool overwrite = false) => throw new NotSupportedException();

        /// <summary>
        /// Copies an existing file to a new file.
        /// If <paramref name="overwrite"/> is false, an exception will be
        /// raised if the destination exists. Otherwise it will be overwritten.
        /// </summary>
        Task CopyToAsync(string newPath, bool overwrite = false)
        {
            CopyTo(newPath, overwrite);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Moves an existing file to a new location and potentially a new name.
        /// If <paramref name="overwrite"/> is false, an exception will be
        /// raised if the destination exists. Otherwise it will be overwritten.
        /// </summary>
        void MoveTo(string newPath, bool overwrite = false) => throw new NotSupportedException();

        /// <summary>
        /// Moves an existing file to a new location and potentially a new name.
        /// If <paramref name="overwrite"/> is false, an exception will be
        /// raised if the destination exists. Otherwise it will be overwritten.
        /// </summary>
        Task MoveToAsync(string newPath, bool overwrite = false)
        {
            MoveTo(newPath, overwrite);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates or overwrites the file from the contents of an input stream.
        /// </summary>
        /// <param name="inStream">The stream whose contents to write to the file. If <c>null</c>, an empty file will be created.</param>
        /// <param name="overwrite"><c>true</c> to overwrite an existing file; <c>false</c> to raise an exception if the file already exists.</param>
        /// <exception cref="FileSystemException">
        /// Thrown if the specified file already exists and <paramref name="overwrite"/> was not set to <c>true</c>.
        /// </exception>
        /// <remarks>
        /// If the specified path contains one or more directories, then those directories are created if they do not already exist.
        /// </remarks>
        void Create(Stream inStream = null, bool overwrite = false) => throw new NotSupportedException();

        /// <summary>
        /// Creates or overwrites the file from the contents of an input stream.
        /// </summary>
        /// <param name="inStream">The stream whose contents to write to the file. If <c>null</c>, an empty file will be created.</param>
        /// <param name="overwrite"><c>true</c> to overwrite an existing file; <c>false</c> to raise an exception if the file already exists.</param>
        /// <exception cref="FileSystemException">
        /// Thrown if the specified file already exists and <paramref name="overwrite"/> was not set to <c>true</c>.
        /// </exception>
        /// <remarks>
        /// If the specified path contains one or more directories, then those directories are created if they do not already exist.
        /// </remarks>
        Task CreateAsync(Stream inStream = null, bool overwrite = false) => throw new NotSupportedException();
    }
}