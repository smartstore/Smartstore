using System.Threading.Tasks;

namespace Smartstore.Core.Content.News
{
    /// <summary>
    /// News service interface.
    /// </summary>
    public partial interface INewsService
    {
        /// <summary>
        /// Updates news item comment totals.
        /// </summary>
        Task UpdateCommentTotalsAsync(NewsItem newsItem);
    }
}
