using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Data.Hooks;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums.Hooks
{
    internal class CustomerHook : AsyncDbSaveHook<Customer>
    {
        private readonly SmartDbContext _db;
        private readonly Lazy<IGdprTool> _gdprTool;

        public CustomerHook(SmartDbContext db, Lazy<IGdprTool> gdprTool)
        {
            _db = db;
            _gdprTool = gdprTool;
        }

        protected override Task<HookResult> OnUpdatedAsync(Customer entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var softDeletedCustomerIds = entries
                .Where(x => x.IsSoftDeleted == true)
                .Select(x => x.Entity)
                .OfType<Customer>()
                .Select(x => x.Id)
                .ToList();

            if (softDeletedCustomerIds.Any())
            {
                var forumPostsSet = _db.ForumPosts();
                var pager = forumPostsSet
                    .Where(x => softDeletedCustomerIds.Contains(x.CustomerId))
                    .OrderBy(x => x.Id)
                    .ToFastPager();

                while ((await pager.ReadNextPageAsync<ForumPost>(cancelToken)).Out(out var posts))
                {
                    foreach (var post in posts)
                    {
                        _gdprTool.Value.AnonymizeData(post, x => x.IPAddress, IdentifierDataType.IpAddress);
                    }

                    await _db.SaveChangesAsync(cancelToken);
                }
            }
        }
    }
}
