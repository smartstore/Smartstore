using System.Threading;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Forums.Hooks
{
    internal class ForumTopicHook : AsyncDbSaveHook<ForumTopic>
    {
        private readonly SmartDbContext _db;
        private readonly IForumService _forumService;
        private readonly HashSet<int> _postCountCustomerIds = new();
        private readonly HashSet<int> _statisticsForumIds = new();

        public ForumTopicHook(SmartDbContext db, IForumService forumService)
        {
            _db = db;
            _forumService = forumService;
        }

        protected override Task<HookResult> OnUpdatingAsync(ForumTopic entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            // Keep previous forum ID to also update forum statistics of previous forum when moving topic.
            if (entry.State != Data.EntityState.Detached)
            {
                var prop = entry.Entry.Property(nameof(ForumTopic.ForumId));
                if (prop?.OriginalValue != null)
                {
                    _statisticsForumIds.Add((int)prop.OriginalValue);
                }
            }

            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnDeletingAsync(ForumTopic entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Keep customer IDs to update customer post counts after saving.
            var topicIds = entries
                .Where(x => x.State == Data.EntityState.Deleted || x.State == Data.EntityState.Modified)
                .Select(x => x.Entity)
                .OfType<ForumTopic>()
                .ToDistinctArray(x => x.Id);

            if (topicIds.Any())
            {
                var customerIds = await _db.ForumPosts()
                    .Where(x => topicIds.Contains(x.TopicId))
                    .Select(x => x.CustomerId)
                    .ToListAsync(cancelToken);

                _postCountCustomerIds.AddRange(customerIds);
            }
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Delete topic subscriptions.
            var deletedTopicIds = entries
                .Where(x => x.InitialState == Data.EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<ForumTopic>()
                .ToDistinctArray(x => x.Id);

            await _forumService.DeleteForumTopicSubscriptionsAsync(deletedTopicIds, cancelToken);

            // Update post counts of customers.
            await _forumService.UpdateForumPostCountsAsync(_postCountCustomerIds.ToArray(), cancelToken);
            _postCountCustomerIds.Clear();

            // Update topic statistics.
            var modifiedTopicIds = entries
                .Where(x => x.InitialState == Data.EntityState.Modified)
                .Select(x => x.Entity)
                .OfType<ForumTopic>()
                .ToDistinctArray(x => x.Id);

            await _forumService.UpdateForumTopicStatisticsAsync(modifiedTopicIds, cancelToken);

            // Update forum statistics.
            _statisticsForumIds.AddRange(entries
                .Select(x => x.Entity)
                .OfType<ForumTopic>()
                .Select(x => x.ForumId));

            await _forumService.UpdateForumStatisticsAsync(_statisticsForumIds.ToArray(), cancelToken);
            _statisticsForumIds.Clear();
        }
    }
}
