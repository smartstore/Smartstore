using System.Threading;
using Smartstore.Core.Identity;

namespace Smartstore.Forums.Services
{
    /// <summary>
    /// Forum service interface.
    /// </summary>
    public partial interface IForumService
    {
        Task<int> DeleteForumSubscriptionsAsync(int[] forumIds, CancellationToken cancelToken = default);
        Task<int> DeleteForumTopicSubscriptionsAsync(int[] forumTopicIds, CancellationToken cancelToken = default);

        Task<int> UpdateForumPostCountsAsync(int[] customerIds, CancellationToken cancelToken = default);
        Task<int> UpdateForumStatisticsAsync(int[] forumIds, CancellationToken cancelToken = default);
        Task<int> UpdateForumTopicStatisticsAsync(int[] forumTopicIds, CancellationToken cancelToken = default);

        string BuildSlug(ForumTopic topic);
        string StripSubject(ForumTopic topic);
        string FormatPostText(ForumPost post);
        string FormatPrivateMessage(PrivateMessage message);

        ForumModerationPermissionFlags GetModerationPermissions(ForumTopic topic = null, ForumPost post = null, Customer customer = null);
    }
}
