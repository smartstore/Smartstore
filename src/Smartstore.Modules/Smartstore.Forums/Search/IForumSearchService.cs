using System.Linq;
using System.Threading.Tasks;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums.Search
{
    /// <summary>
    /// Forum search interface.
    /// </summary>
    public partial interface IForumSearchService
    {
        /// <summary>
        /// Builds a forum post query using LINQ search.
        /// </summary>
        /// <param name="searchQuery">Search term, filters and other parameters used for searching.</param>
        /// <param name="baseQuery">Optional query used to build the forum post query.</param>
        /// <returns>Forum post queryable.</returns>
        IQueryable<ForumPost> PrepareQuery(ForumSearchQuery searchQuery, IQueryable<ForumPost> baseQuery = null);

        /// <summary>
        /// Searches for forum posts.
        /// </summary>
        /// <param name="searchQuery">Search term, filters and other parameters used for searching.</param>
        /// <param name="direct">Bypasses the index provider (if available) and directly searches in the database.</param>
        /// <returns>Forum search result.</returns>
        Task<ForumSearchResult> SearchAsync(ForumSearchQuery searchQuery, bool direct = false);
    }
}
