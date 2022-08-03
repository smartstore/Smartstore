using System.ServiceModel.Syndication;
using Smartstore.Caching;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Logging;
using Smartstore.Core.OutputCache;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Http;
using Smartstore.Net;
using Smartstore.Web.Infrastructure.Hooks;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.Web.Controllers
{
    public partial class CatalogController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly ICategoryService _categoryService;
        private readonly IProductTagService _productTagService;
        private readonly IRecentlyViewedProductsService _recentlyViewedProductsService;
        private readonly IAclService _aclService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly MediaSettings _mediaSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly IProductCompareService _productCompareService;
        private readonly CatalogHelper _helper;
        private readonly IBreadcrumb _breadcrumb;
        private readonly SeoSettings _seoSettings;

        public CatalogController(
            SmartDbContext db,
            ICategoryService categoryService,
            IProductTagService productTagService,
            IRecentlyViewedProductsService recentlyViewedProductsService,
            IProductCompareService productCompareService,
            IAclService aclService,
            IStoreMappingService storeMappingService,
            ICatalogSearchService catalogSearchService,
            MediaSettings mediaSettings,
            CatalogSettings catalogSettings,
            CatalogHelper helper,
            IBreadcrumb breadcrumb,
            SeoSettings seoSettings)
        {
            _db = db;
            _categoryService = categoryService;
            _productTagService = productTagService;
            _recentlyViewedProductsService = recentlyViewedProductsService;
            _productCompareService = productCompareService;
            _aclService = aclService;
            _storeMappingService = storeMappingService;
            _catalogSearchService = catalogSearchService;
            _mediaSettings = mediaSettings;
            _catalogSettings = catalogSettings;
            _helper = helper;
            _breadcrumb = breadcrumb;
            _seoSettings = seoSettings;
        }

        #region Category

        public async Task<IActionResult> Category(int categoryId, CatalogSearchQuery query)
        {
            var category = await _db.Categories
                .Include(x => x.MediaFile)
                .FindByIdAsync(categoryId, false);

            if (category == null || category.Deleted)
                return NotFound();

            // Check whether the current user has a "Manage catalog" permission.
            // It allows him to preview a category before publishing.
            if (!category.Published && !await Services.Permissions.AuthorizeAsync(Permissions.Catalog.Category.Read))
                return NotFound();

            // ACL (access control list).
            if (!await _aclService.AuthorizeAsync(category))
                return NotFound();

            // Store mapping.
            if (!await _storeMappingService.AuthorizeAsync(category))
                return NotFound();

            var customer = Services.WorkContext.CurrentCustomer;
            var storeId = Services.StoreContext.CurrentStore.Id;

            // 'Continue shopping' URL.
            if (!customer.IsSystemAccount)
            {
                customer.GenericAttributes.LastContinueShoppingPage = HttpContext.Request.RawUrl();
            }

            var model = await _helper.PrepareCategoryModelAsync(category);

            if (_seoSettings.CanonicalUrlsEnabled)
            {
                model.CanonicalUrl = Url.RouteUrl("Category", new { model.SeName }, Request.Scheme);
            }

            if (query.IsSubPage && !_catalogSettings.ShowDescriptionInSubPages)
            {
                model.Description.ChangeValue(string.Empty);
                model.BottomDescription.ChangeValue(string.Empty);
            }

            model.Image = await _helper.PrepareCategoryImageModelAsync(category, model.Name);

            // Category breadcrumb.
            if (_catalogSettings.CategoryBreadcrumbEnabled)
            {
                await _helper.GetBreadcrumbAsync(_breadcrumb, ControllerContext);
            }

            // Products.
            var catIds = new int[] { categoryId };
            if (_catalogSettings.ShowProductsFromSubcategories)
            {
                // Include subcategories.
                catIds = catIds.Concat(await _helper.GetChildCategoryIdsAsync(categoryId)).ToArray();
            }

            query.WithCategoryIds(_catalogSettings.IncludeFeaturedProductsInNormalLists ? null : false, catIds);

            var searchResult = await _catalogSearchService.SearchAsync(query);
            model.SearchResult = searchResult;

            var mappingSettings = _helper.GetBestFitProductSummaryMappingSettings(GetViewMode(query));
            model.Products = await _helper.MapProductSummaryModelAsync(searchResult, mappingSettings);

            model.SubCategoryDisplayType = _catalogSettings.SubCategoryDisplayType;

            var pictureSize = _mediaSettings.CategoryThumbPictureSize;
            var fallbackType = _catalogSettings.HideCategoryDefaultPictures ? FallbackPictureType.NoFallback : FallbackPictureType.Entity;

            var hideSubCategories = _catalogSettings.SubCategoryDisplayType == SubCategoryDisplayType.Hide
                || (_catalogSettings.SubCategoryDisplayType == SubCategoryDisplayType.AboveProductList && query.IsSubPage && !_catalogSettings.ShowSubCategoriesInSubPages);
            var hideFeaturedProducts = _catalogSettings.IgnoreFeaturedProducts || (query.IsSubPage && !_catalogSettings.IncludeFeaturedProductsInSubPages);

            // Subcategories.
            if (!hideSubCategories)
            {
                var subCategories = await _categoryService.GetCategoriesByParentCategoryIdAsync(categoryId);
                model.SubCategories = await _helper.MapCategorySummaryModelAsync(subCategories, pictureSize);
            }

            // Featured Products. 
            if (!hideFeaturedProducts)
            {
                CatalogSearchResult featuredProductsResult = null;

                string cacheKey = ModelCacheInvalidator.CATEGORY_HAS_FEATURED_PRODUCTS_KEY.FormatInvariant(categoryId, string.Join(",", customer.GetRoleIds()), storeId);
                var hasFeaturedProductsCache = await Services.Cache.GetAsync<bool?>(cacheKey);

                var featuredProductsQuery = new CatalogSearchQuery()
                    .VisibleOnly(customer)
                    .WithVisibility(ProductVisibility.Full)
                    .WithCategoryIds(true, categoryId)
                    .HasStoreId(storeId)
                    .WithLanguage(Services.WorkContext.WorkingLanguage)
                    .WithCurrency(Services.WorkContext.WorkingCurrency);

                if (!hasFeaturedProductsCache.HasValue)
                {
                    featuredProductsResult = await _catalogSearchService.SearchAsync(featuredProductsQuery);
                    hasFeaturedProductsCache = featuredProductsResult.TotalHitsCount > 0;
                    await Services.Cache.PutAsync(cacheKey, hasFeaturedProductsCache, new CacheEntryOptions().ExpiresIn(TimeSpan.FromHours(6)));
                }

                if (hasFeaturedProductsCache.Value && featuredProductsResult == null)
                {
                    featuredProductsResult = await _catalogSearchService.SearchAsync(featuredProductsQuery);
                }

                if (featuredProductsResult != null)
                {
                    var featuredProductsMappingSettings = _helper.GetBestFitProductSummaryMappingSettings(ProductSummaryViewMode.Grid);
                    model.FeaturedProducts = await _helper.MapProductSummaryModelAsync(featuredProductsResult, featuredProductsMappingSettings);
                }
            }

            // Prepare paging/sorting/mode stuff.
            _helper.MapListActions(model.Products, category, _catalogSettings.DefaultPageSizeOptions);

            // Template.
            var templateCacheKey = string.Format(ModelCacheInvalidator.CATEGORY_TEMPLATE_MODEL_KEY, category.CategoryTemplateId);
            var templateViewPath = await Services.Cache.GetAsync(templateCacheKey, async () =>
            {
                var template = await _db.CategoryTemplates.FindByIdAsync(category.CategoryTemplateId, false)
                    ?? await _db.CategoryTemplates.FirstOrDefaultAsync();

                return template.ViewPath;
            });

            // Activity log.
            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.PublicStoreViewCategory, T("ActivityLog.PublicStore.ViewCategory"), category.Name);

            Services.DisplayControl.Announce(category);

            return View(templateViewPath, model);
        }

        #endregion

        #region Brand

        public async Task<IActionResult> Manufacturer(int manufacturerId, CatalogSearchQuery query)
        {
            var manufacturer = await _db.Manufacturers.FindByIdAsync(manufacturerId, false);
            if (manufacturer == null || manufacturer.Deleted)
                return NotFound();

            // Check whether the current user has a "Manage catalog" permission.
            // It allows him to preview a manufacturer before publishing.
            if (!manufacturer.Published && !await Services.Permissions.AuthorizeAsync(Permissions.Catalog.Manufacturer.Read))
                return NotFound();

            // ACL (access control list).
            if (!await _aclService.AuthorizeAsync(manufacturer))
                return NotFound();

            // Store mapping.
            if (!await _storeMappingService.AuthorizeAsync(manufacturer))
                return NotFound();

            var customer = Services.WorkContext.CurrentCustomer;
            var storeId = Services.StoreContext.CurrentStore.Id;

            // 'Continue shopping' URL.
            if (!customer.IsSystemAccount)
            {
                customer.GenericAttributes.LastContinueShoppingPage = HttpContext.Request.RawUrl();
            }

            var model = await _helper.PrepareBrandModelAsync(manufacturer);

            if (_seoSettings.CanonicalUrlsEnabled)
            {
                model.CanonicalUrl = Url.RouteUrl("Manufacturer", new { model.SeName }, Request.Scheme);
            }

            if (query.IsSubPage && !_catalogSettings.ShowDescriptionInSubPages)
            {
                model.Description.ChangeValue(string.Empty);
                model.BottomDescription.ChangeValue(string.Empty);
            }

            model.Image = await _helper.PrepareBrandImageModelAsync(manufacturer, model.Name);

            // Featured products.
            var hideFeaturedProducts = _catalogSettings.IgnoreFeaturedProducts || (query.IsSubPage && !_catalogSettings.IncludeFeaturedProductsInSubPages);
            if (!hideFeaturedProducts)
            {
                CatalogSearchResult featuredProductsResult = null;

                var cacheKey = ModelCacheInvalidator.MANUFACTURER_HAS_FEATURED_PRODUCTS_KEY.FormatInvariant(manufacturerId, string.Join(",", customer.GetRoleIds()), storeId);
                var hasFeaturedProductsCache = await Services.Cache.GetAsync<bool?>(cacheKey);

                var featuredProductsQuery = new CatalogSearchQuery()
                    .VisibleOnly(customer)
                    .WithVisibility(ProductVisibility.Full)
                    .WithManufacturerIds(true, manufacturerId)
                    .HasStoreId(storeId)
                    .WithLanguage(Services.WorkContext.WorkingLanguage)
                    .WithCurrency(Services.WorkContext.WorkingCurrency);

                if (!hasFeaturedProductsCache.HasValue)
                {
                    featuredProductsResult = await _catalogSearchService.SearchAsync(featuredProductsQuery);
                    hasFeaturedProductsCache = featuredProductsResult.TotalHitsCount > 0;
                    await Services.Cache.PutAsync(cacheKey, hasFeaturedProductsCache);
                }

                if (hasFeaturedProductsCache.Value && featuredProductsResult == null)
                {
                    featuredProductsResult = await _catalogSearchService.SearchAsync(featuredProductsQuery);
                }

                if (featuredProductsResult != null)
                {
                    // TODO: (mc) determine settings properly
                    var featuredProductsmappingSettings = _helper.GetBestFitProductSummaryMappingSettings(ProductSummaryViewMode.Grid);
                    model.FeaturedProducts = await _helper.MapProductSummaryModelAsync(featuredProductsResult, featuredProductsmappingSettings);
                }
            }

            // Products
            query.WithManufacturerIds(_catalogSettings.IncludeFeaturedProductsInNormalLists ? null : false, manufacturerId);

            var searchResult = await _catalogSearchService.SearchAsync(query);
            model.SearchResult = searchResult;

            var mappingSettings = _helper.GetBestFitProductSummaryMappingSettings(GetViewMode(query));
            model.Products = await _helper.MapProductSummaryModelAsync(searchResult, mappingSettings);

            // Prepare paging/sorting/mode stuff
            _helper.MapListActions(model.Products, manufacturer, _catalogSettings.DefaultPageSizeOptions);

            // Template.
            var templateCacheKey = string.Format(ModelCacheInvalidator.MANUFACTURER_TEMPLATE_MODEL_KEY, manufacturer.ManufacturerTemplateId);
            var templateViewPath = await Services.Cache.GetAsync(templateCacheKey, async () =>
            {
                var template = await _db.ManufacturerTemplates.FindByIdAsync(manufacturer.ManufacturerTemplateId, false)
                    ?? await _db.ManufacturerTemplates.FirstOrDefaultAsync();

                return template.ViewPath;
            });

            // Activity log.
            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.PublicStoreViewManufacturer, T("ActivityLog.PublicStore.ViewManufacturer"), manufacturer.Name);

            Services.DisplayControl.Announce(manufacturer);

            return View(templateViewPath, model);
        }

        [LocalizedRoute("/manufacturer/all", Name = "ManufacturerList")]
        public async Task<IActionResult> ManufacturerAll()
        {
            var model = new List<BrandModel>();
            var brands = await _db.Manufacturers
                .AsNoTracking()
                .Include(x => x.MediaFile)
                .ApplyStandardFilter(storeId: Services.StoreContext.CurrentStore.Id)
                .ToListAsync();

            foreach (var brand in brands)
            {
                var manuModel = await _helper.PrepareBrandModelAsync(brand);
                manuModel.Image = await _helper.PrepareBrandImageModelAsync(brand, manuModel.Name);
                model.Add(manuModel);
            }

            Services.DisplayControl.AnnounceRange(brands);

            ViewBag.SortManufacturersAlphabetically = _catalogSettings.SortManufacturersAlphabetically;

            return View(model);
        }

        #endregion

        #region ProductTags

        [LocalizedRoute("/producttag/{productTagId:int}/{*path}", Name = "ProductsByTag")]
        public async Task<IActionResult> ProductsByTag(int productTagId, CatalogSearchQuery query)
        {
            var productTag = await _db.ProductTags.FindByIdAsync(productTagId, false);
            if (productTag == null)
            {
                return NotFound();
            }

            if (!productTag.Published && !await Services.Permissions.AuthorizeAsync(Permissions.Catalog.Product.Read))
            {
                return NotFound();
            }

            var model = new ProductsByTagModel
            {
                Id = productTag.Id,
                TagName = productTag.GetLocalized(y => y.Name)
            };

            if (_seoSettings.CanonicalUrlsEnabled)
            {
                model.CanonicalUrl = Url.RouteUrl("ProductsByTag", new { productTagId, path = model.TagName }, Request.Scheme);
            }

            query.WithProductTagIds(productTagId);

            var searchResult = await _catalogSearchService.SearchAsync(query);
            model.SearchResult = searchResult;

            var mappingSettings = _helper.GetBestFitProductSummaryMappingSettings(GetViewMode(query));
            model.Products = await _helper.MapProductSummaryModelAsync(searchResult, mappingSettings);

            // Prepare paging/sorting/mode stuff.
            _helper.MapListActions(model.Products, null, _catalogSettings.DefaultPageSizeOptions);

            return View(model);
        }

        [LocalizedRoute("/producttag/all", Name = "ProductTagsAll")]
        public async Task<IActionResult> ProductTagsAll()
        {
            var model = new PopularProductTagsModel();
            var productCountsMap = await _productTagService.GetProductCountsMapAsync(null, Services.StoreContext.CurrentStore.Id);
            var pager = new FastPager<ProductTag>(_db.ProductTags.AsNoTracking().Where(x => x.Published), 1000);

            while ((await pager.ReadNextPageAsync<ProductTag>()).Out(out var tags))
            {
                foreach (var tag in tags)
                {
                    if (productCountsMap.TryGetValue(tag.Id, out var productCount) && productCount > 0)
                    {
                        model.Tags.Add(new ProductTagModel
                        {
                            Id = tag.Id,
                            Name = tag.GetLocalized(x => x.Name),
                            Slug = tag.BuildSlug(),
                            ProductCount = productCount
                        });
                    }
                }
            }

            model.Tags = model.Tags.OrderBy(x => x.Name).ToList();

            return View(model);
        }

        #endregion

        #region Recently[...]Products

        [LocalizedRoute("/newproducts", Name = "RecentlyAddedProducts")]
        public async Task<IActionResult> RecentlyAddedProducts(CatalogSearchQuery query)
        {
            if (!_catalogSettings.RecentlyAddedProductsEnabled || _catalogSettings.RecentlyAddedProductsNumber <= 0)
            {
                return View(ProductSummaryModel.Empty);
            }

            query.Sorting.Clear();
            query = query
                .BuildFacetMap(false)
                .SortBy(ProductSortingEnum.CreatedOn)
                .Slice(0, _catalogSettings.RecentlyAddedProductsNumber);

            var result = await _catalogSearchService.SearchAsync(query);
            var hits = await result.GetHitsAsync();
            var settings = _helper.GetBestFitProductSummaryMappingSettings(GetViewMode(query));

            var model = await _helper.MapProductSummaryModelAsync(hits.ToList(), settings);
            model.GridColumnSpan = GridColumnSpan.Max5Cols;

            return View(model);
        }

        [LocalizedRoute("/newproducts/rss", Name = "RecentlyAddedProductsRSS")]
        public async Task<IActionResult> RecentlyAddedProductsRSS(CatalogSearchQuery query)
        {
            // TODO: (mc) find a more prominent place for the "NewProducts" link (may be in main menu?)
            var store = Services.StoreContext.CurrentStore;
            var protocol = Services.WebHelper.IsCurrentConnectionSecured() ? "https" : "http";
            var selfLink = Url.RouteUrl("RecentlyAddedProductsRSS", null, protocol);
            var recentProductsLink = Url.RouteUrl("RecentlyAddedProducts", null, protocol);
            var title = $"{store.Name} - {T("RSS.RecentlyAddedProducts")}";
            var feed = new SmartSyndicationFeed(new Uri(recentProductsLink), title, T("RSS.InformationAboutProducts"));

            feed.AddNamespaces(true);
            feed.Init(selfLink, Services.WorkContext.WorkingLanguage.LanguageCulture.EmptyNull().ToLower());

            if (!_catalogSettings.RecentlyAddedProductsEnabled || _catalogSettings.RecentlyAddedProductsNumber <= 0)
            {
                return new RssActionResult(feed);
            }

            var items = new List<SyndicationItem>();

            query.Sorting.Clear();
            query = query
                .BuildFacetMap(false)
                .SortBy(ProductSortingEnum.CreatedOn)
                .Slice(0, _catalogSettings.RecentlyAddedProductsNumber);

            var result = await _catalogSearchService.SearchAsync(query);
            var hits = await result.GetHitsAsync();
            var storeUrl = store.GetHost();

            // Prefetching.
            var fileIds = hits
                .Select(x => x.MainPictureId ?? 0)
                .Where(x => x != 0)
                .Distinct()
                .ToArray();

            var files = (await Services.MediaService.GetFilesByIdsAsync(fileIds)).ToDictionarySafe(x => x.Id);

            foreach (var product in hits)
            {
                var productUrl = Url.RouteUrl("Product", new { SeName = await product.GetActiveSlugAsync() }, protocol);
                if (productUrl.HasValue())
                {
                    var content = product.GetLocalized(x => x.FullDescription).Value;

                    if (content.HasValue())
                    {
                        content = WebHelper.MakeAllUrlsAbsolute(content, Request);
                    }

                    var item = feed.CreateItem(
                        product.GetLocalized(x => x.Name),
                        product.GetLocalized(x => x.ShortDescription),
                        productUrl,
                        product.CreatedOnUtc,
                        content);

                    try
                    {
                        // We add only the first media file.
                        if (files.TryGetValue(product.MainPictureId ?? 0, out var file))
                        {
                            var url = Services.MediaService.GetUrl(file, _mediaSettings.ProductDetailsPictureSize, storeUrl, false);
                            feed.AddEnclosure(item, file, url);
                        }
                    }
                    catch
                    {
                    }

                    items.Add(item);
                }
            }

            feed.Items = items;

            Services.DisplayControl.AnnounceRange(hits);

            return new RssActionResult(feed);
        }

        [LocalizedRoute("/recentlyviewedproducts", Name = "RecentlyViewedProducts")]
        public async Task<IActionResult> RecentlyViewedProducts()
        {
            if (!_catalogSettings.RecentlyViewedProductsEnabled || _catalogSettings.RecentlyViewedProductsNumber <= 0)
            {
                return View(ProductSummaryModel.Empty);
            }

            var products = await _recentlyViewedProductsService.GetRecentlyViewedProductsAsync(_catalogSettings.RecentlyViewedProductsNumber);
            var settings = _helper.GetBestFitProductSummaryMappingSettings(ProductSummaryViewMode.List);
            var model = await _helper.MapProductSummaryModelAsync(products, settings);

            return View(model);
        }

        #endregion

        #region Comparing products

        [LocalizedRoute("/compareproducts", Name = "CompareProducts")]
        public async Task<IActionResult> CompareProducts()
        {
            if (!_catalogSettings.CompareProductsEnabled)
            {
                return NotFound();
            }

            var products = await _productCompareService.GetCompareListAsync();
            var settings = _helper.GetBestFitProductSummaryMappingSettings(ProductSummaryViewMode.Compare);
            var model = await _helper.MapProductSummaryModelAsync(products, settings);

            return View(model);
        }

        [ActionName("AddProductToCompare")]
        public async Task<IActionResult> AddProductToCompareList(int id)
        {
            if (!_catalogSettings.CompareProductsEnabled)
                return NotFound();

            var product = await _db.Products.FindByIdAsync(id, false);
            if (product == null || product.IsSystemProduct || !product.Published)
                return NotFound();

            _productCompareService.AddToList(id);

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.PublicStoreAddToCompareList, T("ActivityLog.PublicStore.AddToCompareList"), product.Name);

            return RedirectToRoute("CompareProducts");
        }

        [HttpPost]
        [ActionName("AddProductToCompare")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddProductToCompareListAjax(int id)
        {
            var failed = Json(new
            {
                success = false,
                message = T("AddProductToCompareList.CouldNotBeAdded")
            });

            if (!_catalogSettings.CompareProductsEnabled)
                return failed;

            var product = await _db.Products.FindByIdAsync(id, false);

            if (product == null || product.IsSystemProduct || !product.Published)
            {
                return failed;
            }

            _productCompareService.AddToList(id);

            //activity log
            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.PublicStoreAddToCompareList, T("ActivityLog.PublicStore.AddToCompareList"), product.Name);

            return Json(new
            {
                success = true,
                message = T("AddProductToCompareList.ProductWasAdded", product.Name).Value
            });
        }

        [ActionName("RemoveProductFromCompare")]
        public async Task<IActionResult> RemoveProductFromCompareList(int id)
        {
            if (!_catalogSettings.CompareProductsEnabled)
                return NotFound();

            var product = await _db.Products.FindByIdAsync(id, false);
            if (product == null)
                return NotFound();

            _productCompareService.RemoveFromList(id);

            return RedirectToRoute("CompareProducts");
        }

        [HttpPost]
        [ActionName("RemoveProductFromCompare")]
        public async Task<IActionResult> RemoveProductFromCompareListAjax(int id)
        {
            var failed = Json(new
            {
                success = false,
                message = T("AddProductToCompareList.CouldNotBeRemoved")
            });

            if (!_catalogSettings.CompareProductsEnabled)
                return failed;

            var product = await _db.Products.FindByIdAsync(id, false);
            if (product == null)
            {
                return failed;
            }

            _productCompareService.RemoveFromList(id);

            return Json(new
            {
                success = true,
                message = string.Format(T("AddProductToCompareList.ProductWasDeleted"), product.Name)
            });
        }

        public IActionResult ClearCompareList()
        {
            if (!_catalogSettings.CompareProductsEnabled)
                return RedirectToRoute("Homepage");

            _productCompareService.ClearCompareList();

            return RedirectToRoute("CompareProducts");
        }

        // AJAX.
        [HttpPost]
        [ActionName("ClearCompareList")]
        public IActionResult ClearCompareListAjax()
        {
            _productCompareService.ClearCompareList();

            return Json(new
            {
                success = true,
                message = T("CompareList.ListWasCleared")
            });
        }

        public async Task<IActionResult> OffCanvasCompare()
        {
            if (!_catalogSettings.CompareProductsEnabled)
            {
                return PartialView(ProductSummaryModel.Empty);
            }

            var products = await _productCompareService.GetCompareListAsync();
            var settings = _helper.GetBestFitProductSummaryMappingSettings(ProductSummaryViewMode.Grid, x =>
            {
                x.MapAttributes = false;
                x.MapColorAttributes = false;
                x.MapManufacturers = false;
            });

            var model = await _helper.MapProductSummaryModelAsync(products, settings);

            return PartialView(model);
        }

        #endregion

        private static ProductSummaryViewMode GetViewMode(CatalogSearchQuery query)
        {
            Guard.NotNull(query, nameof(query));

            return query.CustomData.Get("ViewMode") is string viewMode && viewMode.EqualsNoCase("list")
                ? ProductSummaryViewMode.List
                : ProductSummaryViewMode.Grid;
        }
    }
}
