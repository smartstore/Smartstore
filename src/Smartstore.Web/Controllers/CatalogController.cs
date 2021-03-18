using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Domain.Catalog;
using Smartstore.Core.Localization;
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

        #endregion
    }
}
