using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.News.Domain;

namespace Smartstore.News.Hooks
{
    [Important]
    internal class NewsCommentHook : AsyncDbSaveHook<NewsComment>
    {
        private readonly SmartDbContext _db;

        public NewsCommentHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override Task<HookResult> OnInsertedAsync(NewsComment entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnDeletedAsync(NewsComment entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        /// <summary>
        /// Update approved comment count after adding or removing a newscomment.
        /// </summary>
        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var comments = entries
                .Select(x => x.Entity)
                .OfType<NewsComment>()
                .ToList();

            var itemIds = comments.Select(x => x.NewsItemId).Distinct().ToArray();

            foreach (var itemId in itemIds)
            {
                var newsItem = await _db.NewsItems().FindByIdAsync(itemId);
                if (newsItem != null)
                {
                    var query = _db.NewsComments();

                    newsItem.ApprovedCommentCount = query.Where(x => x.NewsItemId == itemId && x.IsApproved).Count();
                    newsItem.NotApprovedCommentCount = query.Where(x => x.NewsItemId == itemId && !x.IsApproved).Count();
                }
            }

            await _db.SaveChangesAsync(cancelToken);
        }
    }
}
