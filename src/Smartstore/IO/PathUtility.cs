#nullable enable

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;

namespace Smartstore.IO
{
    public static class PathUtility
    {
        public static readonly char[] PathSeparators = (new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }).Distinct().ToArray();
        public const string CurrentDirectoryToken = ".";
        public const string ParentDirectoryToken = "..";
        public const string ParentDirectorySegment = "../";

        internal const string SeparatorString = "/";

        private static readonly char[] _invalidPathChars;
        private static readonly SearchValues<char> _invalidFileNameChars;
        private static readonly SearchValues<char> _invalidFilterChars;
        private static readonly Regex _invalidCharsPattern;

        static PathUtility()
        {
            _invalidPathChars = Path.GetInvalidPathChars();
            _invalidFileNameChars = SearchValues.Create(Path.GetInvalidFileNameChars());
            _invalidFilterChars = SearchValues.Create(Path.GetInvalidFileNameChars().Where(c => c != '*' && c != '|' && c != '?').ToArray());
            
            var invalidChars = Regex.Escape(new string(Path.GetInvalidPathChars()) + new string(Path.GetInvalidFileNameChars()));
            _invalidCharsPattern = new Regex(string.Format(@"[{0}]+", invalidChars), RegexOptions.Compiled | RegexOptions.NonBacktracking);
        }

        #region Combine

        /// <summary>
        /// Combines multiple path parts into a single path using '/' as directory separator char.
        /// This method considers rooting and path navigation, means: [ "hello", "/world" ] results in "/world", not "hello/world".
        /// [ "hello/world", "../anotherworld" ] results in "hello/anotherworld".
        /// </summary>
        /// <param name="paths">An array of parts of the path.</param>
        /// <returns>Combined path</returns>
        public static string? Combine(params string?[] paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            if (paths.Length == 0)
            {
                return null;
            } 

            var result = paths[0];

            if (paths.Length == 1 || (paths.Length == 2 && string.IsNullOrEmpty(paths[1])))
            {
                return result;
            }

            for (var i = 1; i < paths.Length; i++)
            {
                result = CombineInternal(result!, paths[i]!);
            }

            return result;
        }

        private static string CombineInternal(string first, string second)
        {
            if (first == null)
            {
                throw new ArgumentNullException(nameof(first));
            }

            if (string.IsNullOrWhiteSpace(second))
            {
                return first;
            }

            if (Path.IsPathRooted(second))
            {
                // "second" is already an app-rooted path. Return it as-is.
                return second;
            }

            if (second.Length > 2 && second.Contains(ParentDirectorySegment, StringComparison.OrdinalIgnoreCase))
            {
                return CombineSlow(first, second);
            }

            var firstNeedsSeparator = first.Length > 0 && !IsDirectorySeparatorChar(first[^1]);

            return firstNeedsSeparator
                ? first + '/' + second
                : first + second;
        }

        /// <summary>
        /// Can handle ../ tokens
        /// </summary>
        private static string CombineSlow(string left, string right)
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

            if (IsDirectorySeparatorChar(left[0]))
            {
                result = left[0] + result;
            }

            if (IsDirectorySeparatorChar(right[^1]))
            {
                result += right[^1];
            }

