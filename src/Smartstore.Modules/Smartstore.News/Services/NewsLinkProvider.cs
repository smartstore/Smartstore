using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Widgets;
using Smartstore.Data.Hooks;

namespace Smartstore.News.Services
{
    public class NewsLinkProvider : ILinkProvider
    {
        public const string SchemaNews = "news";

        private readonly SmartDbContext _db;

        public NewsLinkProvider(SmartDbContext db)
        {
            _db = db;
        }

        public int Order { get; }

        public IEnumerable<LinkBuilderMetadata> GetBuilderMetadata()
        {
            return new[]
            {
                new LinkBuilderMetadata { 
                    Schema = SchemaNews, 
                    Icon = "far fa-newspaper", 
                    ResKey = "Common.Entity.NewsItem", 
                    Widget = new PartialViewWidgetInvoker("NewsLinkBuilder", "Smartstore.News")  
                }
            };
        }

        public async Task<LinkTranslationResult> TranslateAsync(LinkExpression expression, int storeId, int languageId)
        {
            if (expression.Schema != SchemaNews)
            {
                return null;
            }

            var summary = await GetEntityDataAsync(expression, x => new LinkTranslatorEntitySummary
            {
                Name = x.Title,
                Published = x.Published,
                LimitedToStores = x.LimitedToStores,
                PictureId = x.MediaFileId
            });

            return new LinkTranslationResult
            {
                EntitySummary = summary,
                EntityName = nameof(NewsItem),
                EntityType = typeof(NewsItem),
                Status = summary == null
                    ? LinkStatus.NotFound
                    : summary.Published ? LinkStatus.Ok : LinkStatus.Hidden
            };
        }

        private async Task<LinkTranslatorEntitySummary> GetEntityDataAsync(LinkExpression expression, Expression<Func<NewsItem, LinkTranslatorEntitySummary>> selector)
        {
            if (int.TryParse(expression.Target, out var entityId))
            {
                var summary = await _db.NewsItems()
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

    internal class NewsLinkInvalidator : DbSaveHook<NewsItem>
    {
        private readonly ILinkResolver _linkResolver;

        public NewsLinkInvalidator(ILinkResolver linkResolver)
        {
            _linkResolver = linkResolver;
        }

        protected override HookResult OnUpdating(NewsItem entity, IHookedEntity entry)
        {
            if (entry.Entry.IsPropertyModified(nameof(NewsItem.Published)))
            {
                _linkResolver.InvalidateLink(NewsLinkProvider.SchemaNews, entity.Id);
            }

            return HookResult.Ok;
        }

        protected override HookResult OnDeleting(NewsItem entity, IHookedEntity entry)
        {
            _linkResolver.InvalidateLink(NewsLinkProvider.SchemaNews, entity.Id);
            return HookResult.Ok;
        }
    }
}
