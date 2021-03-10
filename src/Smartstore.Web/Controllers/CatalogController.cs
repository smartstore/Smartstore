using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Domain.Catalog;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;

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
            IBreadcrumb breadcrumb)
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

            // 'Continue shopping' URL.
            if (!customer.IsSystemAccount)
            {
                customer.GenericAttributes.LastContinueShoppingPage = Services.WebHelper.GetCurrentPageUrl(false);
                await _db.SaveChangesAsync();
            }

            // TODO: (mh) (core) Continue CatalogController.Category()

            return Content($"Category --> Id: {category.Id}, Name: {category.Name}");
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

            // 'Continue shopping' URL.
            if (!customer.IsSystemAccount)
            {
                customer.GenericAttributes.LastContinueShoppingPage = Services.WebHelper.GetCurrentPageUrl(false);
                await _db.SaveChangesAsync();
            }

            // TODO: (mh) (core) Continue CatalogController.Manufacturer()

            return Content($"Manufacturer --> Id: {manufacturer.Id}, Name: {manufacturer.Name}");
        }

        #endregion
    }
}
