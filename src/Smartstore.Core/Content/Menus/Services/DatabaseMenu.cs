using AngleSharp.Dom;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Data;
using Smartstore.Diagnostics;
using Smartstore.Threading;

namespace Smartstore.Core.Content.Menus
{
    /// <summary>
    /// A generic implementation of <see cref="IMenu" /> which represents a <see cref="MenuEntity"/> entity.
    /// </summary>
    internal class DatabaseMenu : MenuBase
    {
        private readonly static AsyncLock _asyncLock = new();

        private readonly Lazy<ICatalogSearchService> _catalogSearchService;
        private readonly Lazy<ICategoryService> _categoryService;
        private readonly CatalogSettings _catalogSettings;
        private readonly SearchSettings _searchSettings;
        private readonly IDictionary<string, Lazy<IMenuItemProvider, MenuItemProviderMetadata>> _menuItemProviders;

        public DatabaseMenu(
            string menuName,
            Lazy<ICatalogSearchService> catalogSearchService,
            Lazy<ICategoryService> categoryService,
            CatalogSettings catalogSettings,
            SearchSettings searchSettings,
            IEnumerable<Lazy<IMenuItemProvider, MenuItemProviderMetadata>> menuItemProviders)
        {
            Guard.NotEmpty(menuName);

            Name = menuName;

            _catalogSearchService = catalogSearchService;
            _categoryService = categoryService;
            _catalogSettings = catalogSettings;
            _searchSettings = searchSettings;
            _menuItemProviders = menuItemProviders.ToDictionarySafe(x => x.Metadata.ProviderName, x => x);
        }

        public DbQuerySettings QuerySettings { get; set; } = DbQuerySettings.Default;

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public override string Name { get; }

        public override bool ApplyPermissions => true;

