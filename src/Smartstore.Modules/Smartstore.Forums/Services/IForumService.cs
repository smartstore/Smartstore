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

        bool IsAllowedToCreateTopic(Customer customer = null);
        bool IsAllowedToEditTopic(ForumTopic topic, Customer customer = null);
        bool IsAllowedToMoveTopic(Customer customer = null);
        bool IsAllowedToDeleteTopic(ForumTopic topic, Customer customer = null);
        bool IsAllowedToCreatePost(Customer customer = null);
        bool IsAllowedToEditPost(ForumPost post, Customer customer = null);
        bool IsAllowedToDeletePost(ForumPost post, Customer customer = null);
    }
}
