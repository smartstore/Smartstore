#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Smartstore.Core.Seo.Routing
{
    public class ReservedSlug
    {
        public string Slug { get; init; } = default!;
        public bool IsPrefix { get; init; }
    }

    /// <summary>
    /// Provides a table with reserved system slugs.
    /// </summary>
    public interface IRouteHelper
    {
        /// <summary>
        /// Checks whether the given <paramref name="slug"/> is reserved by the system.
        /// </summary>
        /// <param name="slug">The slug to check.</param>
        bool IsReservedSlug(string slug);

        /// <summary>
        /// Checks whether the given <paramref name="slug"/> is reserved by the system.
        /// </summary>
        /// <param name="slug">The slug to check.</param>
        /// <param name="partialMatch">If a substring of <paramref name="slug"/> is matched from left, then this is the matched partial, otherwise <c>null</c>.</param>
        bool IsReservedSlug(string slug, [MaybeNullWhen(false)] out string? partialMatch);

        /// <summary>
        /// Enumerates all reserved slugs.
        /// </summary>
        IEnumerable<ReservedSlug> EnumerateReservedSlugs();

        /// <summary>
        /// Enumerates all paths that are disallowed for robots. A path is
        /// disallowed if the corresponding action is decorated with
        /// the <see cref="DisallowRobotAttribute"/> attribute.
        /// </summary>
        IEnumerable<string> EnumerateDisallowedRobotPaths();
    }
}