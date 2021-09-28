using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Identity;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums.Services
{
    /// <summary>
    /// Forum service interface.
    /// </summary>
    public partial interface IForumService
    {
        Task<int> DeleteSubscriptionsByForumIdsAsync(int[] forumIds, CancellationToken cancelToken = default);

        string BuildSlug(ForumTopic topic);
        string StripSubject(ForumTopic topic);
        string FormatPostText(ForumPost post);

        ForumModerationPermissionFlags GetModerationPermissions(ForumTopic topic = null, ForumPost post = null, Customer customer = null);
    }
}
