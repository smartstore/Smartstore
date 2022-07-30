using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;

namespace Smartstore.IO
{
    public static class PathUtility
    {
        public static readonly char[] PathSeparators = (new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }).Distinct().ToArray();
        public const string CurrentDirectoryToken = ".";
        public const string ParentDirectoryToken = "..";

        private static readonly char[] _invalidPathChars;
        private static readonly char[] _invalidFileNameChars;
        private static readonly char[] _invalidFilterChars;
        private static readonly Regex _invalidCharsPattern;

        static PathUtility()
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
        public static string MakeRelativePath(string fromPath, string toPath, char sep = '\\')
        {
            var fromParts = fromPath.Tokenize(PathSeparators, StringSplitOptions.RemoveEmptyEntries);
            var toParts = toPath.Tokenize(PathSeparators, StringSplitOptions.RemoveEmptyEntries);

            var matchedParts = fromParts
                .Zip(toParts, (x, y) => string.Compare(x, y, true) == 0)
                .TakeWhile(x => x).Count();

            return string.Join(string.Empty, Enumerable.Range(0, fromParts.Count() - matchedParts)
                .Select(x => ".." + sep)) +
                    string.Join(sep, toParts.Skip(matchedParts));
        }

        /// <summary>
        /// Ensures that a path is a valid app relative path by converting
        /// backslashes to forward slashes and optionally removing the leading slash.
        /// </summary>
        /// <param name="path">Relative path to normalize</param>
        /// <param name="ensureLeadingSlash"><c>true</c>: ensures that normalized path starts with '/', <c>false</c>: removes leading '/'.</param>
        /// <returns>Normalized relative path</returns>
        public static string NormalizeRelativePath(string path, bool ensureLeadingSlash = false)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            // Trim whitespace
            var trim = char.IsWhiteSpace(path[0]) || (path.Length > 1 && char.IsWhiteSpace(path[^1]));
            if (trim)
            {
                path = path.Trim();
            }

            // Trim leading ~
            if (path[0] == '~')
            {
                path = path[1..];
            }

            var length = path.Length;
            var hasLeadingSlash = path[0] == '/' || path[0] == '\\';
            var addLeadingSlash = ensureLeadingSlash && !hasLeadingSlash;
            var removeLeadingSlash = !ensureLeadingSlash && hasLeadingSlash;
            var transformSlashes = path.IndexOf('\\') != -1;

            if (!addLeadingSlash && !removeLeadingSlash && !transformSlashes)
            {
                return path;
            }

            if (addLeadingSlash)
            {
                length++;
            }
            else if (removeLeadingSlash)
            {
                length--;
            }

            return string.Create(length, (path, addLeadingSlash, removeLeadingSlash), (span, tuple) =>
            {
                var (pathValue, addLeadingSlashValue, removeLeadingSlashValue) = tuple;
                var spanIndex = 0;

                if (addLeadingSlashValue)
                {
                    span[spanIndex++] = '/';
                }

                int start = removeLeadingSlashValue ? 1 : 0;
                int end = pathValue.Length - 1;

                for (var i = start; i <= end; i++)
                {
                    span[spanIndex++] = pathValue[i] == '\\' ? '/' : pathValue[i];
                }
            });
        }

        /// <summary>
        /// Combines multiple path parts using '/' as directory separator char.
        /// </summary>
        /// <param name="paths">Path parts.</param>
        /// <returns>Combined path</returns>
        public static string Combine(params string[] paths)
        {
            if (paths.Length == 0)
                return null;

            var result = paths[0];

            if (paths.Length == 1 || (paths.Length == 2 && string.IsNullOrEmpty(paths[1])))
            {
                return result;
            }

            for (var i = 1; i < paths.Length; i++)
            {
                result = InternalCombine(result, paths[i]);
            }

            return result;
        }

        private static string InternalCombine(string path, string other)
        {
            Guard.NotNull(path, nameof(path));

            if (string.IsNullOrWhiteSpace(other))
            {
                return path;
            }

            if (other.StartsWith('/') || other.StartsWith('\\'))
            {
                // "other" is already an app-rooted path. Return it as-is.
                return other;
            }

            if (other.Length > 2 && other.Contains("../"))
            {
                return JoinPaths(path, other);
            }

            var index = path.LastIndexOfAny(PathSeparators);
            var result = index != path.Length - 1
                ? path + '/' + other
                : path + other;

            return result;
        }

        private static string JoinPaths(string left, string right)
        {
            var segments = left.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries).ToList();
            var rightSegments = right.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);

            foreach (var segment in rightSegments)
            {
                if (segment == "..")
                {
                    if (segments.Count == 0)
                    {
                        throw new InvalidOperationException($"Path '{right}' navigates above root '{left}'.");
                    }
                    segments.RemoveAt(segments.Count - 1);
                }
                else
                {
                    segments.Add(segment);
                }
            }

            var result = string.Join('/', segments);

            if (PathSeparators.Contains(left[0]))
            {
                result = left[0] + result;
            }

            if (PathSeparators.Contains(right[^1]))
            {
                result += right[^1];
            }

            return result;
        }

        /// <summary>
        /// Replaces all occurences of any illegal path or file name char by <paramref name="replacement"/>
        /// </summary>
        /// <param name="name">Path/File name</param>
        /// <returns>Sanitized path/file name</returns>
        public static string SanitizeFileName(string fileName, string replacement = "-")
        {
            if (fileName.IsEmpty())
            {
                return fileName;
            }

            return _invalidCharsPattern.Replace(fileName, replacement);
        }

        /// <summary>
        /// Replaces all occurences of any illegal path char by <paramref name="replacement"/>
        /// </summary>
        /// <param name="path">path</param>
        /// <returns>Sanitized path</returns>
        public static string SanitizePath(string path, string replacement = "-")
        {
            if (path.IsEmpty())
            {
                return path;
            }

            return string.Join(
                replacement ?? "-",
                path.Tokenize(_invalidPathChars));
        }

        public static bool HasInvalidPathChars(string path, bool checkWildcardChars = false)
        {
            if (path == null)
            {
                return false;
            }

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
            if (!string.IsNullOrEmpty(path) && path[^1] != Path.DirectorySeparatorChar)
            {
                return path.TrimEnd(' ', Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            }

            return path;
        }

        public static bool PathNavigatesAboveRoot(string path)
        {
            var tokenizer = new StringTokenizer(path, PathSeparators);
            int depth = 0;

            foreach (StringSegment segment in tokenizer)
            {
                if (segment.Equals(CurrentDirectoryToken) || segment.Equals(string.Empty))
                {
                    continue;
                }
                else if (segment.Equals(ParentDirectoryToken))
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
            if (path == null)
            {
                return false;
            }

            return Path.IsPathFullyQualified(path);
        }

        internal static bool IsUncSharePath(string path) =>
            (((path.Length > 2) && IsDirectorySeparatorChar(path[0])) && IsDirectorySeparatorChar(path[1]));


        private static bool IsDirectorySeparatorChar(char ch)
            => ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar;
    }
}
