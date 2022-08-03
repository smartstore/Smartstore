using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Search.Indexing;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Data.Hooks;
using Smartstore.Events;

namespace Smartstore.Core.Catalog.Categories
{
    public enum CategoryTreeChangeReason
    {
        ElementCounts,
        Data,
        Localization,
        StoreMapping,
        Acl,
        Hierarchy
    }

    public class CategoryTreeChangedEvent
    {
        public CategoryTreeChangedEvent(CategoryTreeChangeReason reason)
        {
            Reason = reason;
        }

        public CategoryTreeChangeReason Reason { get; private set; }
    }

    internal class CategoryTreeChangeHook : AsyncDbSaveHook<BaseEntity>, IConsumer
    {
        #region static

        // Hierarchy affecting category prop names.
        private static readonly string[] _h = new string[]
        {
            nameof(Category.ParentCategoryId),
            nameof(Category.Published),
            nameof(Category.Deleted),
            nameof(Category.DisplayOrder)
        };

        // Visibility affecting category prop names.
        private static readonly string[] _a = new string[]
        {
            nameof(Category.LimitedToStores),
            nameof(Category.SubjectToAcl)
        };

        // Data affecting category prop names.
        private static readonly string[] _d = new string[]
        {
            nameof(Category.Name),
            nameof(Category.Alias),
            nameof(Category.ExternalLink),
            nameof(Category.MediaFileId),
            nameof(Category.BadgeText),
            nameof(Category.BadgeStyle)
        };

        #endregion

        private readonly SmartDbContext _db;
        private readonly ICacheManager _cache;
        private readonly IEventPublisher _eventPublisher;

        private readonly bool[] _handledReasons = new bool[(int)CategoryTreeChangeReason.Hierarchy + 1];
        private bool _invalidated;

        public CategoryTreeChangeHook(
            SmartDbContext db,
            ICacheManager cache,
            IEventPublisher eventPublisher)
        {
            _db = db;
            _cache = cache;
            _eventPublisher = eventPublisher;
        }

        public override async Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            if (_invalidated)
            {
                return HookResult.Ok;
            }

            if (entry.InitialState != EntityState.Modified)
            {
                return HookResult.Void;
            }

            var entity = entry.Entity;

