using Microsoft.Extensions.FileProviders;

namespace Smartstore.IO
{
    /// <inheritdoc/>
    public interface IFileEntry : IFileInfo
    {
        /// <summary>
        /// Gets the file system provider this entry was resolved from.
        /// </summary>
        IFileSystem FileSystem { get; }

        /// <summary>
        /// The path relative to the storage root
        /// </summary>
        string SubPath { get; }

        /// <summary>
        /// When the entry was created (UTC)
        /// </summary>
        DateTimeOffset CreatedOn { get; }

        /// <summary>
        /// Determines whether this file system entry is a symbolic link.
        /// </summary>
        /// <param name="finalTargetPath">The final target path if the entry is a symbolic link, <c>null</c> otherwise</param>
        /// <returns>
        /// <code>true</code> if the entry is a symbolic link, <code>false</code> otherwise.
        /// </returns>
        bool IsSymbolicLink(out string finalTargetPath);

        /// <summary>
        /// Deletes the file entry. Directories will be deleted recursively.
        /// An exception will be raised if the entry does not exists.
        /// </summary>
        void Delete()
            => throw new NotImplementedException();

        /// <summary>
        /// Deletes the file entry if it exists. Directories will be deleted recursively.
        /// An exception will be raised if the entry does not exists.
        /// </summary>
        Task DeleteAsync(CancellationToken cancelToken = default)
        {
            Delete();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Moves an existing file entry to a new location.
        /// An exception will be raised if the destination exists.
        /// </summary>
        void MoveTo(string newPath)
            => throw new NotImplementedException();

        /// <summary>
        /// Moves an existing file entry to a new location.
        /// An exception will be raised if the destination exists.
        /// </summary>
        Task MoveToAsync(string newPath, CancellationToken cancelToken = default)
        {
            MoveTo(newPath);
            return Task.CompletedTask;
        }
    }
}