using Microsoft.AspNetCore.Http;

namespace Smartstore.Forums.Search.Modelling
{
    public partial interface IForumSearchQueryFactory
    {
        /// <summary>
        /// The last created query instance. The MVC model binder uses this property to avoid repeated binding.
        /// </summary>
        ForumSearchQuery Current { get; }

        /// <summary>
        /// Creates a <see cref="ForumSearchQuery"/> instance from the current <see cref="HttpContext"/> 
        /// by looking up corresponding keys in posted form and/or query string.
        /// </summary>
        /// <returns>The query object.</returns>
        Task<ForumSearchQuery> CreateFromQueryAsync();
    }
}
