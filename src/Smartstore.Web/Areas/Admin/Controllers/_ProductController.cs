using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net;
using System.Threading.Tasks;
using AngleSharp.Common;
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
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public partial class ProductController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly IProductService _productService;           
        private readonly IUrlService _urlService;                   
        private readonly IWorkContext _workContext;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IMediaService _mediaService;
        private readonly IProductTagService _productTagService;
        private readonly IAclService _aclService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IDiscountService _discountService;
        private readonly Lazy<IProductCloner> _productCloner;
        private readonly Lazy<ICategoryService> _categoryService;
        private readonly Lazy<IManufacturerService> _manufacturerService;
        private readonly Lazy<IProductAttributeService> _productAttributeService;
        private readonly Lazy<IProductAttributeMaterializer> _productAttributeMaterializer;
        private readonly Lazy<IStockSubscriptionService> _stockSubscriptionService;
        private readonly Lazy<IShoppingCartService> _shoppingCartService;
        private readonly Lazy<IShoppingCartValidator> _shoppingCartValidator;
        private readonly Lazy<IProductAttributeFormatter> _productAttributeFormatter;
        private readonly Lazy<IDownloadService> _downloadService;
        private readonly Lazy<ICatalogSearchService> _catalogSearchService;
        private readonly Lazy<ProductUrlHelper> _productUrlHelper;
        private readonly AdminAreaSettings _adminAreaSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly MeasureSettings _measureSettings;
        private readonly SeoSettings _seoSettings;
        private readonly MediaSettings _mediaSettings;
        private readonly SearchSettings _searchSettings;

        public ProductController(
            SmartDbContext db,
            IProductService productService,
            IUrlService urlService,
            IWorkContext workContext,
            ILocalizedEntityService localizedEntityService,
            IMediaService mediaService,
            IProductTagService productTagService,
            IAclService aclService,
            IStoreMappingService storeMappingService,
            IDateTimeHelper dateTimeHelper,
            IDiscountService discountService,
            Lazy<IProductCloner> productCloner,
            Lazy<ICategoryService> categoryService,
            Lazy<IManufacturerService> manufacturerService,
            Lazy<IProductAttributeService> productAttributeService,
            Lazy<IProductAttributeMaterializer> productAttributeMaterializer,
            Lazy<IStockSubscriptionService> stockSubscriptionService,
            Lazy<IShoppingCartService> shoppingCartService,
            Lazy<IShoppingCartValidator> shoppingCartValidator,
            Lazy<IProductAttributeFormatter> productAttributeFormatter,
            Lazy<IDownloadService> downloadService,
            Lazy<ICatalogSearchService> catalogSearchService,
            Lazy<ProductUrlHelper> productUrlHelper,
            AdminAreaSettings adminAreaSettings,
            CatalogSettings catalogSettings,
            MeasureSettings measureSettings,
            SeoSettings seoSettings,
            MediaSettings mediaSettings,
            SearchSettings searchSettings)
        {
            _db = db;
            _productService = productService;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _urlService = urlService;
            _workContext = workContext;
            _localizedEntityService = localizedEntityService;
            _mediaService = mediaService;
            _productTagService = productTagService;
            _productCloner = productCloner;
            _aclService = aclService;
            _storeMappingService = storeMappingService;
            _dateTimeHelper = dateTimeHelper;
            _discountService = discountService;
            _productAttributeService = productAttributeService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _stockSubscriptionService = stockSubscriptionService;
            _shoppingCartService = shoppingCartService;
            _shoppingCartValidator = shoppingCartValidator;
            _productAttributeFormatter = productAttributeFormatter;
            _downloadService = downloadService;
            _catalogSearchService = catalogSearchService;
            _productUrlHelper = productUrlHelper;
            _adminAreaSettings = adminAreaSettings;
            _catalogSettings = catalogSettings;
            _measureSettings = measureSettings;
            _seoSettings = seoSettings;
            _mediaSettings = mediaSettings;
            _searchSettings = searchSettings;
        }

        #region Product categories

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductCategoryList(int productId)
        {
            var model = new GridModel<ProductModel.ProductCategoryModel>();
            var productCategories = await _categoryService.Value.GetProductCategoriesByProductIdsAsync(new[] { productId }, true);
            var productCategoriesModel = await productCategories
                .AsQueryable()
                .SelectAsync(async x =>
                {
                    var node = await _categoryService.Value.GetCategoryTreeAsync(x.CategoryId, true);
                    return new ProductModel.ProductCategoryModel
                    {
                        Id = x.Id,
                        Category = node != null ? _categoryService.Value.GetCategoryPath(node, aliasPattern: "<span class='badge badge-secondary'>{0}</span>") : string.Empty,
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
            var alreadyAssigned = await _db.ProductCategories.AnyAsync(x => x.CategoryId == model.CategoryId && x.ProductId == productId);

            if (alreadyAssigned)
            {
                NotifyError(T("Admin.Catalog.Products.Categories.NoDuplicatesAllowed"));
                return Json(new { success = false });
            }

            var productCategory = new ProductCategory
            {
                ProductId = productId,
                CategoryId = model.CategoryId,
                IsFeaturedProduct = model.IsFeaturedProduct,
                DisplayOrder = model.DisplayOrder
            };

            try
            {
                _db.ProductCategories.Add(productCategory);

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
            var alreadyAssigned = await _db.ProductCategories.AnyAsync(x => x.CategoryId == model.CategoryId && x.ProductId == model.ProductId);

            if (alreadyAssigned)
            {
                NotifyError(T("Admin.Catalog.Products.Categories.NoDuplicatesAllowed"));
                return Json(new { success = false });
            }

            var productCategory = await _db.ProductCategories.FindByIdAsync(model.Id);
            var categoryChanged = model.CategoryId != productCategory.CategoryId;

            productCategory.CategoryId = model.CategoryId;
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
                var toDelete = await _db.ProductCategories
                    .Where(x => ids.Contains(x.Id))
                    .ToListAsync();

                numDeleted = toDelete.Count;

                _db.ProductCategories.RemoveRange(toDelete);
                await _db.SaveChangesAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        #endregion

        #region Product manufacturers

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductManufacturerList(int productId)
        {
            var model = new GridModel<ProductModel.ProductManufacturerModel>();
            var productManufacturers = await _manufacturerService.Value.GetProductManufacturersByProductIdsAsync(new[] { productId }, true);
            var productManufacturersModel = productManufacturers
                .AsQueryable()
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
            var alreadyAssigned = await _db.ProductManufacturers.AnyAsync(x => x.ManufacturerId == model.ManufacturerId && x.ProductId == productId);

            if (alreadyAssigned)
            {
                NotifyError(T("Admin.Catalog.Products.Manufacturers.NoDuplicatesAllowed"));
                return Json(new { success = false });
            }

            var productManufacturer = new ProductManufacturer
            {
                ProductId = productId,
                ManufacturerId = model.ManufacturerId,
                IsFeaturedProduct = model.IsFeaturedProduct,
                DisplayOrder = model.DisplayOrder
            };

            try
            {
                _db.ProductManufacturers.Add(productManufacturer);

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
            var alreadyAssigned = await _db.ProductManufacturers.AnyAsync(x => x.ManufacturerId == model.ManufacturerId && x.ProductId == model.ProductId);

            if (alreadyAssigned)
            {
                NotifyError(T("Admin.Catalog.Products.Manufacturers.NoDuplicatesAllowed"));
                return Json(new { success = false });
            }

            var productManufacturer = await _db.ProductManufacturers.FindByIdAsync(model.Id);
            var manufacturerChanged = model.ManufacturerId != productManufacturer.ManufacturerId;
            productManufacturer.ManufacturerId = model.ManufacturerId;
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
                var toDelete = await _db.ProductManufacturers
                    .Where(x => ids.Contains(x.Id))
                    .ToListAsync();

                numDeleted = toDelete.Count;

                _db.ProductManufacturers.RemoveRange(toDelete);
                await _db.SaveChangesAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
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
                            productPicture.DisplayOrder,
                            productPicture.MediaFileId,
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

        #region Tier prices

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditTierPrice)]
        public async Task<IActionResult> TierPriceList(GridCommand command, int productId)
        {
            var model = new GridModel<ProductModel.TierPriceModel>();
            var product = await _db.Products
                .Include(x => x.TierPrices)
                .ThenInclude(x => x.CustomerRole)
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

                stores = Services.StoreContext.GetAllStores().ToDictionary(x => x.Id);
            }

            // TODO: (mh) (core) You can't "Include" anything in plain LINQ, only in EF-Linq.
            // TODO: (mh) (core) You shouldn't apply GridCommands to resultsets.
            // INFO: (mh) (core) You MUST learn to distinct between queries and lists. They are totally different things.
            var tierPricesModel = product.TierPrices
                .AsQueryable()
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
            var tierPrice = new TierPrice
            {
                ProductId = productId,
                StoreId = model.StoreId ?? 0,
                CustomerRoleId = model.CustomerRoleId,
                Quantity = model.Quantity,
                Price = model.Price1 ?? 0,
                CalculationMethod = (TierPriceCalculationMethod)model.CalculationMethodId
            };

            try
            {
                _db.TierPrices.Add(tierPrice);
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
        public async Task<IActionResult> TierPriceUpdate(ProductModel.TierPriceModel model)
        {
            var tierPrice = await _db.TierPrices.FindByIdAsync(model.Id);

            tierPrice.StoreId = model.StoreId ?? 0;
            tierPrice.CustomerRoleId = model.CustomerRoleId;
            tierPrice.Quantity = model.Quantity;
            tierPrice.Price = model.Price1 ?? 0;
            tierPrice.CalculationMethod = (TierPriceCalculationMethod)model.CalculationMethodId;

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
                var toDelete = await _db.TierPrices
                    .Where(x => ids.Contains(x.Id))
                    .ToListAsync();

                numDeleted = toDelete.Count;

                _db.TierPrices.RemoveRange(toDelete);
                await _db.SaveChangesAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
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

        [NonAction]
        protected async Task UpdateDataOfProductDownloadsAsync(ProductModel model)
        {
            var testVersions = (new [] { model.DownloadFileVersion, model.NewVersion }).Where(x => x.HasValue());
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

        #endregion

        #region Product tags

        [Permission(Permissions.Catalog.Product.Read)]
        public IActionResult ProductTags()
        {
            return View();
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductTagsList(GridCommand command)
        {
            var model = new GridModel<ProductTagModel>();

            var tags = await _db.ProductTags
                .AsNoTracking()
                .Include(x => x.Products)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            model.Rows = tags
                .Select(x =>
                {
                    return new ProductTagModel
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Published = x.Published,
                        ProductCount = x.Products.Count
                    };
                });
            
            model.Total = tags.TotalCount;

            return Json(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditTag)]
        public async Task<IActionResult> ProductTagsDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                var toDelete = await _db.ProductTags
                    .Where(x => ids.Contains(x.Id))
                    .ToListAsync();

                numDeleted = toDelete.Count;

                _db.ProductTags.RemoveRange(toDelete);
                await _db.SaveChangesAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditTag)]
        public async Task<IActionResult> ProductTagsUpdate(ProductTagModel model)
        {
            var productTag = await _db.ProductTags.FindByIdAsync(model.Id);

            try
            {
                productTag.Published = model.Published;
                await _db.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                NotifyError(ex.GetInnerMessage());
                return Json(new { success = false });
            }
        }

        [Permission(Permissions.Catalog.Product.EditTag)]
        public async Task<IActionResult> EditProductTag(string btnId, string formId, int id)
        {
            var productTag = await _db.ProductTags
                .Include(x => x.Products)
                .FindByIdAsync(id, false);
                
            if (productTag == null)
            {
                return NotFound();
            }

            var model = new ProductTagModel
            {
                Id = productTag.Id,
                Name = productTag.Name,
                Published = productTag.Published,
                ProductCount = productTag.Products.Count
            };

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Name = productTag.GetLocalized(x => x.Name, languageId, false, false);
            });

            ViewBag.btnId = btnId;
            ViewBag.formId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditTag)]
        public async Task<IActionResult> EditProductTag(string btnId, string formId, ProductTagModel model)
        {
            var productTag = await _db.ProductTags
                .Include(x => x.Products)
                .FindByIdAsync(model.Id);

            if (productTag == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                productTag.Name = model.Name;
                productTag.Published = model.Published;

                await UpdateLocalesAsync(productTag, model);
                await _db.SaveChangesAsync();

                ViewBag.RefreshPage = true;
                ViewBag.btnId = btnId;
                ViewBag.formId = formId;
            }

            return View(model);
        }

        [NonAction]
        private async Task UpdateLocalesAsync(ProductTag productTag, ProductTagModel model)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(productTag, x => x.Name, localized.Name, localized.LanguageId);
            }
        }

        #endregion

        #region Low stock reports

        [Permission(Permissions.Catalog.Product.Read)]
        public IActionResult LowStockReport()
        {
            return View();
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> LowStockReportList(GridCommand command)
        {
            var model = new GridModel<ProductModel>();
            var allProducts = await _productService.GetLowStockProducts()
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            model.Rows = await allProducts.SelectAsync(async x =>
            {
                var productModel = await MapperFactory.MapAsync<Product, ProductModel>(x);
                productModel.ProductTypeName = x.GetProductTypeLabel(Services.Localization);
                productModel.EditUrl = Url.Action("Edit", "Product", new { id = x.Id });
                return productModel;
            }).AsyncToList();

            model.Total = allProducts.TotalCount;

            return Json(model);
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