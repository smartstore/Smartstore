using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Data.Hooks;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums.Hooks
{
    internal class ForumPostHook : AsyncDbSaveHook<ForumPost>
    {
        private readonly SmartDbContext _db;
        private readonly IGenericAttributeService _genericAttributeService;

        public ForumPostHook(SmartDbContext db, IGenericAttributeService genericAttributeService)
        {
            _db = db;
            _genericAttributeService = genericAttributeService;
        }

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Update "ForumPostCount" generic attribute.
            var posts = entries
                .Select(x => x.Entity)
                .OfType<ForumPost>();

            var customerIds = posts
                .Select(x => x.CustomerId)
                .Distinct()
                .ToArray();

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

                await _db.SaveChangesAsync(cancelToken);
            }

            // Update topic statistics.
            var topicIds = posts
                .Select(x => x.TopicId)
                .Distinct()
                .ToArray();

            if (await _db.ForumPosts().ApplyStatisticsAsync(topicIds, cancelToken) > 0)
            {
                await _db.SaveChangesAsync(cancelToken);
            }

            // Update forum statistics.
            foreach (var post in posts)
            {
                await _db.LoadReferenceAsync(post, x => x.ForumTopic, cancelToken: cancelToken);
            }
            
            var forumIds = posts
                .Select(x => x.ForumTopic.ForumId)
                .Distinct()
                .ToArray();

            if (await _db.Forums().ApplyStatisticsAsync(forumIds, cancelToken) > 0)
            {
                await _db.SaveChangesAsync(cancelToken);
            }
        }
    }
}
