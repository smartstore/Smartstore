using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Seo;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

namespace Smartstore.Core.Content.Menus.Hooks
{
    public partial class LinkResolverInvalidator : AsyncDbSaveHook<BaseEntity>
    {
        private readonly SmartDbContext _db;
        private readonly ICacheManager _cache;

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

        public LinkResolverInvalidator(SmartDbContext db, ICacheManager cache)
        {
            _db = db;
            _cache = cache;
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
                await _cache.RemoveByPatternAsync(BuildPatternKey("topic", t.Id));
                await _cache.RemoveByPatternAsync(BuildPatternKey("topic", t.SystemName));
            }
            else if (e is Category || e is Product || e is Manufacturer)
            {
                await _cache.RemoveByPatternAsync(BuildPatternKey(e.GetEntityName().ToLowerInvariant(), e.Id));
            }
            else if (e is UrlRecord ur)
            {
                await _cache.RemoveByPatternAsync(BuildPatternKey(ur.EntityName.ToLowerInvariant(), ur.EntityId));
                evictTopicId = ur.EntityId;
            }
            else if (e is StoreMapping sm)
            {
                await _cache.RemoveByPatternAsync(BuildPatternKey(sm.EntityName.ToLowerInvariant(), sm.EntityId));
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
                    await _cache.RemoveByPatternAsync(BuildPatternKey("topic", systemName));
                }
            }

            return HookResult.Ok;
        }

        private static string BuildPatternKey(string entityName, object ident)
        {
            return LinkResolver.LINKRESOLVER_PATTERN_KEY.FormatInvariant(string.Concat(entityName, ":", ident));
        }
    }
}
