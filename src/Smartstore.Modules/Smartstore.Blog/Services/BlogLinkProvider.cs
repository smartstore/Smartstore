using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Blog.Domain;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Widgets;
using Smartstore.Data.Hooks;

namespace Smartstore.Blog.Services
{
    public class BlogLinkProvider : ILinkProvider
    {
        public const string SchemaBlog = "blog";

        private readonly SmartDbContext _db;

        public BlogLinkProvider(SmartDbContext db)
        {
            _db = db;
        }

        public int Order { get; }

        public IEnumerable<LinkBuilderMetadata> GetBuilderMetadata()
        {
            return new[]
            {
                new LinkBuilderMetadata 
                { 
                    Schema = SchemaBlog, 
                    Icon = "fa fa-blog", 
                    ResKey = "Common.Entity.BlogPost", 
                    Widget = new PartialViewWidgetInvoker("BlogLinkBuilder", "Smartstore.Blog") 
                }
            };
        }

        public async Task<LinkTranslationResult> TranslateAsync(LinkExpression expression, int storeId, int languageId)
        {
            if (expression.Schema != SchemaBlog)
            {
                return null;
            }

            var summary = await GetEntityDataAsync(expression, x => new LinkTranslatorEntitySummary
            {
                Name = x.Title,
                Published = x.IsPublished,
                LimitedToStores = x.LimitedToStores,
                PictureId = x.MediaFileId
            });

            return new LinkTranslationResult
            {
                EntitySummary = summary,
                EntityName = nameof(BlogPost),
                EntityType = typeof(BlogPost),
                Status = summary == null
                    ? LinkStatus.NotFound
                    : summary.Published ? LinkStatus.Ok : LinkStatus.Hidden
            };
        }

        private async Task<LinkTranslatorEntitySummary> GetEntityDataAsync(LinkExpression expression, Expression<Func<BlogPost, LinkTranslatorEntitySummary>> selector)
        {
            if (int.TryParse(expression.Target, out var entityId))
            {
                var summary = await _db.BlogPosts()
                    .AsNoTracking()
                    .Where(x => x.Id == entityId)
                    .Select(selector)
                    .SingleOrDefaultAsync();

                if (summary != null)
                {
                    summary.Id = entityId;
                    summary.LocalizedPropertyNames = new[] { "Title" };
                }

                return summary;
            }

            return null;
        }
    }

    internal class BlogLinkInvalidator : DbSaveHook<BlogPost>
    {
        private readonly ILinkResolver _linkResolver;

        public BlogLinkInvalidator(ILinkResolver linkResolver)
        {
            _linkResolver = linkResolver;
        }

        protected override HookResult OnUpdating(BlogPost entity, IHookedEntity entry)
        {
            if (entry.Entry.IsPropertyModified(nameof(BlogPost.IsPublished)))
            {
                _linkResolver.InvalidateLink(BlogLinkProvider.SchemaBlog, entity.Id);
            }

            return HookResult.Ok;
        }

        protected override HookResult OnDeleting(BlogPost entity, IHookedEntity entry)
        {
            _linkResolver.InvalidateLink(BlogLinkProvider.SchemaBlog, entity.Id);
            return HookResult.Ok;
        }
    }
}
