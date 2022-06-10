using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Data;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Content.Menus.Hooks
{
    internal partial class DefaultLinkInvalidator : AsyncDbSaveHook<BaseEntity>
    {
        private readonly SmartDbContext _db;
        private readonly ILinkResolver _linkResolver;

        private static readonly HashSet<string> _toxicProps = new()
        {
            nameof(Topic.SystemName),
            nameof(Topic.IsPublished),
            nameof(Topic.SubjectToAcl),
            nameof(Topic.LimitedToStores),
            nameof(Topic.Title),
            nameof(Topic.ShortTitle),
            nameof(Topic.Intro),
            nameof(Product.Name),
            nameof(Product.Deleted),
            nameof(Product.MainPictureId),
            nameof(Category.Published),
            nameof(Category.MediaFileId),
            nameof(StoreMapping.StoreId),
            nameof(UrlRecord.IsActive),
            nameof(UrlRecord.Slug)
        };

        public DefaultLinkInvalidator(SmartDbContext db, ILinkResolver linkResolver)
        {
            _db = db;
            _linkResolver = linkResolver;
        }

        public override async Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            var e = entry.Entity;

            var evict = e is Topic || e is Product || e is Category || e is Manufacturer || e is StoreMapping || e is UrlRecord;
            if (!evict)
                return HookResult.Void; // Perf

            if (evict && entry.InitialState == Smartstore.Data.EntityState.Modified)
            {
                var modProps = entry.Entry.GetModifiedProperties();
                evict = modProps.Keys.Any(x => _toxicProps.Contains(x));
            }

            if (!evict)
            {
                return HookResult.Ok;
            }

            int? evictTopicId = null;

            if (e is Topic t)
            {
                _linkResolver.InvalidateLink("topic", t.Id);
                _linkResolver.InvalidateLink("topic", t.SystemName);
            }
            else if (e is Category || e is Product || e is Manufacturer)
            {
                _linkResolver.InvalidateLink(e.GetEntityName().ToLowerInvariant(), e.Id);
            }
            else if (e is UrlRecord ur)
            {
                _linkResolver.InvalidateLink(ur.EntityName.ToLowerInvariant(), ur.EntityId);
                evictTopicId = ur.EntityId;
            }
            else if (e is StoreMapping sm)
            {
                _linkResolver.InvalidateLink(sm.EntityName.ToLowerInvariant(), sm.EntityId);
                evictTopicId = sm.EntityId;
            }

            if (evictTopicId.HasValue)
            {
                var systemName = await _db.Set<Topic>()
                    .Where(x => x.Id == evictTopicId.Value)
                    .Select(x => x.SystemName)
                    .FirstOrDefaultAsync(cancellationToken: cancelToken);

                if (systemName.HasValue())
                {
                    _linkResolver.InvalidateLink("topic", systemName);
                }
            }

            return HookResult.Ok;
        }
    }
}
