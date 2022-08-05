using System.Runtime.CompilerServices;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IFile GetFile(this IDirectory directory, string fileName)
            => GetFileInternal(directory, fileName, false).Await();

        /// <summary>
        /// Gets a file.
        /// </summary>
        /// <param name="directory">Directory.</param>
        /// <param name="fileName">Name of the file including file extension.</param>
        /// <returns>File.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<IFile> GetFileAsync(this IDirectory directory, string fileName)
            => GetFileInternal(directory, fileName, true);

        private static async Task<IFile> GetFileInternal(IDirectory directory, string fileName, bool async)
        {
            Guard.NotNull(directory, nameof(directory));

            if (fileName.IsEmpty())
            {
                return null;
            }

            var fs = directory.FileSystem;
            var path = PathUtility.Join(directory.SubPath.AsSpan(), fileName.AsSpan());

            return async ? await fs.GetFileAsync(path) : fs.GetFile(path);
        }
    }
}