            return result;
        }

        #endregion

        #region Join

        /// <summary>
        /// Concatenates path components into a single path using '/' as directory separator char.
        /// Unlike Combine(), Join() does not consider rooting or path navigation. 
        /// It simply combines paths, ensuring that there is a directory separator between them.
        /// </summary>
        /// <param name="ensureLeadingSeparator">
        /// <c>true</c>: ensures that normalized path starts with '/', 
        /// <c>false</c>: removes leading '/',
        /// <c>null</c>: leave it as it is.
        /// </param>
        /// <returns>The concatenated path</returns>
        /// <remarks>
        /// Other than Path.Join() this method detects double separators and collapses them.
        /// </remarks>
        public static string Join(string? path1, string? path2, bool? ensureLeadingSeparator = null)
        {
            return Join(path1.AsSpan(), path2.AsSpan(), ensureLeadingSeparator);
        }

        /// <inheritdoc cref="Join(string, string)"/>
        public static string Join(string? path1, string? path2, string? path3, bool? ensureLeadingSeparator = null)
        {
            return Join(path1.AsSpan(), path2.AsSpan(), path3.AsSpan(), ensureLeadingSeparator);
        }

        /// <inheritdoc cref="Join(string, string)"/>
        public static string Join(string? path1, string? path2, string? path3, string? path4, bool? ensureLeadingSeparator = null)
        {
            return Join(path1.AsSpan(), path2.AsSpan(), path3.AsSpan(), path4.AsSpan(), ensureLeadingSeparator);
        }

        /// <inheritdoc cref="Join(string, string)"/>
        public static string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, bool? ensureLeadingSeparator = null)
        {
            ReadOnlySpan<char> trimChars = PathSeparators;

            var joined = NormalizeRelativePath(Path.Join(
                path1,
                path2.TrimStart(trimChars)), ensureLeadingSeparator);

            return joined[0] == '\\' ? '/' + joined[1..] : joined;
        }

        /// <inheritdoc cref="Join(string, string)"/>
        public static string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, bool? ensureLeadingSeparator = null)
        {
            ReadOnlySpan<char> trimChars = PathSeparators;

            var joined = NormalizeRelativePath(Path.Join(
                path1,
                path2.TrimStart(trimChars),
                path3.TrimStart(trimChars)), ensureLeadingSeparator);

            return joined[0] == '\\' ? '/' + joined[1..] : joined;
        }

        /// <inheritdoc cref="Join(string, string)"/>
        public static string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, ReadOnlySpan<char> path4, bool? ensureLeadingSeparator = null)
        {
            ReadOnlySpan<char> trimChars = PathSeparators;

            var joined = NormalizeRelativePath(Path.Join(
                path1, 
                path2.TrimStart(trimChars),
                path3.TrimStart(trimChars),
                path4.TrimStart(trimChars)), ensureLeadingSeparator);

            return joined[0] == '\\' ? '/' + joined[1..] : joined;
        }

        #endregion

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
        /// <param name="ensureLeadingSeparator">
        /// <c>true</c>: ensures that normalized path starts with '/', 
        /// <c>false</c>: removes leading '/',
        /// <c>null</c>: leave it as it is.
        /// </param>
        /// <returns>Normalized relative path</returns>
        public static string NormalizeRelativePath(string path, bool? ensureLeadingSeparator = false)
        {
            return NormalizeRelativePath(path.AsSpan(), ensureLeadingSeparator).ToString();
        }

        /// <inheritdoc cref="NormalizeRelativePath(string, bool?)"/>
        public static ReadOnlySpan<char> NormalizeRelativePath(ReadOnlySpan<char> path, bool? ensureLeadingSeparator = false)
        {
            if (path.Length == 0)
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

            var hasLeadingSeparator = IsDirectorySeparatorChar(path[0]);
            var addLeadingSeparator = ensureLeadingSeparator == true && !hasLeadingSeparator;
            var removeLeadingSeparator = ensureLeadingSeparator == false && hasLeadingSeparator;
            var transformSeparators = path.IndexOf('\\') != -1;

            if (addLeadingSeparator)
            {
                return transformSeparators ? string.Concat("/", ApplySeparatorTransform(path)) : string.Concat("/", path);
            }

            if (removeLeadingSeparator)
            {
                path = path[1..];
            }

            return transformSeparators ? ApplySeparatorTransform(path) : path;

            static ReadOnlySpan<char> ApplySeparatorTransform(ReadOnlySpan<char> value)
            {
                var destination = new char[value.Length];

                for (var i = 0; i < value.Length; i++)
                {
                    destination[i] = value[i] == '\\' ? '/' : value[i];
                }

                return destination;
            }
        }

        /// <summary>
        /// Replaces all occurences of any illegal path or file name char by <paramref name="replacement"/>
        /// </summary>
        /// <param name="name">Path/File name</param>
        /// <returns>Sanitized path/file name</returns>
        [return: NotNullIfNotNull(nameof(fileName))]
        public static string? SanitizeFileName(string? fileName, string replacement = "-")
        {
            if (fileName.IsEmpty())
            {
                return fileName;
            }

            return _invalidCharsPattern.Replace(fileName!, replacement);
        }

        /// <summary>
        /// Replaces all occurences of any illegal path char by <paramref name="replacement"/>
        /// </summary>
        /// <param name="path">path</param>
        /// <returns>Sanitized path</returns>
        [return: NotNullIfNotNull(nameof(path))]
        public static string? SanitizePath(string? path, string replacement = "-")
        {
            if (path.IsEmpty())
            {
                return path;
            }

            return string.Join(
                replacement ?? "-",
                path!.Tokenize(_invalidPathChars));
        }

        public static bool HasInvalidPathChars(string? path, bool checkWildcardChars = false)
        {
            return path != null && HasInvalidPathChars(path.AsSpan(), checkWildcardChars);
        }

        public static bool HasInvalidPathChars(ReadOnlySpan<char> path, bool checkWildcardChars = false)
        {
            if (path.Length == 0)
            {
                return false;
            }

            return path.IndexOfAny(_invalidPathChars) >= 0
                || (checkWildcardChars && ContainsWildcardChars(path, 0));
        }

        public static bool HasInvalidFileNameChars(string? fileName, bool checkWildcardChars = false)
        {
            return fileName != null && HasInvalidFileNameChars(fileName.AsSpan(), checkWildcardChars);
        }

        public static bool HasInvalidFileNameChars(ReadOnlySpan<char> fileName, bool checkWildcardChars = false)
        {
            if (fileName.Length == 0)
            {
                return false;
            }

            return fileName.IndexOfAny(_invalidFileNameChars) >= 0
                || (checkWildcardChars && ContainsWildcardChars(fileName, 0));
        }

        public static bool HasInvalidFilterChars(string? path)
        {
            return path != null && path!.AsSpan().IndexOfAny(_invalidFilterChars) >= 0;
        }

        public static bool HasInvalidFilterChars(ReadOnlySpan<char> path)
        {
            return path.Length > 0 && path.IndexOfAny(_invalidFilterChars) >= 0;
        }

        private static bool ContainsWildcardChars(ReadOnlySpan<char> path, int startIndex = 0)
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

        [return: NotNullIfNotNull(nameof(path))]
        public static string? EnsureTrailingSlash(string? path)
        {
            if (!string.IsNullOrEmpty(path) && path[^1] != Path.DirectorySeparatorChar)
            {
                return path.TrimEnd(' ', Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            }

            return path;
        }

        public static ReadOnlySpan<char> EnsureTrailingSlash(ReadOnlySpan<char> path)
        {
            if (path.Length > 0 && path[^1] != Path.DirectorySeparatorChar)
            {
                var trimChars = " " + Path.AltDirectorySeparatorChar;
                return string.Concat(path.TrimEnd(trimChars.AsSpan()), Path.DirectorySeparatorChar.ToString());
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
        public static bool IsAbsolutePhysicalPath(string? path)
        {
            return path != null && Path.IsPathFullyQualified(path!);
        }

        /// <summary>
        /// Checks whether the given path is a fully qualified absolute path (either UNC or rooted with drive letter)
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns><c>true</c> if path is fully qualified</returns>
        public static bool IsAbsolutePhysicalPath(ReadOnlySpan<char> path)
        {
            return Path.IsPathFullyQualified(path);
        }

        internal static bool IsUncSharePath(string path) =>
            (((path.Length > 2) && IsDirectorySeparatorChar(path[0])) && path[1] == path[0]);

        internal static bool IsUncSharePath(ReadOnlySpan<char> path) =>
            (((path.Length > 2) && IsDirectorySeparatorChar(path[0])) && path[1] == path[0]);

        private static bool IsDirectorySeparatorChar(char c)
            // Swap check order because we mostly deal with forward slashes
            => c == Path.AltDirectorySeparatorChar || c == Path.DirectorySeparatorChar;
    }
}
