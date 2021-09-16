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

            var postIds = comments.Select(x => x.BlogPostId).Distinct().ToArray();

            foreach (var postId in postIds)
            {
                var blogPost = await _db.BlogPosts().FindByIdAsync(postId);
                if (blogPost != null)
                {
                    var query = _db.BlogComments();

                    blogPost.ApprovedCommentCount = query.Where(x => x.BlogPostId == postId && x.IsApproved).Count();
                    blogPost.NotApprovedCommentCount = query.Where(x => x.BlogPostId == postId && !x.IsApproved).Count();
                }
            }

            await _db.SaveChangesAsync(cancelToken);
        }
    }
}
