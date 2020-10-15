using System.Drawing;
using System.IO;

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
        Stream OpenRead();

        /// <summary>
        /// Creates a stream for writing to the file. If the directory does not exist, it will be created.
        /// </summary>
        /// <exception cref="FileSystemException">Thrown if the file does not exist and the directory could not be created.</exception>
        Stream OpenWrite();
    }
}