using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Common;
using Dasync.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
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
using Smartstore.Core.Catalog.Pricing;
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
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data.Batching;
using Smartstore.Events;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.DataGrid;
using Smartstore.Web.TagHelpers.Shared;

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
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly IStockSubscriptionService _stockSubscriptionService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
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
            IProductAttributeMaterializer productAttributeMaterializer,
            IStockSubscriptionService stockSubscriptionService,
            IShoppingCartService shoppingCartService,
            IShoppingCartValidator shoppingCartValidator,
            IProductAttributeFormatter productAttributeFormatter,
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
            _productAttributeMaterializer = productAttributeMaterializer;
            _stockSubscriptionService = stockSubscriptionService;
            _shoppingCartService = shoppingCartService;
            _shoppingCartValidator = shoppingCartValidator;
            _productAttributeFormatter = productAttributeFormatter;
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
        public async Task<IActionResult> List(ProductListModel model)
        {
            model.DisplayProductPictures = _adminAreaSettings.DisplayProductPictures;
            model.IsSingleStoreMode = _storeContext.IsSingleStoreMode();

            // TODO: (core) Uncomment later
            foreach (var c in (await _categoryService.GetCategoryTreeAsync(includeHidden: true)).FlattenNodes(false))
            {
                model.AvailableCategories.Add(new SelectListItem { Text = c.GetCategoryNameIndented(), Value = c.Id.ToString() });
            }

            foreach (var m in await _db.Manufacturers.AsNoTracking().ApplyStandardFilter(true).Select(x => new { x.Name, x.Id }).ToListAsync())
            {
                model.AvailableManufacturers.Add(new SelectListItem { Text = m.Name, Value = m.Id.ToString() });
            }

            //model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList(false).ToList();

            return View(model);
        }

        [HttpPost, IgnoreAntiforgeryToken] // TODO: (core) Why is posted _RequestVerificationToken not valid?
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductList(GridCommand command, ProductListModel model)
        {
            var gridModel = new GridModel<ProductOverviewModel>();

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

                products = await query.ToPagedList(command).LoadAsync();
            }

            var fileIds = products.AsEnumerable()
                .Select(x => x.MainPictureId ?? 0)
                .Where(x => x != 0)
                .Distinct()
                .ToArray();

            var files = (await _mediaService.GetFilesByIdsAsync(fileIds)).ToDictionarySafe(x => x.Id);

            gridModel.Rows = products.AsEnumerable().Select(x =>
            {
                var productModel = new ProductOverviewModel
                {
                    Sku = x.Sku,
                    Published = x.Published,
                    ProductTypeLabelHint = x.ProductTypeLabelHint,
                    Name = x.Name,
                    Id = x.Id,
                    StockQuantity = x.StockQuantity,
                    Price = x.Price,
                    LimitedToStores = x.LimitedToStores,
                    EditUrl = Url.Action("Edit", "Product", new { id = x.Id }),
                    ManufacturerPartNumber = x.ManufacturerPartNumber,
                    Gtin = x.Gtin,
                    MinStockQuantity = x.MinStockQuantity,
                    OldPrice = x.OldPrice,
                    AvailableStartDateTimeUtc = x.AvailableStartDateTimeUtc,
                    AvailableEndDateTimeUtc = x.AvailableEndDateTimeUtc
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

        [HttpPost, IgnoreAntiforgeryToken] // TODO: (core) Why is posted _RequestVerificationToken not valid?
        [Permission(Permissions.Catalog.Product.Delete)]
        public async Task<IActionResult> ProductDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                var products = await _db.Products.GetManyAsync(ids, true);
                // TODO: (core) Actually delete!
                numDeleted = products.Count;
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products.FindByIdAsync(id);
            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.DeleteProduct, T("ActivityLog.DeleteProduct"), product.Name);

            NotifySuccess(T("Admin.Catalog.Products.Deleted"));
            return RedirectToAction("List");
        }

        [HttpPost, IgnoreAntiforgeryToken] // TODO: (core) Why is posted _RequestVerificationToken not valid?
        [Permission(Permissions.Catalog.Product.Update)]
        public async Task<IActionResult> ProductUpdate(ProductOverviewModel model)
        {   
            var product = await _db.Products.FindByIdAsync(model.Id);

            product.Name = model.Name;
            product.Sku = model.Sku;
            product.Price = model.Price;
            product.StockQuantity = model.StockQuantity;
            product.Published = model.Published;
            product.ManufacturerPartNumber = model.ManufacturerPartNumber;
            product.Gtin = model.Gtin;
            product.MinStockQuantity = model.MinStockQuantity;
            product.OldPrice = model.OldPrice ?? 0;
            product.AvailableStartDateTimeUtc = model.AvailableStartDateTimeUtc;
            product.AvailableEndDateTimeUtc = model.AvailableEndDateTimeUtc;

            try
            {
                await _db.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                NotifyError(ex.GetInnerMessage());
                return Json(new { success = false });
            }
        }

        [HttpPost, IgnoreAntiforgeryToken]
        [Permission(Permissions.Catalog.Product.Create)]
        public async Task<IActionResult> ProductInsert(ProductOverviewModel model)
        {
            var product = new Product 
            {
                Name = model.Name,
                Sku = model.Sku,
                Price = model.Price,
                StockQuantity = model.StockQuantity,
                Published = model.Published,
                ManufacturerPartNumber = model.ManufacturerPartNumber,
                Gtin = model.Gtin,
                MinStockQuantity = model.MinStockQuantity,
                OldPrice = model.OldPrice ?? 0,
                AvailableStartDateTimeUtc = model.AvailableStartDateTimeUtc,
                AvailableEndDateTimeUtc = model.AvailableEndDateTimeUtc
            };

            try
            {
                _db.Products.Add(product);
                await _db.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                NotifyError(ex.GetInnerMessage());
                return Json(new { success = false });
            }
        }

        [Permission(Permissions.Catalog.Product.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new ProductModel();
            await PrepareProductModelAsync(model, null, true, true);
            AddLocales(model.Locales);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Catalog.Product.Create)]
        public async Task<IActionResult> Create(ProductModel model, bool continueEditing, FormCollection form)
        {
            if (model.DownloadFileVersion.HasValue() && model.DownloadId != null)
            {
                try
                {
                    var test = SemanticVersion.Parse(model.DownloadFileVersion);
                }
                catch
                {
                    ModelState.AddModelError("FileVersion", T("Admin.Catalog.Products.Download.SemanticVersion.NotValid"));
                }
            }

            if (ModelState.IsValid)
            {
                var product = new Product();

                _ = await MapModelToProductAsync(model, product, form);

                product.StockQuantity = 10000;
                product.OrderMinimumQuantity = 1;
                product.OrderMaximumQuantity = 100;
                product.HideQuantityControl = false;
                product.IsShippingEnabled = true;
                product.AllowCustomerReviews = true;
                product.Published = true;
                product.MaximumCustomerEnteredPrice = 1000;

                if (product.ProductType == ProductType.BundledProduct)
                {
                    product.BundleTitleText = T("Products.Bundle.BundleIncludes");
                }

                _db.Products.Add(product);
                await _db.SaveChangesAsync();
                
                await UpdateDataOfExistingProductAsync(product, model, false, false);

                // TODO: (mh) (core) Remove comment after review.
                // INFO: Obsolete as this will be taken care of in ProductHook.
                //_productService.UpdateHasDiscountsApplied(product);

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.AddNewProduct, T("ActivityLog.AddNewProduct"), product.Name);

                if (continueEditing)
                {
                    // ensure that the same tab gets selected in edit view
                    var selectedTab = TempData["SelectedTab.product-edit"] as SelectedTabInfo;
                    if (selectedTab != null)
                    {
                        selectedTab.Path = Url.Action("Edit", new RouteValueDictionary { { "id", product.Id } });
                    }
                }

                NotifySuccess(T("Admin.Catalog.Products.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = product.Id }) : RedirectToAction("List");
            }

            // If we got this far, something failed, redisplay form.
            await PrepareProductModelAsync(model, null, false, true);

            return View(model);
        }

        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _db.Products.FindByIdAsync(id);
            if (product == null)
            {
                NotifyWarning(T("Products.NotFound", id));
                return RedirectToAction("List");
            }

            if (product.Deleted)
            {
                NotifyWarning(T("Products.Deleted", id));
                return RedirectToAction("List");
            }

            var model = await MapperFactory.MapAsync<Product, ProductModel>(product);
            await PrepareProductModelAsync(model, product, false, false);

            AddLocales(model.Locales, async (locale, languageId) =>
            {
                locale.Name = product.GetLocalized(x => x.Name, languageId, false, false);
                locale.ShortDescription = product.GetLocalized(x => x.ShortDescription, languageId, false, false);
                locale.FullDescription = product.GetLocalized(x => x.FullDescription, languageId, false, false);
                locale.MetaKeywords = product.GetLocalized(x => x.MetaKeywords, languageId, false, false);
                locale.MetaDescription = product.GetLocalized(x => x.MetaDescription, languageId, false, false);
                locale.MetaTitle = product.GetLocalized(x => x.MetaTitle, languageId, false, false);
                locale.SeName = await product.GetActiveSlugAsync(languageId, false, false);
                locale.BundleTitleText = product.GetLocalized(x => x.BundleTitleText, languageId, false, false);
            });

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Catalog.Product.Update)]
        public async Task<IActionResult> Edit(ProductModel model, bool continueEditing, IFormCollection form)
        {
            var product = await _db.Products.FindByIdAsync(model.Id);
            if (product == null)
            {
                NotifyWarning(T("Products.NotFound", model.Id));
                return RedirectToAction("List");
            }

            if (product.Deleted)
            {
                NotifyWarning(T("Products.Deleted", model.Id));
                return RedirectToAction("List");
            }

            await UpdateDataOfProductDownloadsAsync(model);

            if (ModelState.IsValid)
            {
                var nameChanged = await MapModelToProductAsync(model, product, form);
                await UpdateDataOfExistingProductAsync(product, model, true, nameChanged);

                // TODO: (mh) (core) Remove comment after review.
                // INFO: Obsolete as this will be taken care of in ProductHook.
                //_productService.UpdateHasDiscountsApplied(product);

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditProduct, T("ActivityLog.EditProduct"), product.Name);

                NotifySuccess(T("Admin.Catalog.Products.Updated"));
                return continueEditing ? RedirectToAction("Edit", new { id = product.Id }) : RedirectToAction("List");
            }

            // If we got this far, something failed, redisplay form.
            await PrepareProductModelAsync(model, product, false, true);

            return View(model);
        }

        [NonAction]
        protected async Task UpdateDataOfProductDownloadsAsync(ProductModel model)
        {
            var testVersions = (new string[] { model.DownloadFileVersion, model.NewVersion }).Where(x => x.HasValue());
            var saved = false;
            foreach (var testVersion in testVersions)
            {
                try
                {
                    var test = SemanticVersion.Parse(testVersion);

                    // Insert versioned downloads here so they won't be saved if version ain't correct.
                    // If NewVersionDownloadId has value
                    if (model.NewVersion.HasValue() && !saved)
                    {
                        await InsertProductDownloadAsync(model.NewVersionDownloadId, model.Id, model.NewVersion);
                        saved = true;
                    }
                    else
                    {
                        await InsertProductDownloadAsync(model.DownloadId, model.Id, model.DownloadFileVersion);
                    }
                }
                catch
                {
                    ModelState.AddModelError("DownloadFileVersion", T("Admin.Catalog.Products.Download.SemanticVersion.NotValid"));
                }
            }

            var isUrlDownload = Request.Form["is-url-download-" + model.SampleDownloadId] == "true";
            var setOldFileToTransient = false;

            if (model.SampleDownloadId != model.OldSampleDownloadId && model.SampleDownloadId != 0 && !isUrlDownload)
            {
                // Insert sample download if a new file was uploaded.
                model.SampleDownloadId = await InsertSampleDownloadAsync(model.SampleDownloadId, model.Id);

                setOldFileToTransient = true;
            }
            else if (isUrlDownload)
            {
                var download = await _db.Downloads.FindByIdAsync((int)model.SampleDownloadId);
                download.IsTransient = false;
                await _db.SaveChangesAsync();
                
                setOldFileToTransient = true;
            }

            if (setOldFileToTransient && model.OldSampleDownloadId > 0)
            {
                var download = await _db.Downloads.FindByIdAsync((int)model.OldSampleDownloadId);
                download.IsTransient = true;
                await _db.SaveChangesAsync();
            }
        }

        [NonAction]
        protected async Task InsertProductDownloadAsync(int? fileId, int entityId, string fileVersion = "")
        {
            if (fileId > 0)
            {
                var isUrlDownload = Request.Form["is-url-download-" + fileId] == "true";

                if (!isUrlDownload)
                {
                    var mediaFileInfo = await _mediaService.GetFileByIdAsync((int)fileId);
                    var download = new Download
                    {
                        MediaFile = mediaFileInfo.File,
                        EntityId = entityId,
                        EntityName = nameof(Product),
                        DownloadGuid = Guid.NewGuid(),
                        UseDownloadUrl = false,
                        DownloadUrl = string.Empty,
                        UpdatedOnUtc = DateTime.UtcNow,
                        IsTransient = false,
                        FileVersion = fileVersion
                    };

                    _db.Downloads.Add(download);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    var download = await _db.Downloads.FindByIdAsync((int)fileId);
                    download.FileVersion = fileVersion;
                    download.IsTransient = false;
                    await _db.SaveChangesAsync();
                }
            }
        }

        [NonAction]
        protected async Task<int?> InsertSampleDownloadAsync(int? fileId, int entityId)
        {
            if (fileId > 0)
            {
                var mediaFileInfo = await _mediaService.GetFileByIdAsync((int)fileId);
                var download = new Download
                {
                    MediaFile = mediaFileInfo.File,
                    EntityId = entityId,
                    EntityName = nameof(Product),
                    DownloadGuid = Guid.NewGuid(),
                    UseDownloadUrl = false,
                    DownloadUrl = string.Empty,
                    UpdatedOnUtc = DateTime.UtcNow,
                    IsTransient = false
                };

                _db.Downloads.Add(download);
                await _db.SaveChangesAsync();

                return download.Id;
            }

            return null;
        }

        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> LoadEditTab(int id, string tabName, string viewPath = null)
        {
            try
            {
                if (id == 0)
                {
                    // If id is 0 we're in create mode.
                    return PartialView("_Create.SaveFirst");
                }

                if (tabName.IsEmpty())
                {
                    return Content("A unique tab name has to specified (route parameter: tabName)");
                }

                var product = await _db.Products.FindByIdAsync(id, false);
                var model = await MapperFactory.MapAsync<Product, ProductModel>(product);

                await PrepareProductModelAsync(model, product, false, false);

                AddLocales(model.Locales, async (locale, languageId) =>
                {
                    locale.Name = product.GetLocalized(x => x.Name, languageId, false, false);
                    locale.ShortDescription = product.GetLocalized(x => x.ShortDescription, languageId, false, false);
                    locale.FullDescription = product.GetLocalized(x => x.FullDescription, languageId, false, false);
                    locale.MetaKeywords = product.GetLocalized(x => x.MetaKeywords, languageId, false, false);
                    locale.MetaDescription = product.GetLocalized(x => x.MetaDescription, languageId, false, false);
                    locale.MetaTitle = product.GetLocalized(x => x.MetaTitle, languageId, false, false);
                    locale.SeName = await product.GetActiveSlugAsync(languageId, false, false);
                    locale.BundleTitleText = product.GetLocalized(x => x.BundleTitleText, languageId, false, false);
                });

                return PartialView(viewPath.NullEmpty() ?? "_CreateOrUpdate." + tabName, model);
            }
            catch (Exception ex)
            {
                return Content("Error while loading template: " + ex.Message);
            }
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditPromotion)]
        public async Task<IActionResult> RelatedProductAdd(int productId, int[] selectedProductIds)
        {
            var products = await _db.Products
                .AsNoTracking()
                .Where(x => selectedProductIds.Contains(x.Id))
                .ApplyStandardFilter()
                .ToListAsync();

            RelatedProduct relation = null;
            var maxDisplayOrder = -1;

            foreach (var product in products)
            {
                var existingRelations = await _db.RelatedProducts
                    .ApplyProductId1Filter(productId)
                    .ToListAsync();
                
                if (FindRelatedProduct(existingRelations, productId, product.Id) == null)
                {
                    if (maxDisplayOrder == -1 && (relation = existingRelations.OrderByDescending(x => x.DisplayOrder).FirstOrDefault()) != null)
                    {
                        maxDisplayOrder = relation.DisplayOrder;
                    }

                    _db.RelatedProducts.Add(new RelatedProduct
                    {
                        ProductId1 = productId,
                        ProductId2 = product.Id,
                        DisplayOrder = ++maxDisplayOrder
                    });
                }
            }

            await _db.SaveChangesAsync();

            return new EmptyResult();
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditPromotion)]
        public async Task<IActionResult> CreateAllMutuallyRelatedProducts(int productId)
        {
            string message = null;
            var product = await _db.Products.FindByIdAsync(productId, false);
                
            if (product != null)
            {
                var count = await _productService.EnsureMutuallyRelatedProductsAsync(productId);
                message = T("Admin.Common.CreateMutuallyAssociationsResult", count);
            }

            return new JsonResult ( new { Message = message } );
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditPromotion)]
        public async Task<IActionResult> CreateAllMutuallyCrossSellProducts(int productId)
        {
            string message = null;
            var product = await _db.Products.FindByIdAsync(productId, false);
            if (product != null)
            {
                var count = await _productService.EnsureMutuallyCrossSellProductsAsync(productId);
                message = T("Admin.Common.CreateMutuallyAssociationsResult", count);
            }

            return new JsonResult(new { Message = message });
        }

        /// <summary>
        /// Finds a related product item by specified identifiers
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="productId1">The first product identifier</param>
        /// <param name="productId2">The second product identifier</param>
        /// <returns>Related product</returns>
        private static RelatedProduct FindRelatedProduct(List<RelatedProduct> source, int productId1, int productId2)
        {
            return source.Where(x => x.ProductId1 == productId1 && x.ProductId2 == productId2).FirstOrDefault();
        }

        /// <summary>
        /// Finds a cross-sell product item by specified identifiers
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="productId1">The first product identifier</param>
        /// <param name="productId2">The second product identifier</param>
        /// <returns>Cross-sell product</returns>
        public static CrossSellProduct FindCrossSellProduct(List<CrossSellProduct> source, int productId1, int productId2)
        {
            return source.Where(x => x.ProductId1 == productId1 && x.ProductId2 == productId2).FirstOrDefault();
        }

        #endregion

        #region Product categories

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductCategoryList(GridCommand command, int productId)
        {
            var model = new GridModel<ProductModel.ProductCategoryModel>();
            var productCategories = await _categoryService.GetProductCategoriesByProductIdsAsync(new[] { productId }, true);

            var productCategoriesModel = await productCategories
                .AsQueryable()
                .ApplyGridCommand(command)
                .SelectAsync(async x =>
                {
                    var node = await _categoryService.GetCategoryTreeAsync(x.CategoryId, true);
                    return new ProductModel.ProductCategoryModel
                    {
                        Id = x.Id,
                        Category = node != null ? _categoryService.GetCategoryPath(node, aliasPattern: "<span class='badge badge-secondary'>{0}</span>") : string.Empty,
                        ProductId = x.ProductId,
                        CategoryId = x.CategoryId,
                        IsFeaturedProduct = x.IsFeaturedProduct,
                        DisplayOrder = x.DisplayOrder,
                        IsSystemMapping = x.IsSystemMapping,
                        EditUrl = Url.Action("Edit", "Category", new { id = x.CategoryId })
                    };
                })
                .AsyncToList();

            model.Rows = productCategoriesModel;
            model.Total = productCategoriesModel.Count;

            return Json(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditCategory)]
        public async Task<IActionResult> ProductCategoryInsert(ProductModel.ProductCategoryModel model, int productId)
        {
            var productCategory = new ProductCategory
            {
                ProductId = productId,
                CategoryId = int.Parse(model.Category), // Use Category property (not CategoryId) because appropriate property is stored in it.
                IsFeaturedProduct = model.IsFeaturedProduct,
                DisplayOrder = model.DisplayOrder
            };

            try
            {
                _db.ProductCategories.Add(productCategory);
                await _db.SaveChangesAsync();

                var mru = new TrimmedBuffer<string>(
                    _workContext.CurrentCustomer.GenericAttributes.MostRecentlyUsedCategories,
                    model.Category,
                    _catalogSettings.MostRecentlyUsedCategoriesMaxSize);

                _workContext.CurrentCustomer.GenericAttributes.MostRecentlyUsedCategories = mru.ToString();
                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                NotifyError(ex.GetInnerMessage());
                return Json(new { success = false });
            }
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditCategory)]
        public async Task<IActionResult> ProductCategoryUpdate(ProductModel.ProductCategoryModel model)
        {
            var productCategory = await _db.ProductCategories.FindByIdAsync(model.Id);
            var categoryChanged = int.Parse(model.Category) != productCategory.CategoryId;

            // Use Category property (not CategoryId) because appropriate property is stored in it.
            productCategory.CategoryId = int.Parse(model.Category);
            productCategory.IsFeaturedProduct = model.IsFeaturedProduct;
            productCategory.DisplayOrder = model.DisplayOrder;

            try
            {
                if (categoryChanged)
                {
                    var mru = new TrimmedBuffer<string>(
                    _workContext.CurrentCustomer.GenericAttributes.MostRecentlyUsedCategories,
                    model.Category,
                    _catalogSettings.MostRecentlyUsedCategoriesMaxSize);

                    _workContext.CurrentCustomer.GenericAttributes.MostRecentlyUsedCategories = mru.ToString();
                }

                await _db.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                NotifyError(ex.GetInnerMessage());
                return Json(new { success = false });
            }
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditCategory)]
        public async Task<IActionResult> ProductCategoryDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                numDeleted = await _db.ProductCategories
                    .AsQueryable()
                    .Where(x => ids.Contains(x.Id))
                    .BatchDeleteAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        #endregion

        #region Product manufacturers

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductManufacturerList(GridCommand command, int productId)
        {
            var model = new GridModel<ProductModel.ProductManufacturerModel>();
            var productManufacturers = await _manufacturerService.GetProductManufacturersByProductIdsAsync(new[] { productId }, true);

            var productManufacturersModel = productManufacturers
                .AsQueryable()
                .ApplyGridCommand(command)
                .ToList()
                .Select(x =>
                {
                    return new ProductModel.ProductManufacturerModel
                    {
                        Id = x.Id,
                        Manufacturer = x.Manufacturer.Name,
                        ProductId = x.ProductId,
                        ManufacturerId = x.ManufacturerId,
                        IsFeaturedProduct = x.IsFeaturedProduct,
                        DisplayOrder = x.DisplayOrder,
                        EditUrl = Url.Action("Edit", "Manufacturer", new { id = x.ManufacturerId })
                    };
                })
                .ToList();

            model.Rows = productManufacturersModel;
            model.Total = productManufacturersModel.Count;

            return Json(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditManufacturer)]
        public async Task<IActionResult> ProductManufacturerInsert(ProductModel.ProductManufacturerModel model, int productId)
        {
            var productManufacturer = new ProductManufacturer
            {
                ProductId = productId,
                ManufacturerId = int.Parse(model.Manufacturer), // Use Manufacturer property (not ManufacturerId) because appropriate property is stored in it.
                IsFeaturedProduct = model.IsFeaturedProduct,
                DisplayOrder = model.DisplayOrder
            };

            try
            {
                _db.ProductManufacturers.Add(productManufacturer);
                await _db.SaveChangesAsync();

                var mru = new TrimmedBuffer<string>(
                    _workContext.CurrentCustomer.GenericAttributes.MostRecentlyUsedManufacturers,
                    model.Manufacturer,
                    _catalogSettings.MostRecentlyUsedManufacturersMaxSize);

                _workContext.CurrentCustomer.GenericAttributes.MostRecentlyUsedManufacturers = mru.ToString();
                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                NotifyError(ex.GetInnerMessage());
                return Json(new { success = false });
            }
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditManufacturer)]
        public async Task<IActionResult> ProductManufacturerUpdate(ProductModel.ProductManufacturerModel model)
        {
            var productManufacturer = await _db.ProductManufacturers.FindByIdAsync(model.Id);
            var manufacturerChanged = int.Parse(model.Manufacturer) != productManufacturer.ManufacturerId;

            // Use Manufacturer property (not ManufacturerId) because appropriate property is stored in it.
            productManufacturer.ManufacturerId = int.Parse(model.Manufacturer);
            productManufacturer.IsFeaturedProduct = model.IsFeaturedProduct;
            productManufacturer.DisplayOrder = model.DisplayOrder;

            try
            {
                if (manufacturerChanged)
                {
                    var mru = new TrimmedBuffer<string>(
                    _workContext.CurrentCustomer.GenericAttributes.MostRecentlyUsedManufacturers,
                    model.Manufacturer,
                    _catalogSettings.MostRecentlyUsedManufacturersMaxSize);

                    _workContext.CurrentCustomer.GenericAttributes.MostRecentlyUsedManufacturers = mru.ToString();
                }

                await _db.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                NotifyError(ex.GetInnerMessage());
                return Json(new { success = false });
            }
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditManufacturer)]
        public async Task<IActionResult> ProductManufacturerDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                numDeleted = await _db.ProductManufacturers
                    .AsQueryable()
                    .Where(x => ids.Contains(x.Id))
                    .BatchDeleteAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        #endregion

        #region Related products

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> RelatedProductList(GridCommand command, int productId)
        {
            var model = new GridModel<ProductModel.RelatedProductModel>();
            var relatedProducts = await _db.RelatedProducts
                .AsNoTracking()
                .ApplyProductId1Filter(productId)
                .ApplyGridCommand(command)
                .ToListAsync();
            
            var relatedProductsModel = await relatedProducts
                .SelectAsync(async x =>
                {
                    var product2 = await _db.Products.FindByIdAsync(x.ProductId2, false);

                    return new ProductModel.RelatedProductModel()
                    {
                        Id = x.Id,
                        ProductId2 = x.ProductId2,
                        Product2Name = product2.Name,
                        ProductTypeName = product2.GetProductTypeLabel(_localizationService),
                        ProductTypeLabelHint = product2.ProductTypeLabelHint,
                        DisplayOrder = x.DisplayOrder,
                        Product2Sku = product2.Sku,
                        Product2Published = product2.Published,
                        EditUrl = Url.Action("Edit", "Product", new { id = product2.Id })
                    };
                })
                .AsyncToList();

            model.Rows = relatedProductsModel;
            model.Total = relatedProductsModel.Count;

            return Json(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditPromotion)]
        public async Task<IActionResult> RelatedProductUpdate(ProductModel.RelatedProductModel model)
        {
            var relatedProduct = await _db.RelatedProducts.FindByIdAsync(model.Id);
            relatedProduct.DisplayOrder = model.DisplayOrder;

            try
            {
                await _db.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                NotifyError(ex.GetInnerMessage());
                return Json(new { success = false });
            }
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditCategory)]
        public async Task<IActionResult> RelatedProductDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                numDeleted = await _db.RelatedProducts
                    .AsQueryable()
                    .Where(x => ids.Contains(x.Id))
                    .BatchDeleteAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        #endregion

        #region Cross-sell products

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> CrossSellProductList(GridCommand command, int productId)
        {
            var model = new GridModel<ProductModel.CrossSellProductModel>();
            var crossSellProducts = await _db.CrossSellProducts
                .AsNoTracking()
                .ApplyProductId1Filter(productId, true)
                .ApplyGridCommand(command)
                .ToListAsync();

            var crossSellProductsModel = await crossSellProducts
                .SelectAsync(async x =>
                {
                    var product2 = await _db.Products.FindByIdAsync(x.ProductId2, false);

                    return new ProductModel.CrossSellProductModel
                    {
                        Id = x.Id,
                        ProductId2 = x.ProductId2,
                        Product2Name = product2.Name,
                        ProductTypeName = product2.GetProductTypeLabel(_localizationService),
                        ProductTypeLabelHint = product2.ProductTypeLabelHint,
                        Product2Sku = product2.Sku,
                        Product2Published = product2.Published,
                        EditUrl = Url.Action("Edit", "Product", new { id = product2.Id })
                    };
                })
                .ToListAsync();

            model.Rows = crossSellProductsModel;
            model.Total = crossSellProductsModel.Count;

            return Json(model);
        }

        [Permission(Permissions.Catalog.Product.EditPromotion)]
        public async Task<IActionResult> CrossSellProductDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                numDeleted = await _db.CrossSellProducts
                    .AsQueryable()
                    .Where(x => ids.Contains(x.Id))
                    .BatchDeleteAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditPromotion)]
        public async Task<IActionResult> CrossSellProductAdd(int productId, int[] selectedProductIds)
        {
            var products = await _db.Products
                .AsNoTracking()
                .Where(x => selectedProductIds.Contains(x.Id))
                .ToListAsync();

            foreach (var product in products.OrderBySequence(selectedProductIds))
            {
                var existingRelations = await _db.CrossSellProducts
                    .ApplyProductId1Filter(productId)
                    .ToListAsync();
                
                if (FindCrossSellProduct(existingRelations, productId, product.Id) == null)
                {
                    _db.CrossSellProducts.Add(new CrossSellProduct
                    {
                        ProductId1 = productId,
                        ProductId2 = product.Id
                    });
                }
            }

            await _db.SaveChangesAsync();

            return new EmptyResult();
        }

        #endregion

        #region Associated products

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> AssociatedProductList(GridCommand command, int productId)
        {
            var model = new GridModel<ProductModel.AssociatedProductModel>();
            var searchQuery = new CatalogSearchQuery().HasParentGroupedProduct(productId);
            var query = _catalogSearchService.PrepareQuery(searchQuery);
            var associatedProducts = await query
                .OrderBy(p => p.DisplayOrder)
                .ApplyGridCommand(command)
                .ToListAsync();

            var associatedProductsModel = associatedProducts.Select(x =>
            {
                return new ProductModel.AssociatedProductModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    ProductTypeName = x.GetProductTypeLabel(_localizationService),
                    ProductTypeLabelHint = x.ProductTypeLabelHint,
                    DisplayOrder = x.DisplayOrder,
                    Sku = x.Sku,
                    Published = x.Published,
                    EditUrl = Url.Action("Edit", "Product", new { id = x.Id })
                };
            })
            .ToList();

            model.Rows = associatedProductsModel;
            model.Total = associatedProductsModel.Count;

            return Json(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditAssociatedProduct)]
        public async Task<IActionResult> AssociatedProductUpdate(ProductModel.RelatedProductModel model)
        {
            var relatedProduct = await _db.Products.FindByIdAsync(model.Id);
            relatedProduct.DisplayOrder = model.DisplayOrder;

            try
            {
                await _db.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                NotifyError(ex.GetInnerMessage());
                return Json(new { success = false });
            }
        }

        [Permission(Permissions.Catalog.Product.EditAssociatedProduct)]
        public async Task<IActionResult> AssociatedProductDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                var products = await _db.Products
                    .AsQueryable()
                    .Where(x => ids.Contains(x.Id))
                    .ToListAsync();

                foreach(var product in products)
                {
                    product.ParentGroupedProductId = 0;
                    numDeleted++;
                }

                await _db.SaveChangesAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditAssociatedProduct)]
        public async Task<IActionResult> AssociatedProductAdd(int productId, int[] selectedProductIds)
        {
            var searchQuery = new CatalogSearchQuery().HasParentGroupedProduct(productId);
            var query = _catalogSearchService.PrepareQuery(searchQuery);
            var maxDisplayOrder = query
                .Select(x => x.DisplayOrder)
                .OrderByDescending(x => x)
                .FirstOrDefault();

            var products = await _db.Products
                .AsQueryable()
                .Where(x => selectedProductIds.Contains(x.Id))
                .ToListAsync();

            foreach (var product in products)
            {
                if (product.ParentGroupedProductId != productId)
                {
                    product.ParentGroupedProductId = productId;
                    product.DisplayOrder = ++maxDisplayOrder;
                }
            }

            await _db.SaveChangesAsync();

            return new EmptyResult();
        }

        #endregion

        #region Bundle items

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> BundleItemList(GridCommand command, int productId)
        {
            var model = new GridModel<ProductModel.BundleItemModel>();
            var bundleItems = await _db.ProductBundleItem
                .AsNoTracking()
                .ApplyBundledProductsFilter(new[] { productId }, true)
                .Include(x => x.Product)
                .ApplyGridCommand(command)      
                .ToListAsync();
            
            var bundleItemsModel = bundleItems.Select(x =>
            {
                return new ProductModel.BundleItemModel
                {
                    Id = x.Id,
                    ProductId = x.Product.Id,
                    ProductName = x.Product.Name,
                    ProductTypeName = x.Product.GetProductTypeLabel(_localizationService),
                    ProductTypeLabelHint = x.Product.ProductTypeLabelHint,
                    Sku = x.Product.Sku,
                    Quantity = x.Quantity,
                    Discount = x.Discount,
                    DisplayOrder = x.DisplayOrder,
                    Visible = x.Visible,
                    Published = x.Published
                };
            }).ToList();

            model.Rows = bundleItemsModel;
            model.Total = bundleItemsModel.Count;

            return Json(model);
        }

        [Permission(Permissions.Catalog.Product.EditBundle)]
        public async Task<IActionResult> BundleItemDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                numDeleted = await _db.ProductBundleItem
                    .AsQueryable()
                    .Where(x => ids.Contains(x.Id))
                    .BatchDeleteAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditBundle)]
        public async Task<IActionResult> BundleItemAdd(int productId, int[] selectedProductIds)
        {
            var utcNow = DateTime.UtcNow;
            var products = await _db.Products.GetManyAsync(selectedProductIds, true);

            var maxDisplayOrder = await _db.ProductBundleItem
                .AsNoTracking()
                .ApplyBundledProductsFilter(new[] { productId }, true)
                .OrderByDescending(x => x.DisplayOrder)
                .Select(x => x.DisplayOrder)
                .FirstOrDefaultAsync();

            // TODO: (mh) (core) No notification if not CanBeBundleItem() :-/ > Fix that!
            foreach (var product in products.Where(x => x.CanBeBundleItem()))
            {
                var attributes = await _db.ProductVariantAttributes
                    .ApplyProductFilter(new[] { product.Id })
                    .ToListAsync();

                if (attributes.Count > 0 && attributes.Any(a => a.ProductVariantAttributeValues.Any(v => v.ValueType == ProductVariantAttributeValueType.ProductLinkage)))
                {
                    NotifyError(T("Admin.Catalog.Products.BundleItems.NoAttributeWithProductLinkage"));
                }
                else
                {
                    var bundleItem = new ProductBundleItem
                    {
                        ProductId = product.Id,
                        BundleProductId = productId,
                        Quantity = 1,
                        Visible = true,
                        Published = true,
                        DisplayOrder = ++maxDisplayOrder
                    };

                    _db.ProductBundleItem.Add(bundleItem);
                }
            }

            await _db.SaveChangesAsync();

            return new EmptyResult();
        }

        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> BundleItemEditPopup(int id, string btnId, string formId)
        {
            var bundleItem = await _db.ProductBundleItem
                .Include(x => x.BundleProduct)
                .Include(x => x.Product)
                .FindByIdAsync(id, false);

            if (bundleItem == null)
            {
                throw new ArgumentException("No bundle item found with the specified id");
            }

            var model = await MapperFactory.MapAsync<ProductBundleItem, ProductBundleItemModel>(bundleItem);
            await PrepareBundleItemEditModelAsync(model, bundleItem, btnId, formId);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Catalog.Product.EditBundle)]
        public async Task<IActionResult> BundleItemEditPopup(string btnId, string formId, bool continueEditing, ProductBundleItemModel model)
        {
            ViewBag.CloseWindow = !continueEditing;

            if (ModelState.IsValid)
            {
                var bundleItem = await _db.ProductBundleItem
                    .Include(x => x.BundleProduct)
                    .Include(x => x.Product)
                    .FindByIdAsync(model.Id);

                if (bundleItem == null)
                {
                    throw new ArgumentException("No bundle item found with the specified id");
                }

                await MapperFactory.MapAsync(model, bundleItem);
                await _db.SaveChangesAsync();

                foreach (var localized in model.Locales)
                {
                    await _localizedEntityService.ApplyLocalizedValueAsync(bundleItem, x => x.Name, localized.Name, localized.LanguageId);
                    await _localizedEntityService.ApplyLocalizedValueAsync(bundleItem, x => x.ShortDescription, localized.ShortDescription, localized.LanguageId);
                }

                if (bundleItem.FilterAttributes)
                {
                    // Only update filters if attribute filtering is activated to reduce payload.
                    await SaveFilteredAttributesAsync(bundleItem);
                }

                await PrepareBundleItemEditModelAsync(model, bundleItem, btnId, formId, true);

                if (continueEditing)
                {
                    NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));
                }
            }
            else
            {
                await PrepareBundleItemEditModelAsync(model, null, btnId, formId);
            }

            return View(model);
        }

        #endregion

        #region Product pictures

        [HttpPost]
        public async Task<IActionResult> SortPictures(string pictures, int entityId)
        {
            var response = new List<dynamic>();

            try
            {
                var files = await _db.ProductMediaFiles
                    .ApplyProductFilter(entityId)
                    .ToListAsync();

                var pictureIds = new HashSet<int>(pictures.SplitSafe(",").Select(x => Convert.ToInt32(x)));
                var ordinal = 5;

                foreach (var id in pictureIds)
                {
                    var productPicture = files.Where(x => x.Id == id).FirstOrDefault();
                    if (productPicture != null)
                    {
                        productPicture.DisplayOrder = ordinal;

                        // Add all relevant data of product picture to response.
                        dynamic file = new
                        {
                            DisplayOrder = productPicture.DisplayOrder,
                            MediaFileId = productPicture.MediaFileId,
                            EntityMediaId = productPicture.Id
                        };

                        response.Add(file);
                    }
                    ordinal += 5;
                }

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                NotifyError(ex.Message);
                return StatusCode(501, Json(ex.Message));
            }

            return Json(new { success = true, response });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditPicture)]
        public async Task<IActionResult> ProductMediaFilesAdd(string mediaFileIds, int entityId)
        {
            var ids = mediaFileIds
                .ToIntArray()
                .Distinct()
                .ToArray();

            if (!ids.Any())
            {
                throw new ArgumentException("Missing picture identifiers.");
            }

            var success = false;
            var product = await _db.Products.FindByIdAsync(entityId, false);
            if (product == null)
            {
                throw new ArgumentException(T("Products.NotFound", entityId));
            }

            var response = new List<dynamic>();
            var existingFiles = product.ProductPictures.Select(x => x.MediaFileId).ToList();
            var files = (await _mediaService.GetFilesByIdsAsync(ids, MediaLoadFlags.AsNoTracking)).ToDictionary(x => x.Id);

            foreach (var id in ids)
            {
                var exists = existingFiles.Contains(id);

                // No duplicate assignments!
                if (!exists)
                {
                    var productPicture = new ProductMediaFile
                    {
                        MediaFileId = id,
                        ProductId = entityId
                    };

                    // INFO: (mh) (core) SaveChanges must be done in foreach loop in order to get correct Ids.
                    _db.ProductMediaFiles.Add(productPicture);
                    await _db.SaveChangesAsync();

                    files.TryGetValue(id, out var file);

                    success = true;

                    dynamic respObj = new
                    {
                        MediaFileId = id,
                        ProductMediaFileId = productPicture.Id,
                        file?.Name
                    };

                    response.Add(respObj);
                }
            }

            return Json(new
            {
                success,
                response,
                message = T("Admin.Product.Picture.Added").JsValue.ToString()
            });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditPicture)]
        public async Task<IActionResult> ProductPictureDelete(int id)
        {
            var productPicture = await _db.ProductMediaFiles.FindByIdAsync(id);

            if (productPicture != null)
            {
                _db.ProductMediaFiles.Remove(productPicture);
                await _db.SaveChangesAsync();
            }

            // TODO: (mm) (mc) OPTIONALLY delete file!
            //var file = _mediaService.GetFileById(productPicture.MediaFileId);
            //if (file != null)
            //{
            //    _mediaService.DeleteFile(file.File, true);
            //}

            NotifySuccess(T("Admin.Catalog.Products.ProductPictures.Delete.Success"));
            return StatusCode((int)HttpStatusCode.OK);
        }

        #endregion

        #region Product specification attributes

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductSpecAttrList(GridCommand command, int productId)
        {
            var model = new GridModel<ProductSpecificationAttributeModel>();
            var productSpecAttributes = await _db.ProductSpecificationAttributes
                .AsNoTracking()
                .ApplyProductsFilter(new[] { productId })
                .ApplyGridCommand(command)
                .Include(x => x.SpecificationAttributeOption)
                .ThenInclude(x => x.SpecificationAttribute)
                //.OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            var specAttributeIds = productSpecAttributes.Select(x => x.SpecificationAttributeOption.SpecificationAttributeId).ToArray();
            var options = await _db.SpecificationAttributeOptions
                .AsNoTracking()
                .Where(x => specAttributeIds.Contains(x.SpecificationAttributeId))
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            var specOptions = options.ToMultimap(x => x.SpecificationAttributeId, x => x);

            var productSpecModel = productSpecAttributes
                .Select(x =>
                {
                    var attributeId = x.SpecificationAttributeOption.SpecificationAttributeId;
                    var psaModel = new ProductSpecificationAttributeModel
                    {
                        Id = x.Id,
                        SpecificationAttributeName = x.SpecificationAttributeOption.SpecificationAttribute.Name,
                        SpecificationAttributeOptionName = x.SpecificationAttributeOption.Name,
                        SpecificationAttributeId = attributeId,
                        SpecificationAttributeOptionId = x.SpecificationAttributeOptionId,
                        AllowFiltering = x.AllowFiltering,
                        ShowOnProductPage = x.ShowOnProductPage,
                        DisplayOrder = x.DisplayOrder
                    };

                    if (specOptions.ContainsKey(attributeId))
                    {
                        psaModel.SpecificationAttributeOptionsUrl = Url.Action("GetOptionsByAttributeId", "SpecificationAttribute", new { attributeId = attributeId });
                    }

                    return psaModel;
                })
                .ToList();

            model.Rows = productSpecModel;
            model.Total = productSpecModel.Count;

            return Json(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditAttribute)]
        public async Task<IActionResult> ProductSpecificationAttributeAdd(
            int specificationAttributeOptionId,
            bool? allowFiltering,
            bool? showOnProductPage,
            int displayOrder,
            int productId)
        {
            var success = false;
            var message = string.Empty;

            var psa = new ProductSpecificationAttribute
            {
                SpecificationAttributeOptionId = specificationAttributeOptionId,
                ProductId = productId,
                AllowFiltering = allowFiltering,
                ShowOnProductPage = showOnProductPage,
                DisplayOrder = displayOrder,
            };

            try
            {
                _db.ProductSpecificationAttributes.Add(psa);
                await _db.SaveChangesAsync();
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return Json(new { success, message });
        }

        [IgnoreAntiforgeryToken]
        [Permission(Permissions.Catalog.Product.EditAttribute)]
        public async Task<IActionResult> ProductSpecAttrUpdate(ProductSpecificationAttributeModel model)
        {
            var psa = await _db.ProductSpecificationAttributes.FindByIdAsync(model.Id);

            psa.AllowFiltering = model.AllowFiltering ?? false;
            psa.ShowOnProductPage = model.ShowOnProductPage ?? false;
            psa.DisplayOrder = model.DisplayOrder;
            psa.SpecificationAttributeOptionId = model.SpecificationAttributeOptionId;
            
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        [Permission(Permissions.Catalog.Product.EditAttribute)]
        public async Task<IActionResult> ProductSpecAttrDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                numDeleted = await _db.ProductSpecificationAttributes
                    .AsQueryable()
                    .Where(x => ids.Contains(x.Id))
                    .BatchDeleteAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        #endregion

        #region Tier prices

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditTierPrice)]
        public async Task<IActionResult> TierPriceList(GridCommand command, int productId)
        {
            var model = new GridModel<ProductModel.TierPriceModel>();
            var product = await _db.Products
                .Include(x => x.TierPrices)
                .FindByIdAsync(productId, false);

            string allRolesString = T("Admin.Catalog.Products.TierPrices.Fields.CustomerRole.AllRoles");
            string allStoresString = T("Admin.Common.StoresAll");
            string deletedString = $"[{T("Admin.Common.Deleted")}]";

            var customerRoles = new Dictionary<int, CustomerRole>();
            var stores = new Dictionary<int, Store>();

            if (product.TierPrices.Any())
            {
                var customerRoleIds = new HashSet<int>(product.TierPrices
                    .Select(x => x.CustomerRoleId ?? 0)
                    .Where(x => x != 0));

                var customerRolesQuery = _db.CustomerRoles
                    .AsNoTracking()
                    .ApplyStandardFilter(true)
                    .AsQueryable();

                customerRoles = (await customerRolesQuery
                    .Where(x => customerRoleIds.Contains(x.Id))
                    .ToListAsync())
                    .ToDictionary(x => x.Id);

                stores = _services.StoreContext.GetAllStores().ToDictionary(x => x.Id);
            }

            var tierPricesModel = product.TierPrices
                .AsQueryable()
                .Include(x => x.CustomerRole)
                .OrderBy(x => x.StoreId)
                .ThenBy(x => x.Quantity)
                .ThenBy(x => x.CustomerRoleId)
                .ApplyGridCommand(command)
                .ToList()
                .Select(x =>
                {
                    var tierPriceModel = new ProductModel.TierPriceModel
                    {
                        Id = x.Id,
                        StoreId = x.StoreId,
                        CustomerRoleId = x.CustomerRoleId ?? 0,
                        ProductId = x.ProductId,
                        Quantity = x.Quantity,
                        CalculationMethodId = (int)x.CalculationMethod,
                        Price1 = x.Price
                    };

                    tierPriceModel.CalculationMethod = x.CalculationMethod switch
                    {
                        TierPriceCalculationMethod.Fixed => T("Admin.Product.Price.Tierprices.Fixed").Value,
                        TierPriceCalculationMethod.Adjustment => T("Admin.Product.Price.Tierprices.Adjustment").Value,
                        TierPriceCalculationMethod.Percental => T("Admin.Product.Price.Tierprices.Percental").Value,
                        _ => x.CalculationMethod.ToString(),
                    };

                    if (x.CustomerRoleId.HasValue)
                    {
                        customerRoles.TryGetValue(x.CustomerRoleId.Value, out var role);
                        tierPriceModel.CustomerRole = role?.Name.NullEmpty() ?? allRolesString;
                    }
                    else
                    {
                        tierPriceModel.CustomerRole = allRolesString;
                    }

                    if (x.StoreId > 0)
                    {
                        stores.TryGetValue(x.StoreId, out var store);
                        tierPriceModel.Store = store?.Name.NullEmpty() ?? deletedString;
                    }
                    else
                    {
                        tierPriceModel.Store = allStoresString;
                    }

                    return tierPriceModel;
                })
                .ToList();

            model.Rows = tierPricesModel;
            model.Total = tierPricesModel.Count;

            return Json(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditTierPrice)]
        public async Task<IActionResult> TierPriceInsert(ProductModel.TierPriceModel model, int productId)
        {
            var product = await _db.Products
                .Include(x => x.TierPrices)
                .FindByIdAsync(productId, false);

            var tierPrice = new TierPrice
            {
                ProductId = product.Id,
                // Use Store property (not Store propertyId) because appropriate property is stored in it.
                StoreId = model.Store.ToInt(),
                // Use CustomerRole property (not CustomerRoleId) because appropriate property is stored in it.
                CustomerRoleId = model.CustomerRole.IsNumeric() && int.Parse(model.CustomerRole) != 0 ? int.Parse(model.CustomerRole) : null,
                Quantity = model.Quantity,
                Price = model.Price1 ?? 0,
                CalculationMethod = model.CalculationMethod == null ? TierPriceCalculationMethod.Fixed : (TierPriceCalculationMethod)int.Parse(model.CalculationMethod)
            };

            try
            {
                _db.TierPrices.Add(tierPrice);
                await _db.SaveChangesAsync();

                // TODO: (mh) (core) Remove comment after review.
                // Info: This will be taken care of in TierPriceHook.
                // Update "HasTierPrices" property.
                //_productService.UpdateHasTierPricesProperty(product);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                NotifyError(ex.GetInnerMessage());
                return Json(new { success = false });
            }
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditTierPrice)]
        public async Task<IActionResult> TierPriceUpdate(ProductModel.TierPriceModel model)
        {
            var tierPrice = await _db.TierPrices.FindByIdAsync(model.Id);

            // Use Store property (not Store propertyId) because appropriate property is stored in it.
            tierPrice.StoreId = model.Store.ToInt();
            // Use CustomerRole property (not CustomerRoleId) because appropriate property is stored in it.
            tierPrice.CustomerRoleId = model.CustomerRole.IsNumeric() && int.Parse(model.CustomerRole) != 0 ? int.Parse(model.CustomerRole) : null;
            tierPrice.Quantity = model.Quantity;
            tierPrice.Price = model.Price1 ?? 0;
            tierPrice.CalculationMethod = model.CalculationMethod == null ? TierPriceCalculationMethod.Fixed : (TierPriceCalculationMethod)int.Parse(model.CalculationMethod);

            try
            {
                await _db.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                NotifyError(ex.GetInnerMessage());
                return Json(new { success = false });
            }
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditTierPrice)]
        public async Task<IActionResult> TierPriceDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                numDeleted = await _db.TierPrices
                    .AsQueryable()
                    .Where(x => ids.Contains(x.Id))
                    .BatchDeleteAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        #endregion

        #region Product variant attributes

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductVariantAttributeList(GridCommand command, int productId)
        {
            var model = new GridModel<ProductModel.ProductVariantAttributeModel>();
            var productVariantAttributes = await _db.ProductVariantAttributes
                .ApplyProductFilter(new[] { productId })
                .ApplyGridCommand(command)
                .ToListAsync();
                
            var productVariantAttributesModel = await productVariantAttributes
                .SelectAsync(async x =>
                {
                    var attr = await _db.ProductAttributes.FindByIdAsync(x.ProductAttributeId);

                    var pvaModel = new ProductModel.ProductVariantAttributeModel
                    {
                        Id = x.Id,
                        ProductId = x.ProductId,
                        ProductAttribute = attr.Name,
                        ProductAttributeId = x.ProductAttributeId,
                        TextPrompt = x.TextPrompt,
                        CustomData = x.CustomData,
                        IsRequired = x.IsRequired,
                        AttributeControlType = await x.AttributeControlType.GetLocalizedEnumAsync(),
                        AttributeControlTypeId = x.AttributeControlTypeId,
                        DisplayOrder1 = x.DisplayOrder
                    };

                    if (x.ShouldHaveValues())
                    {
                        pvaModel.ValueCount = x.ProductVariantAttributeValues != null ? x.ProductVariantAttributeValues.Count : 0;
                        pvaModel.EditUrl = Url.Action("EditAttributeValues", "Product", new { productVariantAttributeId = x.Id });
                        pvaModel.EditText = T("Admin.Catalog.Products.ProductVariantAttributes.Attributes.Values.ViewLink", pvaModel.ValueCount);

                        if (x.ProductAttribute.ProductAttributeOptionsSets.Any())
                        {
                            var optionsSets = new StringBuilder($"<option>{T("Admin.Catalog.Products.ProductVariantAttributes.Attributes.Values.CopyOptions")}</option>");
                            x.ProductAttribute.ProductAttributeOptionsSets.Each(set => optionsSets.Append($"<option value=\"{set.Id}\">{set.Name}</option>"));
                            pvaModel.OptionsSets = optionsSets.ToString();
                        }
                    }

                    return pvaModel;
                })
                .AsyncToList();

            model.Rows = productVariantAttributesModel;
            model.Total = productVariantAttributesModel.Count;

            return Json(model);
        }

        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> ProductVariantAttributeInsert(ProductModel.ProductVariantAttributeModel model, int productId)
        {
            // TODO: (mh) (core) Throws if no attribute was selected (also in classic code). Fix it!

            var pva = new ProductVariantAttribute
            {
                ProductId = productId,
                ProductAttributeId = model.ProductAttributeId,
                TextPrompt = model.TextPrompt,
                CustomData = model.CustomData,
                IsRequired = model.IsRequired,
                AttributeControlTypeId = model.AttributeControlTypeId,
                DisplayOrder = model.DisplayOrder1
            };

            try
            {
                _db.ProductVariantAttributes.Add(pva);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Services.Notifier.Error(ex.Message);
                return Json(new { success = false });
            }

            return Json(new { success = true });
        }

        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> ProductVariantAttributeUpdate(ProductModel.ProductVariantAttributeModel model)
        {
            var pva = await _db.ProductVariantAttributes.FindByIdAsync(model.Id);

            pva.ProductAttributeId = model.ProductAttributeId;
            pva.TextPrompt = model.TextPrompt;
            pva.CustomData = model.CustomData;
            pva.IsRequired = model.IsRequired;
            pva.AttributeControlTypeId = model.AttributeControlTypeId;
            pva.DisplayOrder = model.DisplayOrder1;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                NotifyError(ex.Message);
                return Json(new { success = false });
            }

            return Json(new { success = true });
        }

        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> ProductVariantAttributeDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                numDeleted = await _db.ProductVariantAttributes
                    .AsQueryable()
                    .Where(x => ids.Contains(x.Id))
                    .BatchDeleteAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> CopyAttributeOptions(int productVariantAttributeId, int optionsSetId, bool deleteExistingValues)
        {
            var pva = await _db.ProductVariantAttributes.FindByIdAsync(productVariantAttributeId, false);

            if (pva == null)
            {
                NotifyError(T("Products.Variants.NotFound", productVariantAttributeId));
            }
            else
            {
                try
                {
                    var numberOfCopiedOptions = await _productAttributeService.CopyAttributeOptionsAsync(pva, optionsSetId, deleteExistingValues);

                    NotifySuccess(string.Concat(T("Admin.Common.TaskSuccessfullyProcessed"), " ",
                        T("Admin.Catalog.Products.ProductVariantAttributes.Attributes.Values.NumberOfCopiedOptions", numberOfCopiedOptions)));
                }
                catch (Exception ex)
                {
                    NotifyError(ex.Message);
                }
            }

            return Json(string.Empty);
        }

        #endregion

        #region Product variant attribute values

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductAttributeValueList(int productVariantAttributeId, GridCommand command)
        {
            var gridModel = new GridModel<ProductModel.ProductVariantAttributeValueModel>();
            var pva = await _db.ProductVariantAttributes.FindByIdAsync(productVariantAttributeId, false);
            var values = await _db.ProductVariantAttributeValues
                .AsNoTracking()
                .ApplyProductAttributeFilter(productVariantAttributeId)
                .ApplyGridCommand(command)
                .ToListAsync();

            gridModel.Rows = await values.SelectAsync(async x =>
            {
                var linkedProduct = await _db.Products.FindByIdAsync(x.LinkedProductId);

                var model = new ProductModel.ProductVariantAttributeValueModel
                {
                    Id = x.Id,
                    ProductVariantAttributeId = x.ProductVariantAttributeId,
                    Name = x.Name,
                    NameString = (x.Color.IsEmpty() ? x.Name : $"{x.Name} - {x.Color}").HtmlEncode(),
                    Alias = x.Alias,
                    Color = x.Color,
                    HasColor = !x.Color.IsEmpty(),
                    PictureId = x.MediaFileId,
                    PriceAdjustment = x.PriceAdjustment,
                    WeightAdjustment = x.WeightAdjustment,
                    PriceAdjustmentString = x.ValueType == ProductVariantAttributeValueType.Simple ? x.PriceAdjustment.ToString("G29") : string.Empty,
                    WeightAdjustmentString = x.ValueType == ProductVariantAttributeValueType.Simple ? x.WeightAdjustment.ToString("G29") : string.Empty,
                    IsPreSelected = x.IsPreSelected,
                    DisplayOrder = x.DisplayOrder,
                    ValueTypeId = x.ValueTypeId,
                    TypeName = await x.ValueType.GetLocalizedEnumAsync(),
                    TypeNameClass = x.ValueType == ProductVariantAttributeValueType.ProductLinkage ? "fa fa-link mr-2" : "d-none hide hidden-xs-up",
                    LinkedProductId = x.LinkedProductId,
                    Quantity = x.Quantity
                };

                if (linkedProduct != null)
                {
                    model.LinkedProductName = linkedProduct.GetLocalized(p => p.Name);
                    model.LinkedProductTypeName = linkedProduct.GetProductTypeLabel(_localizationService);
                    model.LinkedProductTypeLabelHint = linkedProduct.ProductTypeLabelHint;
                    model.LinkedProductEditUrl = Url.Action("Edit", "Product", new { id = linkedProduct.Id });

                    if (model.Quantity > 1)
                    {
                        model.QuantityInfo = $" × {model.Quantity}";
                    }
                }

                return model;
            }).AsyncToList();

            gridModel.Total = values.Count;

            return Json(gridModel);
        }

        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> EditAttributeValues(int productVariantAttributeId)
        {
            var pva = await _db.ProductVariantAttributes
                .Include(x => x.ProductAttribute)
                .FindByIdAsync(productVariantAttributeId, false);

            if (pva == null)
                throw new ArgumentException(T("Products.Variants.NotFound", productVariantAttributeId));

            var product = await _db.Products.FindByIdAsync(pva.ProductId, false);
            if (product == null)
                throw new ArgumentException(T("Products.NotFound", pva.ProductId));

            var model = new ProductModel.ProductVariantAttributeValueListModel
            {
                ProductName = product.Name,
                ProductId = pva.ProductId,
                ProductVariantAttributeName = pva.ProductAttribute.Name,
                ProductVariantAttributeId = pva.Id
            };

            return View(model);
        }

        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> ProductAttributeValueCreatePopup(string btnId, string formId, int productVariantAttributeId)
        {
            var pva = await _db.ProductVariantAttributes.FindByIdAsync(productVariantAttributeId, false);
            if (pva == null)
                throw new ArgumentException(T("Products.Variants.NotFound", productVariantAttributeId));

            var model = new ProductModel.ProductVariantAttributeValueModel
            {
                ProductId = pva.ProductId,
                ProductVariantAttributeId = productVariantAttributeId,
                IsListTypeAttribute = pva.IsListTypeAttribute(),
                Color = string.Empty,
                Quantity = 1
            };

            AddLocales(model.Locales);

            ViewBag.btnId = btnId;
            ViewBag.formId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> ProductAttributeValueCreatePopup(string btnId, string formId, ProductModel.ProductVariantAttributeValueModel model)
        {
            var pva = await _db.ProductVariantAttributes.FindByIdAsync(model.ProductVariantAttributeId);
            if (pva == null)
            {
                return RedirectToAction("List", "Product");
            }

            if (model.ValueTypeId == (int)ProductVariantAttributeValueType.ProductLinkage && await IsBundleItemAsync(pva.ProductId))
            {
                var product = await _db.Products.FindByIdAsync(pva.ProductId, false);
                var productName = product?.Name.NaIfEmpty();

                ModelState.AddModelError(string.Empty, T("Admin.Catalog.Products.BundleItems.NoProductLinkageForBundleItem", productName));
            }

            if (ModelState.IsValid)
            {
                var pvav = new ProductVariantAttributeValue();
                MiniMapper.Map(model, pvav);
                pvav.MediaFileId = model.PictureId;
                pvav.LinkedProductId = pvav.ValueType == ProductVariantAttributeValueType.Simple ? 0 : model.LinkedProductId;

                try
                {
                    _db.ProductVariantAttributeValues.Add(pvav);
                    await _db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    return View(model);
                }

                try
                {
                    await UpdateLocalesAsync(pvav, model);
                }
                catch { }

                ViewBag.RefreshPage = true;
                ViewBag.btnId = btnId;
                ViewBag.formId = formId;
                return View(model);
            }

            // If we got this far something failed. Redisplay form!
            return View(model);
        }

        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductAttributeValueEditPopup(string btnId, string formId, int id)
        {
            var pvav = await _db.ProductVariantAttributeValues
                .Include(x => x.ProductVariantAttribute)
                .FindByIdAsync(id, false);

            if (pvav == null)
            {
                return RedirectToAction("List", "Product");
            }

            var linkedProduct = await _db.Products.FindByIdAsync(pvav.LinkedProductId, false);

            var model = new ProductModel.ProductVariantAttributeValueModel
            {
                ProductId = pvav.ProductVariantAttribute.ProductId,
                ProductVariantAttributeId = pvav.ProductVariantAttributeId,
                Name = pvav.Name,
                Alias = pvav.Alias,
                Color = pvav.Color,
                PictureId = pvav.MediaFileId,
                IsListTypeAttribute = pvav.ProductVariantAttribute.IsListTypeAttribute(),
                PriceAdjustment = pvav.PriceAdjustment,
                WeightAdjustment = pvav.WeightAdjustment,
                IsPreSelected = pvav.IsPreSelected,
                DisplayOrder = pvav.DisplayOrder,
                ValueTypeId = pvav.ValueTypeId,
                TypeName = await pvav.ValueType.GetLocalizedEnumAsync(),
                TypeNameClass = pvav.ValueType == ProductVariantAttributeValueType.ProductLinkage ? "fa fa-link mr-2" : "d-none hide hidden-xs-up",
                LinkedProductId = pvav.LinkedProductId,
                Quantity = pvav.Quantity
            };

            if (linkedProduct != null)
            {
                model.LinkedProductName = linkedProduct.GetLocalized(p => p.Name);
                model.LinkedProductTypeName = linkedProduct.GetProductTypeLabel(_localizationService);
                model.LinkedProductTypeLabelHint = linkedProduct.ProductTypeLabelHint;
                model.LinkedProductEditUrl = Url.Action("Edit", "Product", new { id = linkedProduct.Id });

                if (model.Quantity > 1)
                {
                    model.QuantityInfo = $" × {model.Quantity}";
                }
            }

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Name = pvav.GetLocalized(x => x.Name, languageId, false, false);
                locale.Alias = pvav.GetLocalized(x => x.Alias, languageId, false, false);
            });

            ViewBag.btnId = btnId;
            ViewBag.formId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> ProductAttributeValueEditPopup(string btnId, string formId, ProductModel.ProductVariantAttributeValueModel model)
        {
            var pvav = await _db.ProductVariantAttributeValues.FindByIdAsync(model.Id);
            if (pvav == null)
            {
                return RedirectToAction("List", "Product");
            }

            if (model.ValueTypeId == (int)ProductVariantAttributeValueType.ProductLinkage && await IsBundleItemAsync(pvav.ProductVariantAttribute.ProductId))
            {
                var product = await _db.Products.FindByIdAsync(pvav.ProductVariantAttribute.ProductId, false);
                var productName = product?.Name.NaIfEmpty();

                ModelState.AddModelError(string.Empty, T("Admin.Catalog.Products.BundleItems.NoProductLinkageForBundleItem", productName));
            }

            if (ModelState.IsValid)
            {
                MiniMapper.Map(model, pvav);
                pvav.MediaFileId = model.PictureId;
                pvav.LinkedProductId = pvav.ValueType == ProductVariantAttributeValueType.Simple ? 0 : model.LinkedProductId;

                try
                {
                    await UpdateLocalesAsync(pvav, model);
                    await _db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    return View(model);
                }

                ViewBag.RefreshPage = true;
                ViewBag.btnId = btnId;
                ViewBag.formId = formId;
            }

            return View(model);
        }

        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> ProductAttributeValueDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                numDeleted = await _db.ProductVariantAttributeValues
                    .AsQueryable()
                    .Where(x => ids.Contains(x.Id))
                    .BatchDeleteAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        [NonAction]
        private async Task UpdateLocalesAsync(ProductVariantAttributeValue pvav, ProductModel.ProductVariantAttributeValueModel model)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(pvav, x => x.Name, localized.Name, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(pvav, x => x.Alias, localized.Alias, localized.LanguageId);
            }
        }

        #endregion

        #region Product variant attribute combinations

        private async Task PrepareProductAttributeCombinationModelAsync(
            ProductVariantAttributeCombinationModel model,
            ProductVariantAttributeCombination entity,
            Product product, bool formatAttributes = false)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var baseDimension = await _db.MeasureDimensions.FindByIdAsync(_measureSettings.BaseDimensionId);

            model.ProductId = product.Id;
            model.PrimaryStoreCurrencyCode = _services.StoreContext.CurrentStore.PrimaryStoreCurrency.CurrencyCode;
            model.BaseDimensionIn = baseDimension?.GetLocalized(x => x.Name) ?? string.Empty;

            if (entity == null)
            {
                // It's a new entity, so initialize it properly.
                model.StockQuantity = 10000;
                model.IsActive = true;
                model.AllowOutOfStockOrders = true;
            }

            if (formatAttributes && entity != null)
            {
                model.AttributesXml = await _productAttributeFormatter.FormatAttributesAsync(
                    entity.AttributeSelection, 
                    product, 
                    _workContext.CurrentCustomer, 
                    "<br />", 
                    includeHyperlinks: false);
            }
        }

        private async Task PrepareVariantCombinationAttributesAsync(ProductVariantAttributeCombinationModel model, Product product)
        {
            var productVariantAttributes = await _db.ProductVariantAttributes
                .AsNoTracking()
                .Include(x => x.ProductAttribute)
                .ApplyProductFilter(new[] { product.Id })
                .ToListAsync();
                
            foreach (var attribute in productVariantAttributes)
            {
                var pvaModel = new ProductVariantAttributeCombinationModel.ProductVariantAttributeModel()
                {
                    Id = attribute.Id,
                    ProductAttributeId = attribute.ProductAttributeId,
                    Name = attribute.ProductAttribute.Name,
                    TextPrompt = attribute.TextPrompt,
                    IsRequired = attribute.IsRequired,
                    AttributeControlType = attribute.AttributeControlType
                };

                if (attribute.ShouldHaveValues())
                {
                    var pvaValues = await _db.ProductVariantAttributeValues
                        .AsNoTracking()
                        .ApplyProductAttributeFilter(attribute.Id)
                        .ToListAsync();
                        
                    foreach (var pvaValue in pvaValues)
                    {
                        var pvaValueModel = new ProductVariantAttributeCombinationModel.ProductVariantAttributeValueModel()
                        {
                            Id = pvaValue.Id,
                            Name = pvaValue.Name,
                            IsPreSelected = pvaValue.IsPreSelected
                        };
                        pvaModel.Values.Add(pvaValueModel);
                    }
                }

                model.ProductVariantAttributes.Add(pvaModel);
            }
        }

        private async Task PrepareVariantCombinationPicturesAsync(ProductVariantAttributeCombinationModel model, Product product)
        {
            var files = (await _db.ProductMediaFiles
                .ApplyProductFilter(product.Id)
                .Include(x => x.MediaFile)
                .ToListAsync())
                .Select(x => x.MediaFile)
                .ToList(); ;
            
            foreach (var file in files)
            {
                model.AssignablePictures.Add(new ProductVariantAttributeCombinationModel.PictureSelectItemModel
                {
                    Id = file.Id,
                    IsAssigned = model.AssignedPictureIds.Contains(file.Id),
                    Media = _mediaService.ConvertMediaFile(file)
                });
            }
        }
        private void PrepareViewBag(string btnId, string formId, bool refreshPage = false, bool isEdit = true)
        {
            ViewBag.btnId = btnId;
            ViewBag.formId = formId;
            ViewBag.RefreshPage = refreshPage;
            ViewBag.IsEdit = isEdit;
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductVariantAttributeCombinationList(GridCommand command, int productId)
        {
            var model = new GridModel<ProductVariantAttributeCombinationModel>();
            var customer = _workContext.CurrentCustomer;
            var product = await _db.Products.FindByIdAsync(productId);
            var productUrlTitle = T("Common.OpenInShop");
            var productSeName = await product.GetActiveSlugAsync();
            var allCombinations = await _db.ProductVariantAttributeCombinations
                .AsNoTracking()
                .Where(x => x.ProductId == product.Id)
                .ApplyGridCommand(command)
                .ToListAsync();

            await _productAttributeMaterializer.PrefetchProductVariantAttributesAsync(allCombinations.Select(x => x.AttributeSelection));

            var productVariantAttributesModel = await allCombinations.SelectAsync(async x =>
            {
                var pvacModel = await MapperFactory.MapAsync<ProductVariantAttributeCombination, ProductVariantAttributeCombinationModel>(x);
                pvacModel.ProductId = product.Id;
                pvacModel.ProductUrlTitle = productUrlTitle;
                pvacModel.ProductUrl = await _productUrlHelper.GetProductUrlAsync(product.Id, productSeName, x.AttributeSelection);
                pvacModel.AttributesXml = await _productAttributeFormatter.FormatAttributesAsync(x.AttributeSelection, product, customer, "<br />", htmlEncode: false, includeHyperlinks: false);

                return pvacModel;
            })
            .AsyncToList();

            model.Rows = productVariantAttributesModel;
            model.Total = allCombinations.Count;

            return Json(model);
        }

        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> ProductVariantAttributeCombinationDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                numDeleted = await _db.ProductBundleItem
                    .AsQueryable()
                    .Where(x => ids.Contains(x.Id))
                    .BatchDeleteAsync();

                foreach(var id in ids)
                {
                    var pvac = await _db.ProductVariantAttributeCombinations.FindByIdAsync(id);
                    var productId = pvac.ProductId;

                    _db.ProductVariantAttributeCombinations.Remove(pvac);
                }

                await _db.SaveChangesAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> AttributeCombinationCreatePopup(string btnId, string formId, int productId)
        {
            var product = await _db.Products.FindByIdAsync(productId);
            if (product == null)
            {
                return RedirectToAction("List", "Product");
            }

            var model = new ProductVariantAttributeCombinationModel();
            await PrepareProductAttributeCombinationModelAsync(model, null, product);
            await PrepareVariantCombinationAttributesAsync(model, product);
            await PrepareVariantCombinationPicturesAsync(model, product);
            PrepareViewBag(btnId, formId, false, false);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> AttributeCombinationCreatePopup(
            string btnId,
            string formId,
            int productId,
            ProductVariantAttributeCombinationModel model,
            ProductVariantQuery query)
        {
            var product = await _db.Products.FindByIdAsync(productId);
            if (product == null)
            {
                return RedirectToAction("List", "Product");
            }

            var warnings = new List<string>();
            var variantAttributes = _db.ProductVariantAttributes.ApplyProductFilter(new[] { product.Id });

            // INFO: (mh) (core) Old code
            //var attributeXml = query.CreateSelectedAttributesXml(product.Id, 0, variantAttributes, _productAttributeParser, _localizationService,
            //    _downloadService, _catalogSettings, this.Request, warnings);

            //warnings.AddRange(_shoppingCartService.GetShoppingCartItemAttributeWarnings(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, product, attributeXml));

            // TODO: (mh) (core) What a fucked up mess! Nothing can be found easily...
            //var selection = await _productAttributeMaterializer.CreateAttributeSelectionAsync(query, variantAttributes, product.Id, 0);
            
            //var cart = await _shoppingCartService.GetCartAsync(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart);
            //await _shoppingCartValidator.ValidateProductAttributesAsync(new ShoppingCartItem { Product = product, AttributeSelection = selection.Selection }, cart.Items, warnings);

            // TODO: (mh) (core) Lets try this again when the code abaove was implemented correctly.
            //if (_productAttributeParser.FindProductVariantAttributeCombination(product.Id, attributeXml) != null)
            //{
            //    warnings.Add(_localizationService.GetResource("Admin.Catalog.Products.ProductVariantAttributes.AttributeCombinations.CombiExists"));
            //}

            if (warnings.Count == 0)
            {
                var combination = await MapperFactory.MapAsync<ProductVariantAttributeCombinationModel, ProductVariantAttributeCombination>(model);
                // TODO: (mh) (core) Lets try this again when the code above was implemented correctly.
                //combination.RawAttributes = attributeXml;
                combination.SetAssignedMediaIds(model.AssignedPictureIds);

                _db.ProductVariantAttributeCombinations.Add(combination);
                await _db.SaveChangesAsync();
            }

            await PrepareProductAttributeCombinationModelAsync(model, null, product);
            await PrepareVariantCombinationAttributesAsync(model, product);
            await PrepareVariantCombinationPicturesAsync(model, product);
            PrepareViewBag(btnId, formId, warnings.Count == 0, false);

            if (warnings.Count > 0)
            {
                model.Warnings = warnings;
            }

            return View(model);
        }

        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> AttributeCombinationEditPopup(int id, string btnId, string formId)
        {
            var combination = await _db.ProductVariantAttributeCombinations.FindByIdAsync(id, false);
            if (combination == null)
            {
                return RedirectToAction("List", "Product");
            }

            var product = await _db.Products.FindByIdAsync(combination.ProductId, false);
            if (product == null)
            {
                return RedirectToAction("List", "Product");
            }

            var model = await MapperFactory.MapAsync<ProductVariantAttributeCombination, ProductVariantAttributeCombinationModel>(combination);

            await PrepareProductAttributeCombinationModelAsync(model, combination, product, true);
            await PrepareVariantCombinationAttributesAsync(model, product);
            await PrepareVariantCombinationPicturesAsync(model, product);
            PrepareViewBag(btnId, formId);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> AttributeCombinationEditPopup(string btnId, string formId, ProductVariantAttributeCombinationModel model)
        {
            if (ModelState.IsValid)
            {
                var combination = await _db.ProductVariantAttributeCombinations.FindByIdAsync(model.Id);
                if (combination == null)
                {
                    return RedirectToAction("List", "Product");
                }

                var attributeXml = combination.RawAttributes;
                await MapperFactory.MapAsync(model, combination);
                combination.RawAttributes = attributeXml;
                combination.SetAssignedMediaIds(model.AssignedPictureIds);

                await _db.SaveChangesAsync();

                PrepareViewBag(btnId, formId, true);
            }

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> CreateAllAttributeCombinations(ProductVariantAttributeCombinationModel model, int productId)
        {
            var product = await _db.Products.FindByIdAsync(productId, false);   
            if (product == null)
            {
                throw new ArgumentException(T("Products.NotFound", productId));
            }

            await _productAttributeService.CreateAllAttributeCombinationsAsync(productId);

            return Json(string.Empty);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public async Task<IActionResult> DeleteAllAttributeCombinations(ProductVariantAttributeCombinationModel model, int productId)
        {
            var product = await _db.Products.FindByIdAsync(productId, false);
            if (product == null)
            {
                throw new ArgumentException(T("Products.NotFound", productId));
            }

            await _db.ProductVariantAttributeCombinations
                .AsQueryable()
                .Where(x => x.ProductId == product.Id)
                .BatchDeleteAsync();
            
            return Json(string.Empty);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> CombinationExistenceNote(int productId, ProductVariantQuery query)
        {
            var warnings = new List<string>();
            var attributes = await _db.ProductVariantAttributes.ApplyProductFilter(new[] { productId }).ToListAsync();

            // TODO: (mh) (core) Do this right!
            var exists = false;
            //var attributeXml = query.CreateSelectedAttributesXml(productId, 0, attributes, _productAttributeParser,
            //    _localizationService, _downloadService, _catalogSettings, Request, warnings);

            //var exists = _productAttributeParser.FindProductVariantAttributeCombination(productId, attributeXml) != null;
            //if (!exists)
            //{
            //    var product = await _db.Products.FindByIdAsync(productId, false);
            //    if (product != null)
            //    {
            //        warnings.AddRange(_shoppingCartService.GetShoppingCartItemAttributeWarnings(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, product, attributeXml));
            //    }
            //}

            if (warnings.Count > 0)
            {
                return new JsonResult( new { Message = warnings[0], HasWarning = true } );
            }

            return new JsonResult( 
                new 
                {
                    Message = T(exists ?
                        "Admin.Catalog.Products.ProductVariantAttributes.AttributeCombinations.CombiExists" :
                        "Admin.Catalog.Products.Variants.ProductVariantAttributes.AttributeCombinations.CombiNotExists"
                    ),
                    HasWarning = exists
                }
            );
        }

        #endregion

        #region Downloads

        [HttpPost]
        [Permission(Permissions.Media.Download.Delete)]
        public async Task<IActionResult> DeleteDownloadVersion(int downloadId)
        {
            var download = await _db.Downloads.FindByIdAsync(downloadId);
            if (download == null)
                return NotFound();

            _db.Downloads.Remove(download);
            await _db.SaveChangesAsync();
            
            return Json( new { success = true, Message = T("Admin.Common.TaskSuccessfullyProcessed").Value } );
        }

        #endregion

        #region Product tags

        // TODO: (mh) (core) Finish the job.

        #endregion

        #region Low stock reports

        // TODO: (mh) (core) Finish the job.

        #endregion

        #region Bulk editing

        // TODO: (mh) (core) Finish the job.

        #endregion

        #region Utitilies

        private async Task PrepareProductModelAsync(ProductModel model, Product product, bool setPredefinedValues, bool excludeProperties)
        {
            Guard.NotNull(model, nameof(model));

            if (product != null)
            {
                var parentGroupedProduct = await _db.Products.FindByIdAsync(product.ParentGroupedProductId, false);
                if (parentGroupedProduct != null)
                {
                    model.AssociatedToProductId = product.ParentGroupedProductId;
                    model.AssociatedToProductName = parentGroupedProduct.Name;
                }

                model.CreatedOn = _dateTimeHelper.ConvertToUserTime(product.CreatedOnUtc, DateTimeKind.Utc);
                model.UpdatedOn = _dateTimeHelper.ConvertToUserTime(product.UpdatedOnUtc, DateTimeKind.Utc);
                model.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(product);
                model.SelectedCustomerRoleIds = await _aclService.GetAuthorizedCustomerRoleIdsAsync(product);
                model.OriginalStockQuantity = product.StockQuantity;

                if (product.LimitedToStores)
                {
                    var storeMappings = await _storeMappingService.GetStoreMappingCollectionAsync(nameof(Product), new[] { product.Id });
                    if (storeMappings.FirstOrDefault(x => x.StoreId == _services.StoreContext.CurrentStore.Id) == null)
                    {
                        var storeMapping = storeMappings.FirstOrDefault();
                        if (storeMapping != null)
                        {
                            var store = _services.StoreContext.GetStoreById(storeMapping.StoreId);
                            if (store != null)
                                model.ProductUrl = store.Url.EnsureEndsWith("/") + await product.GetActiveSlugAsync();
                        }
                    }
                }

                if (model.ProductUrl.IsEmpty())
                {
                    model.ProductUrl = Url.RouteUrl("Product", new { SeName = await product.GetActiveSlugAsync() }, Request.Scheme);
                }

                // Downloads.
                var productDownloads = await _db.Downloads
                    .AsNoTracking()
                    .ApplyEntityFilter(nameof(Product), product.Id)
                    .Where(x => !string.IsNullOrEmpty(x.FileVersion))
                    .ToListAsync();
                
                model.DownloadVersions = productDownloads
                    .Select(x => new DownloadVersion
                    {
                        FileVersion = x.FileVersion,
                        DownloadId = x.Id,
                        FileName = x.UseDownloadUrl ? x.DownloadUrl : x.MediaFile?.Name,
                        DownloadUrl = x.UseDownloadUrl ? x.DownloadUrl : Url.Action("DownloadFile", "Download", new { downloadId = x.Id })
                    })
                    .ToList();

                var currentDownload = productDownloads.FirstOrDefault();

                model.DownloadId = currentDownload?.Id;
                model.CurrentDownload = currentDownload;
                if (currentDownload != null && currentDownload.MediaFile != null)
                {
                    model.DownloadThumbUrl = await _mediaService.GetUrlAsync(currentDownload.MediaFile.Id, _mediaSettings.CartThumbPictureSize, null, true);
                    currentDownload.DownloadUrl = Url.Action("DownloadFile", "Download", new { downloadId = currentDownload.Id });
                }

                model.DownloadFileVersion = (currentDownload?.FileVersion).EmptyNull();
                model.OldSampleDownloadId = model.SampleDownloadId;

                // Media files.
                var file = await _mediaService.GetFileByIdAsync(product.MainPictureId ?? 0);
                model.PictureThumbnailUrl = _mediaService.GetUrl(file, _mediaSettings.CartThumbPictureSize);
                model.NoThumb = file == null;

                await PrepareProductPictureModelAsync(model);
                model.AddPictureModel.PictureId = product.MainPictureId ?? 0;
            }

            model.PrimaryStoreCurrencyCode = _services.StoreContext.CurrentStore.PrimaryStoreCurrency.CurrencyCode;

            var measure = await _db.MeasureWeights.FindByIdAsync(_measureSettings.BaseWeightId, false);
            var dimension = await _db.MeasureDimensions.FindByIdAsync(_measureSettings.BaseDimensionId, false);

            model.BaseWeightIn = measure?.GetLocalized(x => x.Name) ?? string.Empty;
            model.BaseDimensionIn = dimension?.GetLocalized(x => x.Name) ?? string.Empty;

            model.NumberOfAvailableProductAttributes = await _db.ProductAttributes.CountAsync();
            model.NumberOfAvailableManufacturers = await _db.Manufacturers.CountAsync();
            model.NumberOfAvailableCategories = await _db.Categories.CountAsync();

            // Copy product.
            if (product != null)
            {
                model.CopyProductModel.Id = product.Id;
                model.CopyProductModel.Name = T("Admin.Common.CopyOf", product.Name);
                model.CopyProductModel.Published = true;
            }

            // Templates.
            var templates = await _db.ProductTemplates
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            ViewBag.AvailableProductTemplates = new List<SelectListItem>();
            foreach (var template in templates)
            {
                ViewBag.AvailableProductTemplates.Add(new SelectListItem
                {
                    Text = template.Name,
                    Value = template.Id.ToString()
                });
            }

            // Product tags.
            if (product != null)
            {
                model.ProductTags = product.ProductTags.Select(x => x.Name).ToArray();
            }

            var allTags = await _db.ProductTags
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToListAsync();

            ViewBag.AvailableProductTags = new MultiSelectList(allTags, model.ProductTags);

            // Tax categories.
            var taxCategories = await _db.TaxCategories
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            ViewBag.AvailableTaxCategories = new List<SelectListItem>();
            foreach (var tc in taxCategories)
            {
                ViewBag.AvailableTaxCategories.Add(new SelectListItem
                {
                    Text = tc.Name,
                    Value = tc.Id.ToString(),
                    Selected = product != null && !setPredefinedValues && tc.Id == product.TaxCategoryId
                });
            }

            // Do not pre-select a tax category that is not stored.
            if (product != null && product.TaxCategoryId == 0)
            {
                ViewBag.AvailableTaxCategories.Insert(0, new SelectListItem { Text = T("Common.PleaseSelect"), Value = "", Selected = true });
            }

            // Delivery times.
            if (setPredefinedValues)
            {
                var defaultDeliveryTime = await _db.DeliveryTimes
                    .AsNoTracking()
                    .Where(x => x.IsDefault == true)
                    .FirstOrDefaultAsync();

                model.DeliveryTimeId = defaultDeliveryTime?.Id;
            }

            // Quantity units.
            var quantityUnits = await _db.QuantityUnits
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            ViewBag.AvailableQuantityUnits = new List<SelectListItem>();
            foreach (var mu in quantityUnits)
            {
                ViewBag.AvailableQuantityUnits.Add(new SelectListItem
                {
                    Text = mu.Name,
                    Value = mu.Id.ToString(),
                    Selected = product != null && !setPredefinedValues && mu.Id == product.QuantityUnitId.GetValueOrDefault()
                });
            }

            // BasePrice aka PAnGV
            var measureUnitKeys = await _db.MeasureWeights.AsNoTracking().OrderBy(x => x.DisplayOrder).Select(x => x.SystemKeyword).ToListAsync();
            var measureDimensionKeys = await _db.MeasureDimensions.AsNoTracking().OrderBy(x => x.DisplayOrder).Select(x => x.SystemKeyword).ToListAsync();
            var measureUnits = measureUnitKeys.Concat(measureDimensionKeys).ToList();

            // Don't forget biz import!
            if (product != null && !setPredefinedValues && product.BasePriceMeasureUnit.HasValue() && !measureUnits.Exists(u => u.EqualsNoCase(product.BasePriceMeasureUnit)))
            {
                measureUnits.Add(product.BasePriceMeasureUnit);
            }

            ViewBag.AvailableMeasureUnits = new List<SelectListItem>();
            foreach (var mu in measureUnits)
            {
                ViewBag.AvailableMeasureUnits.Add(new SelectListItem
                {
                    Text = mu,
                    Value = mu,
                    Selected = product != null && !setPredefinedValues && mu.Equals(product.BasePriceMeasureUnit, StringComparison.OrdinalIgnoreCase)
                });
            }

            // Specification attributes.
            var specificationAttributes = await _db.SpecificationAttributes
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            var availableAttributes = new List<SelectListItem>();
            var availableOptions = new List<SelectListItem>();
            for (int i = 0; i < specificationAttributes.Count; i++)
            {
                var sa = specificationAttributes[i];
                availableAttributes.Add(new SelectListItem { Text = sa.Name, Value = sa.Id.ToString() });
                if (i == 0)
                {
                    var options = await _db.SpecificationAttributeOptions
                        .AsNoTracking()
                        .Where(x => x.SpecificationAttributeId == sa.Id)
                        .OrderBy(x => x.DisplayOrder)
                        .ToListAsync();

                    // Attribute options.
                    foreach (var sao in options)
                    {
                        availableOptions.Add(new SelectListItem { Text = sao.Name, Value = sao.Id.ToString() });
                    }
                }
            }

            ViewBag.AvailableAttributes = availableAttributes;
            ViewBag.AvailableOptions = availableOptions;
            
            if (product != null && !excludeProperties)
            {
                model.SelectedDiscountIds = product.AppliedDiscounts.Select(d => d.Id).ToArray();
            }

            var inventoryMethods = ((ManageInventoryMethod[])Enum.GetValues(typeof(ManageInventoryMethod))).Where(
                x => model.ProductTypeId != (int)ProductType.BundledProduct || x != ManageInventoryMethod.ManageStockByAttributes
            );

            ViewBag.AvailableManageInventoryMethods = new List<SelectListItem>();
            foreach (var inventoryMethod in inventoryMethods)
            {
                ViewBag.AvailableManageInventoryMethods.Add(new SelectListItem
                {
                    Value = ((int)inventoryMethod).ToString(),
                    Text = inventoryMethod.GetLocalizedEnum(),
                    Selected = ((int)inventoryMethod == model.ManageInventoryMethodId)
                });
            }

            ViewBag.AvailableCountries = await _db.Countries.AsNoTracking().ApplyStandardFilter(true)
                .Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString(),
                    Selected = product != null && x.Id == product.CountryOfOriginId
                })
                .ToListAsync();

            if (setPredefinedValues)
            {
                // TODO: These should be hidden settings.
                model.MaximumCustomerEnteredPrice = 1000;
                model.MaxNumberOfDownloads = 10;
                model.RecurringCycleLength = 100;
                model.RecurringTotalCycles = 10;
                model.StockQuantity = 10000;
                model.NotifyAdminForQuantityBelow = 1;
                model.OrderMinimumQuantity = 1;
                model.OrderMaximumQuantity = 100;
                model.QuantityStep = 1;
                model.HideQuantityControl = false;
                model.UnlimitedDownloads = true;
                model.IsShippingEnabled = true;
                model.AllowCustomerReviews = true;
                model.Published = true;
                model.HasPreviewPicture = false;
            }
        }

        private async Task PrepareProductPictureModelAsync(ProductModel model)
        {
            Guard.NotNull(model, nameof(model));

            var productPictures = await _db.ProductMediaFiles
                .AsNoTracking()
                .Include(x => x.MediaFile)
                .ApplyProductFilter(model.Id)
                .ToListAsync();
            
            model.ProductMediaFiles = productPictures
                .Select(x =>
                {
                    var media = new ProductMediaFile
                    {
                        Id = x.Id,
                        ProductId = x.ProductId,
                        MediaFileId = x.MediaFileId,
                        DisplayOrder = x.DisplayOrder,
                        MediaFile = x.MediaFile
                    };

                    return media;
                })
                .ToList();
        }

        private async Task PrepareBundleItemEditModelAsync(ProductBundleItemModel model, ProductBundleItem bundleItem, string btnId, string formId, bool refreshPage = false)
        {
            ViewBag.BtnId = btnId;
            ViewBag.FormId = formId;
            ViewBag.RefreshPage = refreshPage;

            if (bundleItem == null)
            {
                ViewBag.Title = _localizationService.GetResource("Admin.Catalog.Products.BundleItems.EditOf");
                return;
            }

            model.CreatedOn = _dateTimeHelper.ConvertToUserTime(bundleItem.CreatedOnUtc, DateTimeKind.Utc);
            model.UpdatedOn = _dateTimeHelper.ConvertToUserTime(bundleItem.UpdatedOnUtc, DateTimeKind.Utc);
            model.IsPerItemPricing = bundleItem.BundleProduct.BundlePerItemPricing;

            if (model.Locales.Count == 0)
            {
                AddLocales(model.Locales, (locale, languageId) =>
                {
                    locale.Name = bundleItem.GetLocalized(x => x.Name, languageId, false, false);
                    locale.ShortDescription = bundleItem.GetLocalized(x => x.ShortDescription, languageId, false, false);
                });
            }

            ViewBag.Title = $"{T("Admin.Catalog.Products.BundleItems.EditOf")} {bundleItem.Product.Name} ({bundleItem.Product.Sku})";

            var attributes = await _db.ProductVariantAttributes
                .AsNoTracking()
                .ApplyProductFilter(new[] { bundleItem.ProductId })
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();
            
            foreach (var attribute in attributes)
            {
                var attributeModel = new ProductBundleItemAttributeModel()
                {
                    Id = attribute.Id,
                    Name = attribute.ProductAttribute.Alias.HasValue() ? $"{attribute.ProductAttribute.Name} ({attribute.ProductAttribute.Alias})" : attribute.ProductAttribute.Name
                };

                var attributeValues = await _db.ProductVariantAttributeValues
                    .AsNoTracking()
                    .OrderBy(x => x.DisplayOrder)
                    .Where(x => x.ProductVariantAttributeId == attribute.Id)
                    .ToListAsync();
                
                foreach (var attributeValue in attributeValues)
                {
                    var filteredValue = bundleItem.AttributeFilters.FirstOrDefault(x => x.AttributeId == attribute.Id && x.AttributeValueId == attributeValue.Id);

                    attributeModel.Values.Add(new SelectListItem()
                    {
                        Text = attributeValue.Name,
                        Value = attributeValue.Id.ToString(),
                        Selected = (filteredValue != null)
                    });

                    if (filteredValue != null)
                    {
                        attributeModel.PreSelect.Add(new SelectListItem()
                        {
                            Text = attributeValue.Name,
                            Value = attributeValue.Id.ToString(),
                            Selected = filteredValue.IsPreSelected
                        });
                    }
                }

                if (attributeModel.Values.Count > 0)
                {
                    if (attributeModel.PreSelect.Count > 0)
                    {
                        attributeModel.PreSelect.Insert(0, new SelectListItem() { Text = T("Admin.Common.PleaseSelect") });
                    }
                    
                    model.Attributes.Add(attributeModel);
                }
            }
        }

        private async Task SaveFilteredAttributesAsync(ProductBundleItem bundleItem)
        {
            var form = Request.Form;

            await _db.ProductBundleItemAttributeFilter
                .AsQueryable()
                .Where(x => x.BundleItemId == bundleItem.Id)
                .BatchDeleteAsync();

            var allFilterKeys = form.Keys.Where(x => x.HasValue() && x.StartsWith(ProductBundleItemAttributeModel.AttributeControlPrefix));

            foreach (var key in allFilterKeys)
            {
                int attributeId = key[ProductBundleItemAttributeModel.AttributeControlPrefix.Length..].ToInt();
                string preSelectId = form[ProductBundleItemAttributeModel.PreSelectControlPrefix + attributeId.ToString()].ToString().EmptyNull();

                foreach (var valueId in form[key].ToString().SplitSafe(","))
                {
                    var attributeFilter = new ProductBundleItemAttributeFilter()
                    {
                        BundleItemId = bundleItem.Id,
                        AttributeId = attributeId,
                        AttributeValueId = valueId.ToInt(),
                        IsPreSelected = (preSelectId == valueId)
                    };

                    _db.ProductBundleItemAttributeFilter.Add(attributeFilter);
                }

                await _db.SaveChangesAsync();
            }
        }

        private async Task<bool> IsBundleItemAsync(int productId)
        {
            if (productId == 0)
            {
                return false;
            }

            var query =
                from pbi in _db.ProductBundleItem.AsNoTracking()
                join bundle in _db.Products.AsNoTracking() on pbi.BundleProductId equals bundle.Id
                where pbi.ProductId == productId && !bundle.Deleted
                select pbi;

            var result = await query.AnyAsync();
            return result;
        }

        #endregion

        #region Update[...]

        protected async Task<bool> MapModelToProductAsync(ProductModel model, Product product, IFormCollection form)
        {
            if (model.LoadedTabs == null || model.LoadedTabs.Length == 0)
            {
                model.LoadedTabs = new string[] { "Info" };
            }

            var nameChanged = false;

            foreach (var tab in model.LoadedTabs)
            {
                switch (tab.ToLowerInvariant())
                {
                    case "info":
                        UpdateProductGeneralInfo(product, model, out nameChanged);
                        break;
                    case "inventory":
                        await UpdateProductInventoryAsync(product, model);
                        break;
                    case "bundleitems":
                        await UpdateProductBundleItemsAsync(product, model);
                        break;
                    case "price":
                        await UpdateProductPriceAsync(product, model);
                        break;
                    case "attributes":
                        UpdateProductAttributes(product, model);
                        break;
                    case "downloads":
                        await UpdateProductDownloadsAsync(product, model);
                        break;
                    case "pictures":
                        UpdateProductPictures(product, model);
                        break;
                    case "seo":
                        await UpdateProductSeoAsync(product, model);
                        break;
                }
            }

            await _eventPublisher.PublishAsync(new ModelBoundEvent(model, product, form));

            return nameChanged;
        }

        protected void UpdateProductGeneralInfo(Product product, ProductModel model, out bool nameChanged)
        {
            var p = product;
            var m = model;

            p.ProductTypeId = m.ProductTypeId;
            p.Visibility = m.Visibility;
            p.Condition = m.Condition;
            p.ProductTemplateId = m.ProductTemplateId;

            nameChanged = !string.Equals(p.Name, m.Name, StringComparison.CurrentCultureIgnoreCase);

            p.Name = m.Name;
            p.ShortDescription = m.ShortDescription;
            p.FullDescription = m.FullDescription;
            p.Sku = m.Sku;
            p.ManufacturerPartNumber = m.ManufacturerPartNumber;
            p.Gtin = m.Gtin;
            p.AdminComment = m.AdminComment;
            p.AvailableStartDateTimeUtc = m.AvailableStartDateTimeUtc;
            p.AvailableEndDateTimeUtc = m.AvailableEndDateTimeUtc;

            p.AllowCustomerReviews = m.AllowCustomerReviews;
            p.ShowOnHomePage = m.ShowOnHomePage;
            p.HomePageDisplayOrder = m.HomePageDisplayOrder;
            p.Published = m.Published;
            p.RequireOtherProducts = m.RequireOtherProducts;
            p.RequiredProductIds = m.RequiredProductIds;
            p.AutomaticallyAddRequiredProducts = m.AutomaticallyAddRequiredProducts;

            p.IsGiftCard = m.IsGiftCard;
            p.GiftCardTypeId = m.GiftCardTypeId;

            p.IsRecurring = m.IsRecurring;
            p.RecurringCycleLength = m.RecurringCycleLength;
            p.RecurringCyclePeriodId = m.RecurringCyclePeriodId;
            p.RecurringTotalCycles = m.RecurringTotalCycles;

            p.IsShippingEnabled = m.IsShippingEnabled;
            p.DeliveryTimeId = m.DeliveryTimeId == 0 ? null : m.DeliveryTimeId;
            p.QuantityUnitId = m.QuantityUnitId == 0 ? null : m.QuantityUnitId;
            p.IsFreeShipping = m.IsFreeShipping;
            p.AdditionalShippingCharge = m.AdditionalShippingCharge ?? 0;
            p.Weight = m.Weight ?? 0;
            p.Length = m.Length ?? 0;
            p.Width = m.Width ?? 0;
            p.Height = m.Height ?? 0;

            p.IsEsd = m.IsEsd;
            p.IsTaxExempt = m.IsTaxExempt;
            p.TaxCategoryId = m.TaxCategoryId ?? 0;
            p.CustomsTariffNumber = m.CustomsTariffNumber;
            p.CountryOfOriginId = m.CountryOfOriginId == 0 ? null : m.CountryOfOriginId;

            p.AvailableEndDateTimeUtc = p.AvailableEndDateTimeUtc.ToEndOfTheDay();
            p.SpecialPriceEndDateTimeUtc = p.SpecialPriceEndDateTimeUtc.ToEndOfTheDay();
        }

        protected async Task UpdateProductDownloadsAsync(Product product, ProductModel model)
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Media.Download.Update))
            {
                return;
            }

            var p = product;
            var m = model;

            p.IsDownload = m.IsDownload;
            //p.DownloadId = m.DownloadId ?? 0;
            p.UnlimitedDownloads = m.UnlimitedDownloads;
            p.MaxNumberOfDownloads = m.MaxNumberOfDownloads;
            p.DownloadExpirationDays = m.DownloadExpirationDays;
            p.DownloadActivationTypeId = m.DownloadActivationTypeId;
            p.HasUserAgreement = m.HasUserAgreement;
            p.UserAgreementText = m.UserAgreementText;
            p.HasSampleDownload = m.HasSampleDownload;
            p.SampleDownloadId = m.SampleDownloadId == 0 ? null : m.SampleDownloadId;
        }

        protected async Task UpdateProductInventoryAsync(Product product, ProductModel model)
        {
            var p = product;
            var m = model;
            var updateStockQuantity = true;
            var stockQuantityInDatabase = product.StockQuantity;

            if (p.ManageInventoryMethod == ManageInventoryMethod.ManageStock && p.Id != 0)
            {
                if (m.OriginalStockQuantity != stockQuantityInDatabase)
                {
                    // The stock has changed since the edit page was loaded, e.g. because an order has been placed.
                    updateStockQuantity = false;

                    if (m.StockQuantity != m.OriginalStockQuantity)
                    {
                        // The merchant has changed the stock quantity manually.
                        NotifyWarning(T("Admin.Catalog.Products.StockQuantityNotChanged", stockQuantityInDatabase.ToString("N0")));
                    }
                }
            }

            if (updateStockQuantity)
            {
                p.StockQuantity = m.StockQuantity;
            }

            p.ManageInventoryMethodId = m.ManageInventoryMethodId;
            p.DisplayStockAvailability = m.DisplayStockAvailability;
            p.DisplayStockQuantity = m.DisplayStockQuantity;
            p.MinStockQuantity = m.MinStockQuantity;
            p.LowStockActivityId = m.LowStockActivityId;
            p.NotifyAdminForQuantityBelow = m.NotifyAdminForQuantityBelow;
            p.BackorderModeId = m.BackorderModeId;
            p.AllowBackInStockSubscriptions = m.AllowBackInStockSubscriptions;
            p.OrderMinimumQuantity = m.OrderMinimumQuantity;
            p.OrderMaximumQuantity = m.OrderMaximumQuantity;
            p.QuantityStep = m.QuantityStep;
            p.HideQuantityControl = m.HideQuantityControl;

            if (p.ManageInventoryMethod == ManageInventoryMethod.ManageStock && updateStockQuantity)
            {
                // Back in stock notifications.
                if (p.BackorderMode == BackorderMode.NoBackorders &&
                    p.AllowBackInStockSubscriptions &&
                    p.StockQuantity > 0 &&
                    stockQuantityInDatabase <= 0 &&
                    p.Published &&
                    !p.Deleted &&
                    !p.IsSystemProduct)
                {
                    await _stockSubscriptionService.SendNotificationsToSubscribersAsync(p);
                }

                if (p.StockQuantity != stockQuantityInDatabase)
                {
                    await _productService.AdjustInventoryAsync(p, null, true, 0);
                }
            }
        }

        protected async Task UpdateProductBundleItemsAsync(Product product, ProductModel model)
        {
            var p = product;
            var m = model;

            p.BundleTitleText = m.BundleTitleText;
            p.BundlePerItemPricing = m.BundlePerItemPricing;
            p.BundlePerItemShipping = m.BundlePerItemShipping;
            p.BundlePerItemShoppingCart = m.BundlePerItemShoppingCart;

            // SEO
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(product, x => x.BundleTitleText, localized.BundleTitleText, localized.LanguageId);
            }
        }

        protected async Task UpdateProductPriceAsync(Product product, ProductModel model)
        {
            var p = product;
            var m = model;

            p.Price = m.Price;
            p.OldPrice = m.OldPrice ?? 0;
            p.ProductCost = m.ProductCost ?? 0;
            p.SpecialPrice = m.SpecialPrice;
            p.SpecialPriceStartDateTimeUtc = m.SpecialPriceStartDateTimeUtc;
            p.SpecialPriceEndDateTimeUtc = m.SpecialPriceEndDateTimeUtc;
            p.DisableBuyButton = m.DisableBuyButton;
            p.DisableWishlistButton = m.DisableWishlistButton;
            p.AvailableForPreOrder = m.AvailableForPreOrder;
            p.CallForPrice = m.CallForPrice;
            p.CustomerEntersPrice = m.CustomerEntersPrice;
            p.MinimumCustomerEnteredPrice = m.MinimumCustomerEnteredPrice ?? 0;
            p.MaximumCustomerEnteredPrice = m.MaximumCustomerEnteredPrice ?? 0;

            p.BasePriceEnabled = m.BasePriceEnabled;
            p.BasePriceBaseAmount = m.BasePriceBaseAmount;
            p.BasePriceAmount = m.BasePriceAmount;
            p.BasePriceMeasureUnit = m.BasePriceMeasureUnit;

            // Discounts.
            var allDiscounts = await _discountService.GetAllDiscountsAsync(DiscountType.AssignedToSkus, null, true);
            foreach (var discount in allDiscounts)
            {
                if (model.SelectedDiscountIds != null && model.SelectedDiscountIds.Contains(discount.Id))
                {
                    if (!product.AppliedDiscounts.Any(d => d.Id == discount.Id))
                        product.AppliedDiscounts.Add(discount);
                }
                else
                {
                    if (product.AppliedDiscounts.Any(d => d.Id == discount.Id))
                        product.AppliedDiscounts.Remove(discount);
                }
            }
        }

        protected void UpdateProductAttributes(Product product, ProductModel model)
        {
            product.AttributeChoiceBehaviour = model.AttributeChoiceBehaviour;
        }

        protected async Task UpdateProductSeoAsync(Product product, ProductModel model)
        {
            var p = product;
            var m = model;

            p.MetaKeywords = m.MetaKeywords;
            p.MetaDescription = m.MetaDescription;
            p.MetaTitle = m.MetaTitle;

            var service = _localizedEntityService;
            foreach (var localized in model.Locales)
            {
                await service.ApplyLocalizedValueAsync(product, x => x.MetaKeywords, localized.MetaKeywords, localized.LanguageId);
                await service.ApplyLocalizedValueAsync(product, x => x.MetaDescription, localized.MetaDescription, localized.LanguageId);
                await service.ApplyLocalizedValueAsync(product, x => x.MetaTitle, localized.MetaTitle, localized.LanguageId);
            }
        }

        protected void UpdateProductPictures(Product product, ProductModel model)
        {
            product.HasPreviewPicture = model.HasPreviewPicture;
        }

        private async Task UpdateDataOfExistingProductAsync(Product product, ProductModel model, bool editMode, bool nameChanged)
        {
            var p = product;
            var m = model;

            //var seoTabLoaded = m.LoadedTabs.Contains("SEO", StringComparer.OrdinalIgnoreCase);

            // SEO.
            var validateSlugResult = await p.ValidateSlugAsync(p.Name, true, 0);
            m.SeName = validateSlugResult.Slug;
            await _urlService.ApplySlugAsync(validateSlugResult);

            if (editMode)
            {
                _db.Products.Update(p);
                await _db.SaveChangesAsync();
            }

            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(product, x => x.Name, localized.Name, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(product, x => x.ShortDescription, localized.ShortDescription, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(product, x => x.FullDescription, localized.FullDescription, localized.LanguageId);
                
                validateSlugResult = await p.ValidateSlugAsync(localized.Name, false, localized.LanguageId);
                await _urlService.ApplySlugAsync(validateSlugResult);
            }

            await _productTagService.UpdateProductTagsAsync(p, m.ProductTags);

            await SaveStoreMappingsAsync(p, model.SelectedStoreIds);
            await SaveAclMappingsAsync(p, model.SelectedCustomerRoleIds);
        }

        #endregion

        #region Hidden normalizers

        // TODO: (mh) (core) Implement FixProductMainPictureIds
        //[Permission(Permissions.Catalog.Product.Update)]
        //public ActionResult FixProductMainPictureIds(DateTime? ifModifiedSinceUtc = null)
        //{
        //    var count = DataMigrator.FixProductMainPictureIds(_dbContext, ifModifiedSinceUtc);

        //    return Content("Fixed {0} ids.".FormatInvariant(count));
        //}

        #endregion
    }
}
