using System;
using System.Threading.Tasks;

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
        /// Moves an existing directory to a new location.
        /// An exception will be raised if the destination exists.
        /// </summary>
        void MoveTo(string newPath) => throw new NotSupportedException();

        /// <summary>
        /// Moves an existing directory to a new location.
        /// An exception will be raised if the destination exists.
        /// </summary>
        Task MoveToAsync(string newPath)
        {
            MoveTo(newPath);
            return Task.CompletedTask;
        }
    }
}