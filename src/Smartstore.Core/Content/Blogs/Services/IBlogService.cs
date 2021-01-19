using System.Collections.Generic;
using System.Threading.Tasks;

namespace Smartstore.Core.Content.Blogs
{
    /// <summary>
    /// Blog service interface.
    /// </summary>
    public partial interface IBlogService
    {
        /// <summary>
        /// Updates blog post comment totals.
        /// </summary>
        Task UpdateCommentTotalsAsync(BlogPost blogPost);

        /// <summary>
        /// Gets all blog post tags.
        /// </summary>
        /// <param name="storeId">The store identifier. Pass 0 to get all blog posts.</param>
        /// <param name="languageId">Filter by a language identifier. Pass 0 to get all blog posts.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden records.</param>
        /// <returns>Blog post tags.</returns>
        Task<IList<BlogPostTag>> GetAllBlogPostTagsAsync(int storeId, int languageId = 0, bool includeHidden = false);
    }
}
