using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;

namespace Smartstore.Web.Controllers
{
    public partial class ProductController : PublicControllerBase
    {
        private readonly SmartDbContext _db;
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


        public ProductController(
            SmartDbContext db,
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

        #region Products

        public async Task<IActionResult> ProductDetails(int productId, ProductVariantQuery query)
        {
            var product = await _db.Products.FindByIdAsync(productId, false);
            if (product == null || product.Deleted || product.IsSystemProduct)
                return NotFound();

            // Is published? Check whether the current user has a "Manage catalog" permission.
            // It allows him to preview a product before publishing.
            if (!product.Published && !await Services.Permissions.AuthorizeAsync(Permissions.Catalog.Product.Read))
                return NotFound();

            // ACL (access control list).
            if (!await _aclService.AuthorizeAsync(product))
                return NotFound();

            // Store mapping.
            if (!await _storeMappingService.AuthorizeAsync(product))
                return NotFound();

            // Save as recently viewed
            _recentlyViewedProductsService.AddProductToRecentlyViewedList(product.Id);

            // Activity log
            Services.ActivityLogger.LogActivity("PublicStore.ViewProduct", T("ActivityLog.PublicStore.ViewProduct"), product.Name);

            // TODO: (mh) (core) Continue CatalogController.Category()
            
            var store = Services.StoreContext.CurrentStore;
            var price = Services.CurrencyService.ConvertToWorkingCurrency(product.Price);

            return Content($"Product --> Id: {product.Id}, Name: {product.Name}, Price: {price}");
        }

        #endregion
    }
}
