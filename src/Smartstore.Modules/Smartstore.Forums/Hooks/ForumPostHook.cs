using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
            var postSet = _db.ForumPosts();

            // Update "ForumPostCount" generic attribute.
            var posts = entries
                .Select(x => x.Entity)
                .OfType<ForumPost>();

            var customerIds = posts
                .Select(x => x.CustomerId)
                .Distinct()
                .ToArray();

            var numPosts = await postSet.GetForumPostsCountAsync(customerIds);
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

            // Update topic statistics.
            var topicIds = posts
                .Select(x => x.TopicId)
                .Distinct()
                .ToArray();

            if (topicIds.Any())
            {
                var lastPosts = await postSet
                    .ApplyLastPostFilter(topicIds)
                    .ToListAsync(cancelToken);

                //var query =
                //    from ft in _db.ForumTopics()
                //    join fp in postSet on ft.Id equals fp.TopicId
                //    where topicIds.Contains(ft.Id) && ft.Published && fp.Published
                //    select

                var postCountQuery =
                    from fp in postSet
                    where topicIds.Contains(fp.TopicId) && fp.Published && fp.ForumTopic.Published
                    group fp by fp.TopicId into grp
                    select new
                    {
                        TopicId = grp.Key,
                        PostCount = grp.Count()
                    };

                var postCounts = await postCountQuery
                    .ToDictionaryAsync(x => x.TopicId, cancelToken);

                foreach (var lastPost in lastPosts)
                {
                    lastPost.ForumTopic.LastPostId = lastPost.Id;
                    lastPost.ForumTopic.LastPostCustomerId = lastPost.CustomerId;
                    lastPost.ForumTopic.LastPostTime = lastPost.CreatedOnUtc;

                    // TODO: (mg) (core) lastPost.ForumTopic.NumPosts
                }

                await _db.SaveChangesAsync(cancelToken);
            }
        }
    }
}
