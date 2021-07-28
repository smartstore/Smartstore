using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.IO;
using Smartstore.Utilities;

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

            var path = directory.FileSystem.PathCombine(directory.SubPath.EmptyNull(), fileName);

            return directory.FileSystem.GetFile(path);
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
            
            var path = directory.FileSystem.PathCombine(directory.SubPath.EmptyNull(), fileName);

            return await directory.FileSystem.GetFileAsync(path);
        }

        ///// <summary>
        ///// Copies the contents of a directory to <paramref name="destination"/> directory.
        ///// Files existing in destination are skipped if their content equal the source files contents.
        ///// </summary>
        ///// <param name="source">The source directory</param>
        ///// <param name="destination">The destination directory</param>
        ///// <param name="ignorePatterns">Path patterns to exclude from copy. Supports * and ? wildcards.</param>
        ///// <returns></returns>
        //public static void CopyContents(this IDirectory source, IDirectory destination, params string[] ignorePatterns)
        //{
        //    Guard.NotNull(source, nameof(source));
        //    Guard.NotNull(destination, nameof(destination));

        //    var ignores = ignorePatterns.Select(x => new Wildcard(x));
        //    var content = GetDirectoryContent(source, ignores);
        //    var fs = source.FileSystem;

        //    foreach (var filePath in content.Files)
        //    {
        //        var sourceFile = source.GetFile(filePath);
        //        var destFile = destination.GetFile(filePath);

        //        // If destination file exist, overwrite only if changed
        //        if (destFile.Exists)
        //        {
        //            if (sourceFile.Length == destFile.Length)
        //            {
        //                using var sourceStream = sourceFile.OpenRead();
        //                using var destStream = destFile.OpenRead();
        //                if (sourceStream.ContentsEqual(destStream, false))
        //                {
        //                    continue;
        //                }  
        //            }
        //        }

        //        fs.TryCreateDirectory(sourceFile.Directory);
        //        fs.CopyFile(sourceFile.SubPath, destFile.SubPath, true);
        //    }
        //}

        //private static DirectoryContent GetDirectoryContent(IDirectory directory, IEnumerable<Wildcard> ignores)
        //{
        //    var files = new List<string>();
        //    GetDirectoryContent(directory, string.Empty, files, ignores);
        //    return new DirectoryContent { Directory = directory, Files = files };
        //}

        //private static void GetDirectoryContent(IDirectory directory, string prefix, List<string> files, IEnumerable<Wildcard> ignores)
        //{
        //    if (!directory.Exists)
        //        return;

        //    if (ignores.Any(w => w.IsMatch(prefix)))
        //        return;

        //    var fs = directory.FileSystem;

        //    foreach (var entry in fs.EnumerateEntries(directory.SubPath))
        //    {
        //        if (entry is IDirectory dir)
        //        {
        //            GetDirectoryContent(dir, fs.PathCombine(prefix, dir.Name), files, ignores);
        //        }
        //        else
        //        {
        //            var path = fs.PathCombine(prefix, entry.Name);
        //            var ignore = ignores.Any(w => w.IsMatch(path));
        //            if (!ignore)
        //            {
        //                files.Add(path);
        //            }
        //        }
        //    }
        //}

        //class DirectoryContent
        //{
        //    public IDirectory Directory { get; set; }
        //    public IEnumerable<string> Files { get; set; }
        //}
    }
}
