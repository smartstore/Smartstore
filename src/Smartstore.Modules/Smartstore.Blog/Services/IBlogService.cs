namespace Smartstore.Blog.Services
{
    /// <summary>
    /// Blog service interface.
    /// </summary>
    public partial interface IBlogService
    {
        /// <summary>
        /// Gets all blog post tags.
        /// </summary>
        /// <param name="storeId">The store identifier. Pass 0 to get all blog posts.</param>
        /// <param name="languageId">Filter by a language identifier. Pass 0 to get all blog posts.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden records.</param>
        /// <returns>Blog post tags.</returns>
        Task<ISet<BlogPostTag>> GetAllBlogPostTagsAsync(int storeId, int languageId = 0, bool includeHidden = false);
    }
}