        public override async Task ResolveElementCountAsync(TreeNode<MenuItem> curNode, bool deep = false)
        {
            if (curNode == null || !_catalogSettings.ShowCategoryProductNumber || !await ContainsProviderAsync("catalog"))
            {
                return;
            }

            try
            {
                using (Services.Chronometer.Step($"DatabaseMenu.ResolveElementCountAsync() for {curNode.Value.Text.NaIfEmpty()}"))
                {
                    // Perf: only resolve counts for categories in the current path.
                    while (curNode != null)
                    {
                        if (curNode.Children.Any(x => !x.Value.ElementsCountResolved))
                        {
                            using (await _asyncLock.LockAsync())
                            {
                                if (curNode.Children.Any(x => !x.Value.ElementsCountResolved))
                                {
                                    var nodes = deep ? curNode.SelectNodes(x => true, false) : curNode.Children.AsEnumerable();

                                    foreach (var node in nodes)
                                    {
                                        var item = node.Value;
                                        if (item.EntityId <= 0)
                                        {
                                            item.ElementsCountResolved = true;
                                            continue;
                                        }

                                        var isCategory = item.EntityName.EqualsNoCase(nameof(Category));
                                        var isManufacturer = item.EntityName.EqualsNoCase(nameof(Manufacturer));

                                        if (!isCategory && !isManufacturer)
                                        {
                                            item.ElementsCountResolved = true;
                                            continue;
                                        }

                                        var storeId = Services.StoreContext.CurrentStoreIdIfMultiStoreMode;
                                        var query = new CatalogSearchQuery()
                                            .VisibleOnly()
                                            .WithVisibility(ProductVisibility.Full)
                                            .HasStoreId(storeId)
                                            .BuildFacetMap(false)
                                            .BuildHits(false);

                                        if (isCategory)
                                        {
                                            if (_catalogSettings.ShowCategoryProductNumberIncludingSubcategories)
                                            {
                                                var categoryTree = await _categoryService.Value.GetCategoryTreeAsync(0, false, storeId);
                                                var categoryNode = categoryTree.SelectNodeById(item.EntityId);
                                                if (categoryNode != null)
                                                {
                                                    query = query.WithCategoryTreePath(categoryNode.GetTreePath(), null);
                                                }
                                            }
                                            else
                                            {
                                                query = query.WithCategoryIds(null, new[] { item.EntityId });
                                            }
                                        }
                                        else
                                        {
                                            query = query.WithManufacturerIds(null, new[] { item.EntityId });
                                        }

                                        if (!_searchSettings.IncludeNotAvailable)
                                        {
                                            query = query.AvailableOnly(true);
                                        }

                                        var searchResult = await _catalogSearchService.Value.SearchAsync(query);
                                        item.ElementsCount = searchResult.TotalHitsCount;
                                        item.ElementsCountResolved = true;
                                    }
                                }
                            }
                        }

                        curNode = curNode.Parent;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public override async Task<TreeNode<MenuItem>> ResolveCurrentNodeAsync(ActionContext actionContext)
        {
            Guard.NotNull(actionContext);

            if (actionContext == null || !await ContainsProviderAsync("catalog"))
            {
                return await base.ResolveCurrentNodeAsync(actionContext);
            }

            TreeNode<MenuItem> currentNode = null;

            try
            {
                int currentCategoryId = GetRequestValue<int?>(actionContext, "currentCategoryId") ?? GetRequestValue<int>(actionContext, "categoryId");
                int currentProductId = 0;

                if (currentCategoryId == 0)
                {
                    currentProductId = GetRequestValue<int?>(actionContext, "currentProductId") ?? GetRequestValue<int>(actionContext, "productId");
                }

                if (currentCategoryId == 0 && currentProductId == 0)
                {
                    // Possibly not a category node of a menu where the category tree is attached to.
                    return await base.ResolveCurrentNodeAsync(actionContext);
                }

                var cacheKey = $"sm.temp.category.breadcrumb.{currentCategoryId}-{currentProductId}";
                currentNode = await Services.RequestCache.GetAsync(cacheKey, async () =>
                {
                    var root = await GetRootNodeAsync();
                    TreeNode<MenuItem> node = null;

                    if (currentCategoryId > 0)
                    {
                        node = root.SelectNodeById(currentCategoryId) ?? root.SelectNode(x => x.Value.EntityId == currentCategoryId);
                    }

                    if (node == null && currentProductId > 0)
                    {
                        var productCategories = await _categoryService.Value.GetProductCategoriesByProductIdsAsync(new[] { currentProductId });
                        if (productCategories.Any())
                        {
                            currentCategoryId = productCategories[0].Category.Id;
                            node = root.SelectNodeById(currentCategoryId) ?? root.SelectNode(x => x.Value.EntityId == currentCategoryId);
                        }
                    }

                    return node;
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return currentNode;
        }

        protected override async Task<TreeNode<MenuItem>> BuildAsync(CacheEntryOptions cacheEntryOptions)
        {
            IEnumerable<MenuItemEntity> entities;

            if (Name.HasValue())
            {
                var db = Services.DbContext;
                var store = Services.StoreContext.CurrentStore;
                var customerRoleIds = Services.WorkContext.CurrentCustomer.GetRoleIds();

                var menuItemQuery = db.Menus
                    .ApplyStandardFilter(Name, null, store.Id, customerRoleIds)
                    .ApplyMenuItemFilter(store.Id, customerRoleIds)
                    .Include(x => x.Menu);

                entities = await menuItemQuery.ToListAsync();
            }
            else
            {
                entities = Enumerable.Empty<MenuItemEntity>();
            }

            var tree = await entities.GetTreeAsync("DatabaseMenu", _menuItemProviders);

            return tree;
        }

        protected override string GetCacheKey()
        {
            var cacheKey = "{0}-{1}-{2}".FormatInvariant(
                Services.WorkContext.WorkingLanguage.Id,
                QuerySettings.IgnoreMultiStore ? 0 : Services.StoreContext.CurrentStore.Id,
                QuerySettings.IgnoreAcl ? "0" : Services.WorkContext.CurrentCustomer.GetRolesIdent());

            return cacheKey;
        }
    }
}
