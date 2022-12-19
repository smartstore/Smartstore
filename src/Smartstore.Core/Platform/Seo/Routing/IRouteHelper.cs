#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Smartstore.Core.Seo.Routing
{
    public readonly struct ReservedPath
    {
        public ReservedPath(string path, bool isPrefix)
        {
            Path = path;
            IsPrefix = isPrefix;
        }

        public string Path { get; }
        public bool IsPrefix { get; }
    }

    /// <summary>
    /// Provides a table with reserved system slugs.
    /// </summary>
    public interface IRouteHelper
    {
        /// <summary>
        /// Checks whether the given <paramref name="path"/> is reserved by the system.
        /// </summary>
        /// <param name="path">The path to check.</param>
        bool IsReservedPath(string path);

        /// <summary>
        /// Checks whether the given <paramref name="path"/> is reserved by the system.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <param name="partialMatch">If a substring of <paramref name="path"/> is matched from left, then this is the matched partial, otherwise <c>null</c>.</param>
        bool IsReservedPath(string path, [MaybeNullWhen(false)] out string? partialMatch);

        /// <summary>
        /// Enumerates all reserved paths.
        /// </summary>
        IEnumerable<ReservedPath> EnumerateReservedPaths();

        /// <summary>
        /// Enumerates all paths that are disallowed for robots. A path is
        /// disallowed if the corresponding action is decorated with
        /// the <see cref="DisallowRobotAttribute"/> attribute.
        /// </summary>
        IEnumerable<string> EnumerateDisallowedRobotPaths();
    }
}