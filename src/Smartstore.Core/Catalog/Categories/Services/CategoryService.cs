using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Seo;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Utilities;

namespace Smartstore.Core.Catalog.Categories
{
    public partial class CategoryService : ICategoryService, IXmlSitemapPublisher
    {
        internal static TimeSpan CategoryTreeCacheDuration = TimeSpan.FromHours(6);

        // {0} = IncludeHidden, {1} = CustomerRoleIds, {2} = StoreId
        internal const string CATEGORY_TREE_KEY = "category:tree-{0}-{1}-{2}";
        internal const string CATEGORY_TREE_PATTERN_KEY = "category:tree-*";

        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly ICacheManager _cache;
        private readonly IStoreMappingService _storeMappingService;

        public CategoryService(
            SmartDbContext db,
            IWorkContext workContext,
            ICacheManager cache,
            IStoreMappingService storeMappingService)
        {
            _db = db;
            _workContext = workContext;
            _cache = cache;
            _storeMappingService = storeMappingService;
        }

        public virtual async Task<(int AffectedCategories, int AffectedProducts)> InheritAclIntoChildrenAsync(
            int categoryId,
            bool touchProductsWithMultipleCategories = false,
            bool touchExistingAcls = false,
            bool categoriesOnly = false)
        {
            var categoryEntityName = nameof(Category);
            //var productEntityName = nameof(Product);
            var affectedCategories = 0;
            var affectedProducts = 0;

            var allCustomerRolesIds = await _db.CustomerRoles
                .AsQueryable()
                .Select(x => x.Id)
                .ToListAsync();

            var referenceCategory = await _db.Categories.FindByIdAsync(categoryId);
            // TODO: (mg) (core) Complete ICategoryService.InheritAclIntoChildrenAsync (IAclService required).
            var referenceRoleIds = new int[0];

            using (var scope = new DbContextScope(_db, autoDetectChanges: false))
            {
                await ProcessCategory(scope, referenceCategory);
            }

            // TODO: (mg) (core) Complete ICategoryService.InheritAclIntoChildrenAsync (IAclService required).
            //_cache.RemoveByPattern(AclService.ACL_SEGMENT_PATTERN);

            return (affectedCategories, affectedProducts);

            async Task ProcessCategory(DbContextScope scope, Category c)
            {
                // Process sub-categories.
                var subCategories = await _db.Categories
                    .AsQueryable()
                    .Where(x => x.ParentCategoryId == c.Id)
                    .OrderBy(x => x.DisplayOrder)
                    .ToListAsync();

                foreach (var subCategory in subCategories)
                {
                    if (subCategory.SubjectToAcl != referenceCategory.SubjectToAcl)
                    {
                        subCategory.SubjectToAcl = referenceCategory.SubjectToAcl;
                    }

                    var aclRecords = await _db.AclRecords
                        .ApplyEntityFilter(subCategory)
                        .ToListAsync();

                    var aclRecordsDic = aclRecords.ToDictionarySafe(x => x.CustomerRoleId);

                    foreach (var roleId in allCustomerRolesIds)
                    {
                        if (referenceRoleIds.Contains(roleId))
                        {
                            if (!aclRecordsDic.ContainsKey(roleId))
                            {
                                await _db.AclRecords.AddAsync(new AclRecord { CustomerRoleId = roleId, EntityId = subCategory.Id, EntityName = categoryEntityName });
                            }
                        }
                        else if (aclRecordsDic.TryGetValue(roleId, out var aclRecordToDelete))
                        {
                            _db.AclRecords.Remove(aclRecordToDelete);
                        }
                    }
                }

                await scope.CommitAsync();
                affectedCategories += subCategories.Count;

                // Process products.
                var categoryIds = new HashSet<int>(subCategories.Select(x => x.Id));
                categoryIds.Add(c.Id);

                // TODO: (mg) (core) Complete ICategoryService.InheritAclIntoChildrenAsync (ICatalogSearchService required).
                //var searchQuery = new CatalogSearchQuery().WithCategoryIds(null, categoryIds.ToArray());
                //var productsQuery = _catalogSearchService.PrepareQuery(searchQuery);
                //var productsPager = new FastPager<Product>(productsQuery, 500);

                //while (productsPager.ReadNextPage(out var products))
                //{
                //    foreach (var product in products)
                //    {
                //        if (product.SubjectToAcl != referenceCategory.SubjectToAcl)
                //        {
                //            product.SubjectToAcl = referenceCategory.SubjectToAcl;
                //        }

                //        var aclRecords = await _db.AclRecords
                //            .ApplyEntityFilter(product)
                //            .ToListAsync();

                //        var aclRecordsDic = aclRecords.ToDictionarySafe(x => x.CustomerRoleId);

                //        foreach (var roleId in allCustomerRolesIds)
                //        {
                //            if (referenceRoleIds.Contains(roleId))
                //            {
                //                if (!aclRecordsDic.ContainsKey(roleId))
                //                {
                //                    await _db.AclRecords.AddAsync(new AclRecord { CustomerRoleId = roleId, EntityId = product.Id, EntityName = productEntityName });
                //                }
                //            }
                //            else if (aclRecordsDic.TryGetValue(roleId, out var aclRecordToDelete))
                //            {
                //                _db.AclRecords.Remove(aclRecordToDelete);
                //            }
                //        }
                //    }

                //    await scope.CommitAsync();
                //    affectedProducts += products.Count;
                //}

                //await scope.CommitAsync();

                try
                {
                    scope.DbContext.DetachEntities(x => x is Product || x is Category || x is AclRecord, false);
                }
                catch { }

                foreach (var subCategory in subCategories)
                {
                    await ProcessCategory(scope, subCategory);
                }
            }
        }

