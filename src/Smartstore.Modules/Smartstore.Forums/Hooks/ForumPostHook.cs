using System.Threading;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Forums.Hooks
{
    internal class ForumPostHook : AsyncDbSaveHook<ForumPost>
    {
        private readonly SmartDbContext _db;
        private readonly IForumService _forumService;

        public ForumPostHook(SmartDbContext db, IForumService forumService)
        {
            _db = db;
            _forumService = forumService;
        }

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var posts = entries
                .Select(x => x.Entity)
                .OfType<ForumPost>()
                .ToArray();

            // Update post counts of customers.
            await _forumService.UpdateForumPostCountsAsync(posts.ToDistinctArray(x => x.CustomerId), cancelToken);

            // Update topic statistics.
            await _forumService.UpdateForumTopicStatisticsAsync(posts.ToDistinctArray(x => x.TopicId), cancelToken);

            // Update forum statistics.
            foreach (var post in posts)
            {
                await _db.LoadReferenceAsync(post, x => x.ForumTopic, cancelToken: cancelToken);
            }

            await _forumService.UpdateForumStatisticsAsync(posts.ToDistinctArray(x => x.ForumTopic.ForumId), cancelToken);            
        }
    }
}
