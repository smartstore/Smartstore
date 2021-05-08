using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Catalog;
using Smartstore.Collections;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Events;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class ProductController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly ICustomerService _customerService;
        private readonly IUrlService _urlService;
        private readonly IWorkContext _workContext;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IMediaService _mediaService;
        private readonly IProductTagService _productTagService;
        private readonly IProductCloner _productCloner;
        private readonly IActivityLogger _activityLogger;
        private readonly IAclService _aclService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreMappingService _storeMappingService;
        private readonly AdminAreaSettings _adminAreaSettings;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IDiscountService _discountService;
        private readonly IProductAttributeService _productAttributeService;
        //private readonly IBackInStockSubscriptionService _backInStockSubscriptionService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        //private readonly IProductAttributeParser _productAttributeParser;
        private readonly CatalogSettings _catalogSettings;
        private readonly IDownloadService _downloadService;
        private readonly IDeliveryTimeService _deliveryTimesService;
        private readonly IMeasureService _measureService;
        private readonly MeasureSettings _measureSettings;
        private readonly IEventPublisher _eventPublisher;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ICommonServices _services;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly ProductUrlHelper _productUrlHelper;
        private readonly SeoSettings _seoSettings;
        private readonly MediaSettings _mediaSettings;
        private readonly SearchSettings _searchSettings;

        public ProductController(
            SmartDbContext db,
            IProductService productService,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            ICustomerService customerService,
            IUrlService urlService,
            IWorkContext workContext,
            ILanguageService languageService,
            ILocalizationService localizationService,
            ILocalizedEntityService localizedEntityService,
            IMediaService mediaService,
            IProductTagService productTagService,
            IProductCloner productCloner,
            IActivityLogger activityLogger,
            IAclService aclService,
            IStoreContext storeContext,
            IStoreMappingService storeMappingService,
            AdminAreaSettings adminAreaSettings,
            IDateTimeHelper dateTimeHelper,
            IDiscountService discountService,
            IProductAttributeService productAttributeService,
            //IBackInStockSubscriptionService backInStockSubscriptionService,
            IShoppingCartService shoppingCartService,
            IProductAttributeFormatter productAttributeFormatter,
            //IProductAttributeParser productAttributeParser,
            CatalogSettings catalogSettings,
            IDownloadService downloadService,
            IDeliveryTimeService deliveryTimesService,
            IMeasureService measureService,
            MeasureSettings measureSettings,
            IEventPublisher eventPublisher,
            IGenericAttributeService genericAttributeService,
            ICommonServices services,
            ICatalogSearchService catalogSearchService,
            ProductUrlHelper productUrlHelper,
            SeoSettings seoSettings,
            MediaSettings mediaSettings,
            SearchSettings searchSettings)
        {
            _db = db;
            _productService = productService;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _customerService = customerService;
            _urlService = urlService;
            _workContext = workContext;
            _languageService = languageService;
            _localizationService = localizationService;
            _localizedEntityService = localizedEntityService;
            _mediaService = mediaService;
            _productTagService = productTagService;
            _productCloner = productCloner;
            _activityLogger = activityLogger;
            _aclService = aclService;
            _storeContext = storeContext;
            _storeMappingService = storeMappingService;
            _adminAreaSettings = adminAreaSettings;
            _dateTimeHelper = dateTimeHelper;
            _discountService = discountService;
            _productAttributeService = productAttributeService;
            //_backInStockSubscriptionService = backInStockSubscriptionService;
            _shoppingCartService = shoppingCartService;
            _productAttributeFormatter = productAttributeFormatter;
            //_productAttributeParser = productAttributeParser;
            _catalogSettings = catalogSettings;
            _downloadService = downloadService;
            _deliveryTimesService = deliveryTimesService;
            _measureService = measureService;
            _measureSettings = measureSettings;
            _eventPublisher = eventPublisher;
            _genericAttributeService = genericAttributeService;
            _services = services;
            _catalogSearchService = catalogSearchService;
            _productUrlHelper = productUrlHelper;
            _seoSettings = seoSettings;
            _mediaSettings = mediaSettings;
            _searchSettings = searchSettings;
        }

        #region Product list / create / edit / delete

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        [Permission(Permissions.Catalog.Product.Read)]
        public IActionResult List(ProductListModel model)
        {
            model.DisplayProductPictures = _adminAreaSettings.DisplayProductPictures;
            model.IsSingleStoreMode = _storeContext.IsSingleStoreMode();
            model.GridPageSize = _adminAreaSettings.GridPageSize;

            // TODO: (core) Uncomment later
            //foreach (var c in _categoryService.GetCategoryTree(includeHidden: true).FlattenNodes(false))
            //{
            //    model.AvailableCategories.Add(new SelectListItem { Text = c.GetCategoryNameIndented(), Value = c.Id.ToString() });
            //}

            //foreach (var m in _manufacturerService.GetAllManufacturers(true))
            //{
            //    model.AvailableManufacturers.Add(new SelectListItem { Text = m.Name, Value = m.Id.ToString() });
            //}

            //model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList(false).ToList();

            return View(model);
        }

        [HttpPost, IgnoreAntiforgeryToken] // TODO: (core) Why is posted _RequestVerificationToken not valid?
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductList(GridCommand command, ProductListModel model)
        {
            var gridModel = new GridModel<ProductModel>();

            var fields = new List<string> { "name" };
            if (_searchSettings.SearchFields.Contains("sku"))
            {
                fields.Add("sku");
            }
                
            if (_searchSettings.SearchFields.Contains("shortdescription"))
            {
                fields.Add("shortdescription");
            }  

            var searchQuery = new CatalogSearchQuery(fields.ToArray(), model.SearchProductName)
                .HasStoreId(model.SearchStoreId)
                .WithCurrency(_workContext.WorkingCurrency)
                .WithLanguage(_workContext.WorkingLanguage);

            if (model.SearchIsPublished.HasValue)
            {
                searchQuery = searchQuery.PublishedOnly(model.SearchIsPublished.Value);
            }
                
            if (model.SearchHomePageProducts.HasValue)
            {
                searchQuery = searchQuery.HomePageProductsOnly(model.SearchHomePageProducts.Value);
            }
                
            if (model.SearchProductTypeId > 0)
            {
                searchQuery = searchQuery.IsProductType((ProductType)model.SearchProductTypeId);
            }   

            if (model.SearchWithoutManufacturers.HasValue)
            {
                searchQuery = searchQuery.HasAnyManufacturer(!model.SearchWithoutManufacturers.Value);
            }  
            else if (model.SearchManufacturerId != 0)
            {
                searchQuery = searchQuery.WithManufacturerIds(null, model.SearchManufacturerId);
            }
                

            if (model.SearchWithoutCategories.HasValue)
            {
                searchQuery = searchQuery.HasAnyCategory(!model.SearchWithoutCategories.Value);
            }
            else if (model.SearchCategoryId != 0)
            {
                searchQuery = searchQuery.WithCategoryIds(null, model.SearchCategoryId);
            }

            IPagedList<Product> products;

            if (_searchSettings.UseCatalogSearchInBackend)
            {
                searchQuery = searchQuery.Slice((command.Page - 1) * command.PageSize, command.PageSize);

                var sort = command.Sorting?.FirstOrDefault();
                if (sort != null)
                {
                    switch (sort.Member)
                    {
                        case nameof(ProductModel.Name):
                            searchQuery = searchQuery.SortBy(sort.Descending ? ProductSortingEnum.NameDesc : ProductSortingEnum.NameAsc);
                            break;
                        case nameof(ProductModel.Price):
                            searchQuery = searchQuery.SortBy(sort.Descending ? ProductSortingEnum.PriceDesc : ProductSortingEnum.PriceAsc);
                            break;
                        case nameof(ProductModel.CreatedOn):
                            searchQuery = searchQuery.SortBy(sort.Descending ? ProductSortingEnum.CreatedOn : ProductSortingEnum.CreatedOnAsc);
                            break;
                    }
                }

                if (!searchQuery.Sorting.Any())
                {
                    searchQuery = searchQuery.SortBy(ProductSortingEnum.NameAsc);
                }

                var searchResult = await _catalogSearchService.SearchAsync(searchQuery);
                products = await searchResult.GetHitsAsync();
            }
            else
            {
                var query = _catalogSearchService
                    .PrepareQuery(searchQuery)
                    .ApplyGridCommand(command, false);

                products = await new PagedList<Product>(query, command.Page - 1, command.PageSize).LoadAsync();
            }

            var fileIds = products.AsEnumerable()
                .Select(x => x.MainPictureId ?? 0)
                .Where(x => x != 0)
                .Distinct()
                .ToArray();

            var files = (await _mediaService.GetFilesByIdsAsync(fileIds)).ToDictionarySafe(x => x.Id);

            gridModel.Rows = products.AsEnumerable().Select(x =>
            {
                var productModel = new ProductModel
                {
                    Sku = x.Sku,
                    Published = x.Published,
                    ProductTypeLabelHint = x.ProductTypeLabelHint,
                    Name = x.Name,
                    Id = x.Id,
                    StockQuantity = x.StockQuantity,
                    Price = x.Price,
                    LimitedToStores = x.LimitedToStores,
                    EditUrl = Url.Action("Edit", "Product", new { id = x.Id })
                };

                //MiniMapper.Map(x, productModel);

                files.TryGetValue(x.MainPictureId ?? 0, out var file);

                // TODO: (core) Use IImageModel
                productModel.PictureThumbnailUrl = _mediaService.GetUrl(file, _mediaSettings.CartThumbPictureSize);
                productModel.NoThumb = file == null;

                productModel.ProductTypeName = x.GetProductTypeLabel(_localizationService);
                productModel.UpdatedOn = _dateTimeHelper.ConvertToUserTime(x.UpdatedOnUtc, DateTimeKind.Utc);
                productModel.CreatedOn = _dateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);

                return productModel;
            });

            gridModel.Total = products.TotalCount;

            return Json(gridModel);
        }

        #endregion
    }
}