        public virtual async Task<(int AffectedCategories, int AffectedProducts)> InheritStoresIntoChildrenAsync(
            int categoryId,
            bool touchProductsWithMultipleCategories = false,
            bool touchExistingAcls = false,
            bool categoriesOnly = false)
        {
            var categoryEntityName = nameof(Category);
            //var productEntityName = nameof(Product);
            var affectedCategories = 0;
            var affectedProducts = 0;

            var allStoreIds = await _db.Stores
                .AsQueryable()
                .Select(x => x.Id)
                .ToListAsync();

            var referenceCategory = await _db.Categories.FindByIdAsync(categoryId);
            var referenceStoreMappingIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(referenceCategory);

            using (var scope = new DbContextScope(_db, autoDetectChanges: false))
            {
                await ProcessCategory(scope, referenceCategory);
            }

            _cache.RemoveByPattern(StoreMappingService.STOREMAPPING_SEGMENT_PATTERN);

            return (affectedCategories, affectedProducts);

            async Task ProcessCategory(DbContextScope scope, Category c)
            {
                // Process sub-categories.
                var subCategories = await _db.Categories
                    .AsQueryable()
                    .Where(x => x.ParentCategoryId == c.Id)
                    .OrderBy(x => x.DisplayOrder)
                    .ToListAsync();

                foreach (var subCategory in subCategories)
                {
                    if (subCategory.LimitedToStores != referenceCategory.LimitedToStores)
                    {
                        subCategory.LimitedToStores = referenceCategory.LimitedToStores;
                    }

                    var storeMappings = await _db.StoreMappings
                        .ApplyEntityFilter(subCategory)
                        .ToListAsync();

                    var storeMappingsDic = storeMappings.ToDictionarySafe(x => x.StoreId);

                    foreach (var storeId in allStoreIds)
                    {
                        if (referenceStoreMappingIds.Contains(storeId))
                        {
                            if (!storeMappingsDic.ContainsKey(storeId))
                            {
                                await _db.StoreMappings.AddAsync(new StoreMapping { StoreId = storeId, EntityId = subCategory.Id, EntityName = categoryEntityName });
                            }
                        }
                        else if (storeMappingsDic.TryGetValue(storeId, out var storeMappingToDelete))
                        {
                            _db.StoreMappings.Remove(storeMappingToDelete);
                        }
                    }
                }

                await scope.CommitAsync();
                affectedCategories += subCategories.Count;

                // Process products.
                var categoryIds = new HashSet<int>(subCategories.Select(x => x.Id));
                categoryIds.Add(c.Id);

                // TODO: (mg) (core) Complete ICategoryService.InheritStoresIntoChildrenAsync (ICatalogSearchService required).
                //var searchQuery = new CatalogSearchQuery().WithCategoryIds(null, categoryIds.ToArray());
                //var productsQuery = _catalogSearchService.PrepareQuery(searchQuery);
                //var productsPager = new FastPager<Product>(productsQuery, 500);

                //while (productsPager.ReadNextPage(out var products))
                //{
                //    foreach (var product in products)
                //    {
                //        if (product.LimitedToStores != referenceCategory.LimitedToStores)
                //        {
                //            product.LimitedToStores = referenceCategory.LimitedToStores;
                //        }

                //        var storeMappings = await _db.StoreMappings
                //            .ApplyEntityFilter(product)
                //            .ToListAsync();

                //        var storeMappingsDic = storeMappings.ToDictionarySafe(x => x.StoreId);

                //        foreach (var storeId in allStoreIds)
                //        {
                //            if (referenceStoreMappingIds.Contains(storeId))
                //            {
                //                if (!storeMappingsDic.ContainsKey(storeId))
                //                {
                //                    await _db.StoreMappings.AddAsync(new StoreMapping { StoreId = storeId, EntityId = product.Id, EntityName = productEntityName });
                //                }
                //            }
                //            else if (storeMappingsDic.TryGetValue(storeId, out var storeMappingToDelete))
                //            {
                //                _db.StoreMappings.Remove(storeMappingToDelete);
                //            }
                //        }
                //    }

                //    await scope.CommitAsync();
                //    affectedProducts += products.Count;
                //}

                //await scope.CommitAsync();

                try
                {
                    scope.DbContext.DetachEntities(x => x is Product || x is Category || x is StoreMapping, false);
                }
                catch { }

                foreach (var subCategory in subCategories)
                {
                    await ProcessCategory(scope, subCategory);
                }
            }
        }

