#nullable enable

namespace Smartstore.Core.Search
{
    public class SearchTerm
    {
        public SearchTerm()
            : this(string.Empty, Array.Empty<string>())
        {
        }

        public SearchTerm(string term, string field, SearchMode mode = SearchMode.Contains)
            : this(term, new[] { field }, mode)
        {
        }

        public SearchTerm(string term, string[] fields, SearchMode mode = SearchMode.Contains)
        {
            Term = term;
            Fields = fields ?? Array.Empty<string>();
            Mode = mode;
        }

        /// <summary>
        /// Specifies the search term.
        /// </summary>
        public string Term { get; init; }

        /// <summary>
        /// Specifies the fields to be searched.
        /// </summary>
        public string[] Fields { get; init; }

        /// <summary>
        /// Specifies the search mode.
        /// </summary>
        /// <remarks>
        /// Note that the mode has an impact on the performance of the search. <see cref="SearchMode.ExactMatch"/> is the fastest,
        /// <see cref="SearchMode.StartsWith"/> is slower and <see cref="SearchMode.Contains"/> the slowest.
        /// </remarks>
        public SearchMode Mode { get; init; }

        /// <summary>
        /// A value idicating whether to search by distance. For example "roam" finds "foam" and "roams".
        /// Only applicable if the search engine supports it. Note that a fuzzy search is typically slower.
        /// </summary>
        public bool IsFuzzySearch { get; init; }

        /// <summary>
        /// A value indicating whether to escape the search term.
        /// </summary>
        public bool Escape { get; init; }

        public override string ToString()
        {
            return "'{0}' in {1} ({2})".FormatInvariant(
                Term.EmptyNull(),
                string.Join(", ", Fields),
                string.Join(" ", Escape ? "escape" : string.Empty, IsFuzzySearch ? "fuzzy" : Mode.ToString()));
        }
    }
}
