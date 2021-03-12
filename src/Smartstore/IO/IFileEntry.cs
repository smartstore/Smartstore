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
        /// Determines whether this file system entry is a symbolic link.
        /// </summary>
        /// <param name="finalPhysicalPath">The final target path if the entry is a symbolic link, <c>null</c> otherwise</param>
        /// <returns>
        /// <code>true</code> if the entry is a symbolic link, <code>false</code> otherwise.
        /// </returns>
        bool IsSymbolicLink(out string finalPhysicalPath);
    }
}