        public virtual async Task<string> GetCategoryPathAsync(
            ICategoryNode categoryNode,
            int? languageId = null,
            string aliasPattern = null,
            string separator = " » ")
        {
            Guard.NotNull(categoryNode, nameof(categoryNode));

            var treeNode = await GetCategoryTreeAsync(categoryNode.Id, true);
            if (treeNode != null)
            {
                return GetCategoryPath(treeNode, languageId, aliasPattern, separator);
            }

            return string.Empty;
        }

        public virtual string GetCategoryPath(
            TreeNode<ICategoryNode> treeNode,
            int? languageId = null,
            string aliasPattern = null,
            string separator = " » ")
        {
            Guard.NotNull(treeNode, nameof(treeNode));

            var lookupKey = "Path.{0}.{1}.{2}".FormatInvariant(separator, languageId ?? 0, aliasPattern.HasValue());
            var cachedPath = treeNode.GetMetadata<string>(lookupKey, false);

            if (cachedPath != null)
            {
                return cachedPath;
            }

            var trail = treeNode.Trail;
            using var psb = StringBuilderPool.Instance.Get(out var sb);

            foreach (var node in trail)
            {
                if (!node.IsRoot)
                {
                    var cat = node.Value;

                    var name = languageId.HasValue
                        ? cat.GetLocalized(n => n.Name, languageId.Value)
                        : cat.Name;

                    sb.Append(name);

                    if (aliasPattern.HasValue() && cat.Alias.HasValue())
                    {
                        sb.Append(' ');
                        sb.Append(string.Format(aliasPattern, cat.Alias));
                    }

                    if (node != treeNode)
                    {
                        // Is not self (trail end).
                        sb.Append(separator);
                    }
                }
            }

            var path = sb.ToString();
            treeNode.SetThreadMetadata(lookupKey, path);

            return path;
        }

