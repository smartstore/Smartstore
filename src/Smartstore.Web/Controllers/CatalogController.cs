using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching.OutputCache;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Domain.Catalog;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Web.Infrastructure.Hooks;
using Smartstore.Web.Models.Catalog;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Controllers
{
    public partial class CatalogController : PublicControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IProductService _productService;
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
        private readonly Lazy<IUrlHelper> _urlHelper;

        public CatalogController(
            SmartDbContext db,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            IProductService productService,
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
            SeoSettings seoSettings,
            Lazy<IUrlHelper> urlHelper)
        {
            _db = db;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _productService = productService;
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
            _urlHelper = urlHelper;
        }

        #region Category

        public async Task<IActionResult> Category(int categoryId, CatalogSearchQuery query)
        {
            var category = await _db.Categories.FindByIdAsync(categoryId, false);
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
                customer.GenericAttributes.LastContinueShoppingPage = Services.WebHelper.GetCurrentPageUrl(false);
            }

            var model = await _helper.PrepareCategoryModelAsync(category);

            if (_seoSettings.CanonicalUrlsEnabled)
            {
                model.CanonicalUrl = _urlHelper.Value.RouteUrl("Category", new { model.SeName }, Request.Scheme);
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

            var viewMode = _helper.GetSearchQueryViewMode(query);
            var mappingSettings = _helper.GetBestFitProductSummaryMappingSettings(viewMode);
            model.Products = await _helper.MapProductSummaryModelAsync(await searchResult.GetHitsAsync(), mappingSettings);

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
                    await Services.Cache.PutAsync(cacheKey, hasFeaturedProductsCache);
                }

                if (hasFeaturedProductsCache.Value && featuredProductsResult == null)
                {
                    featuredProductsResult = await _catalogSearchService.SearchAsync(featuredProductsQuery);
                }

                if (featuredProductsResult != null)
                {
                    var featuredProductsMappingSettings = _helper.GetBestFitProductSummaryMappingSettings(ProductSummaryViewMode.Grid);
                    model.FeaturedProducts = await _helper.MapProductSummaryModelAsync(await featuredProductsResult.GetHitsAsync(), featuredProductsMappingSettings);
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
            Services.ActivityLogger.LogActivity("PublicStore.ViewCategory", T("ActivityLog.PublicStore.ViewCategory"), category.Name);

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
                customer.GenericAttributes.LastContinueShoppingPage = Services.WebHelper.GetCurrentPageUrl(false);
            }

            var model = await _helper.PrepareBrandModelAsync(manufacturer);

            if (_seoSettings.CanonicalUrlsEnabled)
            {
                model.CanonicalUrl = _urlHelper.Value.RouteUrl("Manufacturer", new { model.SeName }, Request.Scheme);
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
                    model.FeaturedProducts = await _helper.MapProductSummaryModelAsync(await featuredProductsResult.GetHitsAsync(), featuredProductsmappingSettings);
                }
            }

            // Products
            query.WithManufacturerIds(_catalogSettings.IncludeFeaturedProductsInNormalLists ? null : false, manufacturerId);

            var searchResult = await _catalogSearchService.SearchAsync(query);
            model.SearchResult = searchResult;

            var viewMode = _helper.GetSearchQueryViewMode(query);
            var mappingSettings = _helper.GetBestFitProductSummaryMappingSettings(viewMode);
            model.Products = await _helper.MapProductSummaryModelAsync(await searchResult.GetHitsAsync(), mappingSettings);

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
            Services.ActivityLogger.LogActivity("PublicStore.ViewManufacturer", T("ActivityLog.PublicStore.ViewManufacturer"), manufacturer.Name);

            // TODO: (mh) (core) Why weren't categories announced?
            Services.DisplayControl.Announce(manufacturer);

            return View(templateViewPath, model);
        }

        [LocalizedRoute("/manufacturer/all", Name = "ManufacturerList")]
        public async Task<IActionResult> ManufacturerAll()
        {
            var model = new List<BrandModel>();
            var manufacturers = await _db.Manufacturers
                .AsNoTracking()
                .ApplyStandardFilter(storeId: Services.StoreContext.CurrentStore.Id)
                .ToListAsync();

            var fileIds = manufacturers
                .Select(x => x.MediaFileId ?? 0)
                .Where(x => x != 0)
                .Distinct()
                .ToArray();

            var files = (await Services.MediaService.GetFilesByIdsAsync(fileIds)).ToDictionarySafe(x => x.Id);

            foreach (var manufacturer in manufacturers)
            {
                var manuModel = await _helper.PrepareBrandModelAsync(manufacturer);
                manuModel.Image = await _helper.PrepareBrandImageModelAsync(manufacturer, manuModel.Name, files);
                model.Add(manuModel);
            }

            Services.DisplayControl.AnnounceRange(manufacturers);

            ViewBag.SortManufacturersAlphabetically = _catalogSettings.SortManufacturersAlphabetically;

            return View(model);
        }

        #endregion

        #region ProductTags

        // TODO: (mh) (core) [RewriteUrl(SslRequirement.No)] Is this stil necessary?
        // TODO: (mh) (core) What about this original RouteValue > productTagId = idConstraint?
        [LocalizedRoute("/producttag/{productTagId}/{*path}", Name = "ProductsByTag")]
        public async Task<IActionResult> ProductsByTag(int productTagId, CatalogSearchQuery query)
        {
            var productTag = await _db.ProductTags.FindByIdAsync(productTagId);
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
                model.CanonicalUrl = _urlHelper.Value.RouteUrl("ProductsByTag", new { productTagId = productTagId, path = model.TagName }, Request.Scheme);
            }

            query.WithProductTagIds(new int[] { productTagId });

            var searchResult = await _catalogSearchService.SearchAsync(query);
            model.SearchResult = searchResult;

            var mappingSettings = _helper.GetBestFitProductSummaryMappingSettings(_helper.GetSearchQueryViewMode(query));
            model.Products = await _helper.MapProductSummaryModelAsync(await searchResult.GetHitsAsync(), mappingSettings);

            // Prepare paging/sorting/mode stuff.
            _helper.MapListActions(model.Products, null, _catalogSettings.DefaultPageSizeOptions);

            return View(model);
        }

        // TODO: (mh) (core) [RewriteUrl(SslRequirement.No)] Is this stil necessary?
        [LocalizedRoute("/producttag/all", Name = "ProductTagsAll")]
        public async Task<IActionResult> ProductTagsAll()
        {
            // TODO: (mh) (core) This is nearly the same code as in PopularProductTagsViewComponent > implement helper method PreparePopularProductTagsModel?
            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var model = new PopularProductTagsModel();

            // TODO: (mg) This is gonna explode with large amount of tags. Rethink!
            var allTags = await _db.ProductTags
                .Where(x => x.Published)
                .ToListAsync();

            var tags = (from t in allTags
                        let numProducts = _productTagService.CountProductsByTagIdAsync(t.Id, customer, store.Id).Await()
                        where numProducts > 0
                        orderby numProducts descending
                        select new
                        {
                            Tag = t,
                            LocalizedName = t.GetLocalized(x => x.Name),
                            NumProducts = numProducts
                        })
                        .OrderBy(x => x.LocalizedName.Value)
                        .ToList();

            foreach (var tag in tags)
            {
                model.Tags.Add(new ProductTagModel
                {
                    Id = tag.Tag.Id,
                    Name = tag.LocalizedName,
                    Slug = tag.Tag.BuildSlug(),
                    ProductCount = tag.NumProducts
                });
            }

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
            var settings = _helper.GetBestFitProductSummaryMappingSettings(_helper.GetSearchQueryViewMode(query));

            // TODO: (mh) (core) PagedList seems not to be correct. 100 items, Pagesize = 100 but 124 Pages :-/
            var model = await _helper.MapProductSummaryModelAsync(await result.GetHitsAsync(), settings);
            model.GridColumnSpan = GridColumnSpan.Max5Cols;

            return View(model);
        }

        // TODO: (mh) (core) Implement when RssActionResult is available.
        //[LocalizedRoute("/newproducts/rss", Name = "RecentlyAddedProductsRSS")]
        //public async Task<IActionResult> RecentlyAddedProductsRSS()
        //{
        //    ...
        //    return new RssActionResult { Feed = feed };
        //}

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
            var product = await _db.Products.FindByIdAsync(id);
            if (product == null || product.Deleted || product.IsSystemProduct || !product.Published)
                return NotFound();

            if (!_catalogSettings.CompareProductsEnabled)
                return NotFound();

            _productCompareService.AddToList(id);

            //activity log
            Services.ActivityLogger.LogActivity("PublicStore.AddToCompareList", T("ActivityLog.PublicStore.AddToCompareList"), product.Name);

            return RedirectToRoute("CompareProducts");
        }

        [HttpPost]
        [ActionName("AddProductToCompare")]
        public async Task<IActionResult> AddProductToCompareListAjax(int id)
        {
            var product = await _db.Products.FindByIdAsync(id);

            if (product == null || product.Deleted || product.IsSystemProduct || !product.Published || !_catalogSettings.CompareProductsEnabled)
            {
                return Json(new
                {
                    success = false,
                    message = T("AddProductToCompareList.CouldNotBeAdded")
                });
            }

            _productCompareService.AddToList(id);

            //activity log
            Services.ActivityLogger.LogActivity("PublicStore.AddToCompareList", T("ActivityLog.PublicStore.AddToCompareList"), product.Name);

            return Json(new
            {
                success = true,
                message = string.Format(T("AddProductToCompareList.ProductWasAdded"), product.Name)
            });
        }

        [ActionName("RemoveProductFromCompare")]
        public async Task<IActionResult> RemoveProductFromCompareList(int id)
        {
            var product = await _db.Products.FindByIdAsync(id);
            if (product == null)
                return NotFound();

            if (!_catalogSettings.CompareProductsEnabled)
                return NotFound();

            _productCompareService.RemoveFromList(id);

            return RedirectToRoute("CompareProducts");
        }

        [HttpPost]
        [ActionName("RemoveProductFromCompare")]
        public async Task<IActionResult> RemoveProductFromCompareListAjax(int id)
        {
            var product = await _db.Products.FindByIdAsync(id);
            if (product == null || !_catalogSettings.CompareProductsEnabled)
            {
                return Json(new
                {
                    success = false,
                    message = T("AddProductToCompareList.CouldNotBeRemoved")
                });
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
                return RedirectToRoute("HomePage");

            _productCompareService.ClearCompareList();

            return RedirectToRoute("CompareProducts");
        }

        // ajax
        [HttpPost]
        [ActionName("ClearCompareList")]
        public ActionResult ClearCompareListAjax()
        {
            _productCompareService.ClearCompareList();

            return Json(new
            {
                success = true,
                message = T("CompareList.ListWasCleared")
            });
        }

        // TODO: (mh) (core) OffCanvasCompare

        #endregion
    }
}
