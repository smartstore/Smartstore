using Smartstore.Core.Search.Facets;

namespace Smartstore.Core.Search
{
    public interface ISearchEngine
    {
        /// <summary>
        /// Search query
        /// </summary>
        ISearchQuery Query { get; }

        /// <summary>
        /// Get search hit by entity identifier
        /// </summary>
        /// <param name="id">Entity identifier</param>
        /// <returns>Search hit</returns>
        ISearchHit Get(int id);

        /// <summary>
        /// Get search hit by entity identifier
        /// </summary>
        /// <param name="id">Entity identifier</param>
        /// <returns>Search hit</returns>
        Task<ISearchHit> GetAsync(int id);

        /// <summary>
        /// Get total number of search hits
        /// </summary>
        /// <returns>Total number of search hits</returns>
        int Count();

        /// <summary>
        /// Get total number of search hits
        /// </summary>
        /// <returns>Total number of search hits</returns>
        Task<int> CountAsync();

        /// <summary>
        /// Search
        /// </summary>
        /// <returns>Search hits</returns>
        IEnumerable<ISearchHit> Search();

        /// <summary>
        /// Search
        /// </summary>
        /// <returns>Search hits</returns>
        Task<IEnumerable<ISearchHit>> SearchAsync();

        /// <summary>
        /// Gets the facet map for drilldown navigation
        /// </summary>
        /// <returns>The facet groups</returns>
        IDictionary<string, FacetGroup> GetFacetMap();

        /// <summary>
        /// Gets the facet map for drilldown navigation
        /// </summary>
        /// <returns>Map of <see cref="FacetDescriptor.Key"/> to <see cref="FacetGroup"/>.</returns>
        Task<IDictionary<string, FacetGroup>> GetFacetMapAsync();

        /// <summary>
        /// Gets the number of facets (also known as cardinality).
        /// </summary>
        /// <remarks>
        /// This core faceting method can be invoked extremely often. It is typically called internally by <see cref="ISearchProvider"/>.
        /// </remarks>
        /// <param name="value">The value to be faceted.</param>
        /// <param name="context">Context for counting facets.</param>
        /// <returns>Number of facets. Returns <see cref="long.MaxValue"/> indicating to cancel faceting immediately.</returns>
        long GetFacetCount(FacetValue value, FacetValueContext context);

        /// <summary>
        /// Gets the number of facets (also known as cardinality).
        /// </summary>
        /// <remarks>
        /// This core faceting method can be invoked extremely often. It is typically called internally by <see cref="ISearchProvider"/>.
        /// </remarks>
        /// <param name="value">The value to be faceted.</param>
        /// <param name="context">Context for counting facets.</param>
        /// <returns>Number of facets. Returns <see cref="long.MaxValue"/> indicating to cancel faceting immediately.</returns>
        Task<long> GetFacetCountAsync(FacetValue value, FacetValueContext context);

        /// <summary>
        /// Performs spell checking ("Diy you mean?")
        /// </summary>
        /// <returns>Suggestions/corrections or an empty array</returns>
        string[] CheckSpelling();

        /// <summary>
        /// Performs spell checking ("Diy you mean?")
        /// </summary>
        /// <returns>Suggestions/corrections or an empty array</returns>
        Task<string[]> CheckSpellingAsync();

        /// <summary>
        /// Highlights chosen terms in a text, extracting the most relevant sections
        /// </summary>
        /// <param name="input">Text to highlight terms in</param>
        /// <param name="fieldName">Field name</param>
        /// <param name="preMatch">Text/HTML to prepend to matched keyword</param>
        /// <param name="postMatch">Text/HTML to append to matched keyword</param>
        /// <returns>Highlighted text fragments</returns>
        string Highlight(string input, string fieldName, string preMatch, string postMatch);

        /// <summary>
        /// Gets highlighted text fragments for an entity identifier.
        /// </summary>
        /// <param name="id">Entity identifier.</param>
        /// <param name="fieldName">Field name.</param>
        /// <param name="preMatch">Text/HTML to prepend to matched keyword.</param>
        /// <param name="postMatch">Text/HTML to append to matched keyword.</param>
        /// <param name="numMaxFragments">Maximum number of returned text fragments.</param>
        /// <returns>Highlighted text fragments.</returns>
        string Highlight(int id, string fieldName, string preMatch, string postMatch, int numMaxFragments);
    }
}