        public async Task<TreeNode<ICategoryNode>> GetCategoryTreeAsync(int rootCategoryId = 0, bool includeHidden = false, int storeId = 0)
        {
            var rolesIds = _workContext.CurrentCustomer.GetRoleIds();
            var storeToken = _db.QuerySettings.IgnoreMultiStore ? "0" : storeId.ToString();
            var rolesToken = _db.QuerySettings.IgnoreAcl || includeHidden ? "0" : string.Join(",", rolesIds);
            var cacheKey = CATEGORY_TREE_KEY.FormatInvariant(includeHidden.ToString().ToLower(), rolesToken, storeToken);

            var root = await _cache.GetAsync(cacheKey, async o =>
            {
                o.ExpiresIn(CategoryTreeCacheDuration);

                var categoryQuery = _db.Categories
                    .ApplyStandardFilter(includeHidden, includeHidden ? null : rolesIds, includeHidden ? 0 : storeId);

                // (Perf) don't fetch every field from db.
                var query = categoryQuery
                    .Select(x => new
                    {
                        x.Id,
                        x.ParentCategoryId,
                        x.Name,
                        x.ExternalLink,
                        x.Alias,
                        x.MediaFileId,
                        x.Published,
                        x.DisplayOrder,
                        x.UpdatedOnUtc,
                        x.BadgeText,
                        x.BadgeStyle,
                        x.LimitedToStores,
                        x.SubjectToAcl
                    });

                var categories = await query.ToListAsync();
                var unsortedNodes = categories.Select(x => new CategoryNode
                {
                    Id = x.Id,
                    ParentCategoryId = x.ParentCategoryId,
                    Name = x.Name,
                    ExternalLink = x.ExternalLink,
                    Alias = x.Alias,
                    MediaFileId = x.MediaFileId,
                    Published = x.Published,
                    DisplayOrder = x.DisplayOrder,
                    UpdatedOnUtc = x.UpdatedOnUtc,
                    BadgeText = x.BadgeText,
                    BadgeStyle = x.BadgeStyle,
                    LimitedToStores = x.LimitedToStores,
                    SubjectToAcl = x.SubjectToAcl
                });

                var nodeMap = unsortedNodes.ToMultimap(x => x.ParentCategoryId, x => x);
                var curParent = new TreeNode<ICategoryNode>(new CategoryNode { Name = "Home" });

                AddChildTreeNodes(curParent, 0, nodeMap);

                return curParent.Root;
            });

            if (rootCategoryId > 0)
            {
                root = root.SelectNodeById(rootCategoryId);
            }

            return root;
        }

        private void AddChildTreeNodes(TreeNode<ICategoryNode> parentNode, int parentItemId, Multimap<int, CategoryNode> nodeMap)
        {
            if (parentNode == null)
            {
                return;
            }

            var nodes = nodeMap.ContainsKey(parentItemId)
                ? nodeMap[parentItemId].OrderBy(x => x.DisplayOrder)
                : Enumerable.Empty<CategoryNode>();

            foreach (var node in nodes)
            {
                var newNode = new TreeNode<ICategoryNode>(node)
                {
                    Id = node.Id
                };

                parentNode.Append(newNode);

                AddChildTreeNodes(newNode, node.Id, nodeMap);
            }
        }

        public XmlSitemapProvider PublishXmlSitemap(XmlSitemapBuildContext context)
        {
            if (!context.LoadSettings<SeoSettings>().XmlSitemapIncludesCategories)
            {
                return null;
            }

            var customerRolesIds = _workContext.CurrentCustomer.GetRoleIds();

            var query = _db.Categories
                .AsNoTracking()
                .ApplyStandardFilter(false, customerRolesIds, context.RequestStoreId);

            return new CategoryXmlSitemapResult { Query = query };
        }

        class CategoryXmlSitemapResult : XmlSitemapProvider
        {
            public IQueryable<Category> Query { get; set; }

            public override async Task<int> GetTotalCountAsync()
            {
                return await Query.CountAsync();
            }

            public override async IAsyncEnumerable<NamedEntity> EnlistAsync([EnumeratorCancellation] CancellationToken cancelToken = default)
            {
                var categories = await Query.Select(x => new { x.Id, x.UpdatedOnUtc }).ToListAsync(cancelToken);

                await foreach (var x in categories)
                {
                    yield return new NamedEntity { EntityName = nameof(Category), Id = x.Id, LastMod = x.UpdatedOnUtc };
                }
            }

            public override int Order => int.MinValue;
        }
    }
}
