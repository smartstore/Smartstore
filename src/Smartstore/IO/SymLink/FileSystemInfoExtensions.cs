using System.IO;

namespace Smartstore.IO.SymLinks
{
    public static class FileSystemInfoExtensions
    {
        /// <summary>
        /// Determines whether this file system entry is a symbolic link.
        /// </summary>
        /// <param name="fsi">The directory or file in question.</param>
        /// <returns><code>true</code> if the entry is a symbolic link, <code>false</code> otherwise.</returns>
        public static bool IsSymbolicLink(this FileSystemInfo fsi)
        {
            return SymbolicLink.IsSymbolicLink(fsi, out _);
        }

        /// <summary>
        /// Determines whether this file system entry is a symbolic link.
        /// </summary>
        /// <param name="fsi">The directory or file in question.</param>
        /// <param name="linkedPathName">The final target path name if the entry is a symbolic link or <c>null</c> otherwise</param>
        /// <returns><code>true</code> if the entry is a symbolic link, <code>false</code> otherwise.</returns>
        public static bool IsSymbolicLink(this FileSystemInfo fsi, out string linkedPathName)
        {
            return SymbolicLink.IsSymbolicLink(fsi, out linkedPathName);
        }

        /// <summary>
        /// Returns the full path to the target of a symbolic link or mount.
        /// </summary>
        /// <param name="fsi">The symbolic link in question.</param>
        /// <returns>The path to the target.</returns>
        public static string GetLinkedPathName(this FileSystemInfo fsi)
        {
            return SymbolicLink.GetFinalPathName(fsi.FullName);
        }
    }
}