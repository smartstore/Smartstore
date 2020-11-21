using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;

namespace Smartstore.Utilities
{
    public static class PathHelper
    {
        internal static readonly char[] PathSeparators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        private static readonly char[] _invalidPathChars;
        private static readonly char[] _invalidFileNameChars;
        private static readonly char[] _invalidFilterChars;
        private static readonly Regex _invalidCharsPattern;

        static PathHelper()
        {
            _invalidPathChars = Path.GetInvalidPathChars();
            _invalidFileNameChars = Path.GetInvalidFileNameChars();
            _invalidFilterChars = _invalidFileNameChars.Where(c => c != '*' && c != '|' && c != '?').ToArray();

            var invalidChars = Regex.Escape(new string(_invalidPathChars) + new string(_invalidFileNameChars));
            _invalidCharsPattern = new Regex(string.Format(@"[{0}]+", invalidChars));
        }

        /// <summary>
        /// Checks whether path is empty or starts with '/' or '\'
        /// </summary>
        public static bool IsRootedPath(string basepath)
        {
            return (string.IsNullOrEmpty(basepath) || PathSeparators.Any(x => x == basepath[0]));
        }

        /// <summary>
        /// Determines the relative path from <paramref name="fromPath"/> to <paramref name="toPath"/>
        /// </summary>
        /// <param name="fromPath">From path</param>
        /// <param name="toPath">To path</param>
        /// <param name="sep">Directory separator</param>
        /// <returns>The relative path</returns>
        public static string MakeRelativePath(string fromPath, string toPath, string sep = "\\")
        {
            var fromParts = fromPath.Tokenize(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var toParts = toPath.Tokenize(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            var matchedParts = fromParts
                .Zip(toParts, (x, y) => string.Compare(x, y, true) == 0)
                .TakeWhile(x => x).Count();

            return string.Join("", Enumerable.Range(0, fromParts.Count() - matchedParts)
                .Select(x => ".." + sep)) +
                    string.Join(sep, toParts.Skip(matchedParts));
        }

        /// <summary>
        /// Ensures that a path is a valid app relative path by replacing '\' with '/'
        /// and trimming '/'
        /// </summary>
        /// <param name="path">Relative path</param>
        /// <returns>Normalized relative path</returns>
        public static string NormalizeRelativePath(string path)
        {
            if (path.IsEmpty())
                return path;

            return path.Replace('\\', '/').Trim('/');
        }

        /// <summary>
        /// Checks whether a path is a safe app root path.
        /// </summary>
        /// <param name="path">Relative path</param>
        public static bool IsSafeAppRootPath_TODO(string path)
        {
            // TODO: (core) Refactor
            //if (path.EmptyNull().Length > 2 && !path.IsCaseInsensitiveEqual("con") && !HasInvalidPathChars(path))
            //{
            //    try
            //    {
            //        var mappedPath = CommonHelper.MapPath(path);
            //        var appPath = CommonHelper.MapPath("~/");
            //        return !mappedPath.IsCaseInsensitiveEqual(appPath);
            //    }
            //    catch { }
            //}

            return false;
        }

        /// <summary>
        /// Replaces all occurences of any illegal path or file name char by '-'
        /// </summary>
        /// <param name="name">Path/File name</param>
        /// <returns>Sanitized path/file name</returns>
        public static string SanitizeFileName(string name)
        {
            if (name.IsEmpty())
                return name;

            return _invalidCharsPattern.Replace(name, "-");
        }

        public static bool HasInvalidPathChars(string path, bool checkWildcardChars = false)
        {
            if (path == null)
                return false;

            return path.IndexOfAny(_invalidPathChars) >= 0
                || (checkWildcardChars && ContainsWildcardChars(path, 0));
        }

        public static bool HasInvalidFileNameChars(string fileName, bool checkWildcardChars = false)
        {
            if (fileName == null)
                return false;

            return fileName.IndexOfAny(_invalidFileNameChars) >= 0
                || (checkWildcardChars && ContainsWildcardChars(fileName, 0));
        }

        public static bool HasInvalidFilterChars(string path)
        {
            if (path == null)
                return false;

            return path.IndexOfAny(_invalidFilterChars) >= 0;
        }

        private static bool ContainsWildcardChars(string path, int startIndex = 0)
        {
            for (int i = startIndex; i < path.Length; i++)
            {
                switch (path[i])
                {
                    case '*':
                    case '?':
                        return true;
                }
            }

            return false;
        }

        public static string EnsureTrailingSlash(string path)
        {
            if (!string.IsNullOrEmpty(path) && path[path.Length - 1] != Path.DirectorySeparatorChar)
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }

        public static bool PathNavigatesAboveRoot(string path)
        {
            var tokenizer = new StringTokenizer(path, PathSeparators);
            int depth = 0;

            foreach (StringSegment segment in tokenizer)
            {
                if (segment.Equals(".") || segment.Equals(""))
                {
                    continue;
                }
                else if (segment.Equals(".."))
                {
                    depth--;

                    if (depth == -1)
                    {
                        return true;
                    }
                }
                else
                {
                    depth++;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the given path is a fully qualified absolute path (either UNC or rooted with drive letter)
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns><c>true</c> if path is fully qualified</returns>
        public static bool IsAbsolutePhysicalPath(string path)
        {
            if ((path == null) || (path.Length < 3))
            {
                return false;
            }

            return (((path[1] == ':') && IsDirectorySeparatorChar(path[2])) || IsUncSharePath(path));
        }

        internal static bool IsUncSharePath(string path) =>
            (((path.Length > 2) && IsDirectorySeparatorChar(path[0])) && IsDirectorySeparatorChar(path[1]));


        private static bool IsDirectorySeparatorChar(char ch)
        {
            if (ch != '\\')
            {
                return (ch == '/');
            }

            return true;
        }
    }
}