            if (entity is Product)
            {
                var modProps = _db.GetModifiedProperties(entity);
                var toxicPropNames = Product.GetVisibilityAffectingPropertyNames();
                if (modProps.Keys.Any(x => toxicPropNames.Contains(x)))
                {
                    // No eviction, just notification.
                    await PublishEvent(CategoryTreeChangeReason.ElementCounts);
                }
            }
            else if (entity is ProductCategory)
            {
                var modProps = _db.GetModifiedProperties(entity);
                if (modProps.ContainsKey("CategoryId"))
                {
                    // No eviction, just notification.
                    await PublishEvent(CategoryTreeChangeReason.ElementCounts);
                }
            }
            else if (entity is Category category)
            {
                var modProps = _db.GetModifiedProperties(entity);

                if (modProps.Keys.Any(x => _h.Contains(x)))
                {
                    // Hierarchy affecting properties has changed. Nuke each tree.
                    await _cache.RemoveByPatternAsync(CategoryService.CATEGORY_TREE_PATTERN_KEY);
                    await PublishEvent(CategoryTreeChangeReason.Hierarchy);
                    _invalidated = true;
                }
                else if (modProps.Keys.Any(x => _a.Contains(x)))
                {
                    if (modProps.ContainsKey("LimitedToStores"))
                    {
                        // Don't nuke store agnostic trees.
                        await _cache.RemoveByPatternAsync(BuildCacheKeyPattern("*", "*", "[^0]*"));
                        await PublishEvent(CategoryTreeChangeReason.StoreMapping);
                    }
                    if (modProps.ContainsKey("SubjectToAcl"))
                    {
                        // Don't nuke ACL agnostic trees.
                        await _cache.RemoveByPatternAsync(BuildCacheKeyPattern("*", "[^0]*", "*"));
                        await PublishEvent(CategoryTreeChangeReason.Acl);
                    }
                }
                else if (modProps.Keys.Any(x => _d.Contains(x)))
                {
                    // Only data has changed. Don't nuke trees, update corresponding cache entries instead.
                    var keys = _cache.Keys(CategoryService.CATEGORY_TREE_PATTERN_KEY).ToArray();
                    foreach (var key in keys)
                    {
                        var tree = await _cache.GetAsync<TreeNode<ICategoryNode>>(key);
                        if (tree != null)
                        {
                            var node = tree.SelectNodeById(entity.Id);
                            if (node != null)
                            {
                                if (node.Value is CategoryNode value)
                                {
                                    value.Name = category.Name;
                                    value.ExternalLink = category.ExternalLink;
                                    value.Alias = category.Alias;
                                    value.MediaFileId = category.MediaFileId;
                                    value.BadgeText = category.BadgeText;
                                    value.BadgeStyle = category.BadgeStyle;

                                    // Persist to cache store.
                                    await _cache.PutAsync(key, tree, new CacheEntryOptions().ExpiresIn(CategoryService.CategoryTreeCacheDuration));
                                }
                                else
                                {
                                    // Cannot update. Nuke tree.
                                    await _cache.RemoveAsync(key);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                return HookResult.Void;
            }

            return HookResult.Ok;
        }

        public override async Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            if (_invalidated)
            {
                return HookResult.Ok;
            }

            // INFO: Acl & StoreMapping affect element counts.

            var isNewOrDeleted = entry.InitialState == EntityState.Added || entry.InitialState == EntityState.Deleted;
            var entity = entry.Entity;

            if (entity is Product product)
            {
                // INFO: 'Modified' case already handled in 'OnBeforeSave()'.
                if (entry.InitialState == EntityState.Deleted || (entry.InitialState == EntityState.Added && product.Published))
                {
                    // No eviction, just notification, but for PUBLISHED products only.
                    await PublishEvent(CategoryTreeChangeReason.ElementCounts);
                }
            }
            else if (entity is ProductCategory && isNewOrDeleted)
            {
                // INFO: 'Modified' case already handled in 'OnBeforeSave()'
                // New or deleted product category mappings affect counts
                await PublishEvent(CategoryTreeChangeReason.ElementCounts);
            }
            else if (entity is Category && isNewOrDeleted)
            {
                // INFO: 'Modified' case already handled in 'OnBeforeSave()'.
                // Hierarchy affecting change, nuke all.
                await _cache.RemoveByPatternAsync(CategoryService.CATEGORY_TREE_PATTERN_KEY);
                await PublishEvent(CategoryTreeChangeReason.Hierarchy);
                _invalidated = true;
            }
            else if (entity is Setting setting)
            {
                var name = setting.Name.ToLowerInvariant();
                if (name == "catalogsettings.showcategoryproductnumber" || name == "catalogsettings.showcategoryproductnumberincludingsubcategories")
                {
                    await PublishEvent(CategoryTreeChangeReason.ElementCounts);
                }
            }
            else if (entity is Language && entry.InitialState == EntityState.Deleted)
            {
                await PublishEvent(CategoryTreeChangeReason.Localization);
            }
            else if (entity is LocalizedProperty lp)
            {
                var key = lp.LocaleKey;
                if (lp.LocaleKeyGroup == "Category" && (key == "Name" || key == "BadgeText"))
                {
                    await PublishEvent(CategoryTreeChangeReason.Localization);
                }
            }
            else if (entity is StoreMapping stm)
            {
                if (stm.EntityName == "Product")
                {
                    await PublishEvent(CategoryTreeChangeReason.ElementCounts);
                }
                else if (stm.EntityName == "Category")
                {
                    // Don't nuke store agnostic trees.
                    await _cache.RemoveByPatternAsync(BuildCacheKeyPattern("*", "*", "[^0]*"));
                    await PublishEvent(CategoryTreeChangeReason.StoreMapping);
                }
            }
            else if (entity is AclRecord acl)
            {
                if (!acl.IsIdle)
                {
                    if (acl.EntityName == "Product")
                    {
                        await PublishEvent(CategoryTreeChangeReason.ElementCounts);
                    }
                    else if (acl.EntityName == "Category")
                    {
                        // Don't nuke ACL agnostic trees.
                        await _cache.RemoveByPatternAsync(BuildCacheKeyPattern("*", "[^0]*", "*"));
                        await PublishEvent(CategoryTreeChangeReason.Acl);
                    }
                }
            }
            else
            {
                return HookResult.Void;
            }

            return HookResult.Ok;
        }

        public async Task HandleEventAsync(IndexingCompletedEvent message)
        {
            if (message.IndexInfo.IsModified)
            {
                await PublishEvent(CategoryTreeChangeReason.ElementCounts);
            }
        }

        private async Task PublishEvent(CategoryTreeChangeReason reason)
        {
            if (_handledReasons[(int)reason] == false)
            {
                await _eventPublisher.PublishAsync(new CategoryTreeChangedEvent(reason));
                _handledReasons[(int)reason] = true;
            }
        }

        private static string BuildCacheKeyPattern(string includeHiddenToken = "*", string rolesToken = "*", string storeToken = "*")
        {
            return CategoryService.CATEGORY_TREE_KEY.FormatInvariant(includeHiddenToken, rolesToken, storeToken);
        }
    }
}
