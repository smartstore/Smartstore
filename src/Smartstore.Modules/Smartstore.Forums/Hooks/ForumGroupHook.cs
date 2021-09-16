using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Data;
using Smartstore.Data.Batching;
using Smartstore.Data.Hooks;
using Smartstore.Forums.Domain;
using Smartstore.Forums.Services;

namespace Smartstore.Forums.Hooks
{
    internal class ForumGroupHook : AsyncDbSaveHook<ForumGroup>
    {
        private readonly SmartDbContext _db;
        private readonly IForumService _forumService;
        private readonly HashSet<int> _deletedForumIds = new();

        public ForumGroupHook(SmartDbContext db, IForumService forumService)
        {
            _db = db;
            _forumService = forumService;
        }

        protected override async Task<HookResult> OnDeletingAsync(ForumGroup entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await _db.LoadCollectionAsync(entity, x => x.Forums, cancelToken: cancelToken);

            _deletedForumIds.AddRange(entity.Forums.Select(x => x.Id));

            return HookResult.Ok;
        }

        public override async Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Delete stuff of forums that were deleted by referential integrity.
            if (_deletedForumIds.Any())
            {
                await _forumService.DeleteSubscriptionsByForumIdsAsync(_deletedForumIds.ToArray(), cancelToken);

                await _db.LocalizedProperties
                    .Where(x => _deletedForumIds.Contains(x.EntityId) && x.LocaleKeyGroup == nameof(Forum))
                    .BatchDeleteAsync(cancelToken);

                _deletedForumIds.Clear();
            }
        }
    }
}
