using System.Threading.Tasks;
using Smartstore.IO;

namespace Smartstore
{
    public static class IDirectoryExtensions
    {
        /// <summary>
        /// Gets a file.
        /// </summary>
        /// <param name="directory">Directory.</param>
        /// <param name="fileName">Name of the file including file extension.</param>
        /// <returns>File.</returns>
        public static IFile GetFile(this IDirectory directory, string fileName)
        {
            Guard.NotNull(directory, nameof(directory));

            if (fileName.IsEmpty())
            {
                return null;
            }

            return directory.FileSystem.GetFile(directory.FileSystem.PathCombine(directory.SubPath, fileName));
        }

        /// <summary>
        /// Gets a file.
        /// </summary>
        /// <param name="directory">Directory.</param>
        /// <param name="fileName">Name of the file including file extension.</param>
        /// <returns>File.</returns>
        public static async Task<IFile> GetFileAsync(this IDirectory directory, string fileName)
        {
            Guard.NotNull(directory, nameof(directory));

            if (fileName.IsEmpty())
            {
                return null;
            }

            return await directory.FileSystem.GetFileAsync(directory.FileSystem.PathCombine(directory.SubPath, fileName));
        }
    }
}
