using System.Threading;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Forums.Hooks
{
    internal class ForumGroupHook : AsyncDbSaveHook<ForumGroup>
    {
        private readonly SmartDbContext _db;
        private readonly IForumService _forumService;
        private readonly HashSet<int> _deletingForumIds = new();
        private readonly HashSet<int> _postCountCustomerIds = new();

        public ForumGroupHook(SmartDbContext db, IForumService forumService)
        {
            _db = db;
            _forumService = forumService;
        }

        protected override async Task<HookResult> OnDeletingAsync(ForumGroup entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await _db.LoadCollectionAsync(entity, x => x.Forums, cancelToken: cancelToken);

            _deletingForumIds.AddRange(entity.Forums.Select(x => x.Id));

            return HookResult.Ok;
        }

        protected override Task<HookResult> OnDeletedAsync(ForumGroup entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_deletingForumIds.Any())
            {
                // Delete forum subscriptions.
                await _forumService.DeleteForumSubscriptionsAsync(_deletingForumIds.ToArray(), cancelToken);

                // Keep related customerIDs (could be a lot of IDs loaded here).
                _postCountCustomerIds.AddRange(await _db.ForumPosts()
                    .Where(x => _deletingForumIds.Contains(x.ForumTopic.ForumId))
                    .Select(x => x.CustomerId)
                    .Distinct()
                    .ToListAsync(cancelToken));
            }
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Update post counts of customers.
            await _forumService.UpdateForumPostCountsAsync(_postCountCustomerIds.ToArray(), cancelToken);
            _postCountCustomerIds.Clear();
        }
    }
}
