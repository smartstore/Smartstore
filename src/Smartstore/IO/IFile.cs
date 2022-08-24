using System.Drawing;

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
        /// Determines the pixel size if the file is an image.
        /// </summary>
        /// <returns>The image pixel size or <see cref="Size.Empty"/>if file is not an image or pixel size could not be determined.</returns>
        Size GetPixelSize();

        /// <summary>
        /// Determines the pixel size if the file is an image.
        /// </summary>
        /// <returns>The image pixel size or <see cref="Size.Empty"/>if file is not an image or pixel size could not be determined.</returns>
        ValueTask<Size> GetPixelSizeAsync()
            => ValueTask.FromResult(GetPixelSize());

        /// <summary>
        /// Creates a stream for reading from the file.
        /// </summary>
        /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
        Stream OpenRead()
            => throw new NotImplementedException();

        /// <summary>
        /// Creates a stream for reading from the file.
        /// </summary>
        /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
        Task<Stream> OpenReadAsync(CancellationToken cancelToken = default)
            => Task.FromResult(OpenRead());

        /// <summary>
        /// Creates a stream for writing to the file. If the directory does not exist, it will be created.
        /// </summary>
        /// <param name="contentType">
        /// The content/mime type of the file that is about to be written to the stream. 
        /// </param>
        /// <exception cref="FileSystemException">Thrown if the file does not exist and the directory could not be created.</exception>
        Stream OpenWrite(string contentType = null)
            => throw new NotImplementedException();

        /// <summary>
        /// Creates a stream for writing to the file. If the directory does not exist, it will be created.
        /// </summary>
        /// <param name="contentType">
        /// The content/mime type of the file that is about to be written to the stream. 
        /// </param>
        /// <exception cref="FileSystemException">Thrown if the file does not exist and the directory could not be created.</exception>
        Task<Stream> OpenWriteAsync(string contentType = null, CancellationToken cancelToken = default)
            => Task.FromResult(OpenWrite());

        /// <summary>
        /// Copies an existing file to a new file.
        /// If <paramref name="overwrite"/> is false, an exception will be
        /// raised if the destination exists. Otherwise it will be overwritten.
        /// </summary>
        IFile CopyTo(string newPath, bool overwrite)
            => throw new NotImplementedException();

        /// <summary>
        /// Copies an existing file to a new file.
        /// If <paramref name="overwrite"/> is false, an exception will be
        /// raised if the destination exists. Otherwise it will be overwritten.
        /// </summary>
        Task<IFile> CopyToAsync(string newPath, bool overwrite, CancellationToken cancelToken = default)
            => Task.FromResult(CopyTo(newPath, overwrite));

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
        void Create(Stream inStream, bool overwrite)
            => throw new NotImplementedException();

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
        Task CreateAsync(Stream inStream, bool overwrite, CancellationToken cancelToken = default)
        {
            Create(inStream, overwrite);
            return Task.CompletedTask;
        }
    }
}