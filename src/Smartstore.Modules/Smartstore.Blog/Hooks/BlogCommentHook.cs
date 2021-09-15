using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Blog.Domain;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Blog.Hooks
{
    [Important]
    internal class BlogCommentHook : AsyncDbSaveHook<BlogComment>
    {
        private readonly SmartDbContext _db;

        public BlogCommentHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override Task<HookResult> OnInsertedAsync(BlogComment entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnDeletedAsync(BlogComment entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        /// <summary>
        /// Update approved comment count after adding or removing a blogcomment.
        /// </summary>
        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var comments = entries
                .Select(x => x.Entity)
                .OfType<BlogComment>()
                .ToList();

            foreach (var comment in comments)
            {
                var blogPost = await _db.BlogPosts().FindByIdAsync(comment.BlogPostId);
                var blogComments = await _db.BlogComments()
                    .AsNoTracking()
                    .Where(x => x.BlogPostId == blogPost.Id)
                    .ToListAsync();

                blogPost.ApprovedCommentCount = blogComments.Where(x => x.IsApproved).Count();
                blogPost.NotApprovedCommentCount = blogComments.Where(x => !x.IsApproved).Count();
            }

            await _db.SaveChangesAsync(cancelToken);
        }
    }
}
