using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Data.Batching;
using Smartstore.Data.Hooks;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums.Hooks
{
    internal class ForumTopicHook : AsyncDbSaveHook<ForumTopic>
    {
        private readonly SmartDbContext _db;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly HashSet<int> _postCountCustomerIds = new();

        public ForumTopicHook(SmartDbContext db, IGenericAttributeService genericAttributeService)
        {
            _db = db;
            _genericAttributeService = genericAttributeService;
        }

        protected override Task<HookResult> OnUpdatingAsync(ForumTopic entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

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
                .Select(x => x.Id)
                .Distinct()
                .ToArray();

            if (topicIds.Any())
            {
                var customerIds = await _db.ForumPosts()
                    .Where(x => topicIds.Contains(x.TopicId))
                    .Select(x => x.CustomerId)
                    .ToListAsync(cancellationToken: cancelToken);

                _postCountCustomerIds.AddRange(customerIds);
            }
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            await DeleteTopicSubscriptions(entries, cancelToken);
            await UpdateStatistics(entries, cancelToken);
            await UpdateForumPostCounts(cancelToken);
        }

        private async Task<int> DeleteTopicSubscriptions(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedTopicIds = entries
                .Where(x => x.InitialState == Data.EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<ForumTopic>()
                .Select(x => x.Id)
                .Distinct()
                .ToArray();

            if (deletedTopicIds.Any())
            {
                return await _db.ForumSubscriptions()
                    .Where(x => deletedTopicIds.Contains(x.TopicId))
                    .BatchDeleteAsync(cancelToken);
            }

            return 0;
        }

        private async Task<int> UpdateStatistics(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var num = 0;

            // Update topic statistics.
            var modifiedTopicIds = entries
                .Where(x => x.InitialState == Data.EntityState.Modified)
                .Select(x => x.Entity)
                .OfType<ForumTopic>()
                .Select(x => x.Id)
                .Distinct()
                .ToArray();

            if (await _db.ForumPosts().ApplyStatisticsAsync(modifiedTopicIds, cancelToken) > 0)
            {
                num += await _db.SaveChangesAsync(cancelToken);
            }

            // Update forum statistics.
            var forumIds = entries
                .Select(x => x.Entity)
                .OfType<ForumTopic>()
                .Select(x => x.ForumId)
                .Distinct()
                .ToArray();

            if (await _db.Forums().ApplyStatisticsAsync(forumIds, cancelToken) > 0)
            {
                num += await _db.SaveChangesAsync(cancelToken);
            }

            return num;
        }

        private async Task<int> UpdateForumPostCounts(CancellationToken cancelToken)
        {
            var num = 0;
            var customerIds = _postCountCustomerIds.ToArray();
            var numPostsByCustomer = await _db.ForumPosts().GetForumPostCountsByCustomerIdsAsync(customerIds, cancelToken);

            if (numPostsByCustomer.Any())
            {
                var entityName = nameof(Customer);
                await _genericAttributeService.PrefetchAttributesAsync(entityName, customerIds);

                foreach (var pair in numPostsByCustomer)
                {
                    var attributes = _genericAttributeService.GetAttributesForEntity(entityName, pair.Key);

                    // This should actually be stored per store and retrieved as well if the related forum group
                    // is limited to a store. For the sake of simplicity, we'll leave it that way.
                    attributes.Set(Module.ForumPostCountKey, pair.Value);
                }

                num = await _db.SaveChangesAsync(cancelToken);
            }

            _postCountCustomerIds.Clear();

            return num;
        }
    }
}
