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
    internal class ForumTopicHook : AsyncDbSaveHook<ForumTopic>
    {
        private readonly SmartDbContext _db;
        private readonly IGenericAttributeService _genericAttributeService;

        public ForumTopicHook(SmartDbContext db, IGenericAttributeService genericAttributeService)
        {
            _db = db;
            _genericAttributeService = genericAttributeService;
        }

        protected override Task<HookResult> OnUpdatedAsync(ForumTopic entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnDeletedAsync(ForumTopic entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Update "ForumPostCount" generic attribute.
            var customerIds = entries
                .Select(x => x.Entity)
                .OfType<ForumTopic>()
                .Select(x => x.CustomerId)
                .Distinct()
                .ToArray();

            var numPosts = await _db.ForumPosts().GetForumPostsCountAsync(customerIds);
            if (numPosts.Any())
            {
                var entityName = nameof(Customer);
                await _genericAttributeService.PrefetchAttributesAsync(entityName, customerIds);
                
                foreach (var pair in numPosts)
                {
                    var attributes = _genericAttributeService.GetAttributesForEntity(entityName, pair.Key);

                    // This should actually be stored per store and retrieved as well if the related forum group
                    // is limited to a store. For the sake of simplicity, we'll leave it that way.
                    attributes.Set(Module.ForumPostCountKey, pair.Value);
                }

                await _db.SaveChangesAsync(cancelToken);
            }
        }
    }
}
