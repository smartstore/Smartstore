using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums.Services
{
    /// <summary>
    /// Forum service interface.
    /// </summary>
    public partial interface IForumService
    {
        Task<int> DeleteSubscriptionsByForumIdsAsync(int[] forumIds, CancellationToken cancelToken = default);

        string BuildSlug(ForumTopic forumTopic);
    }
}
