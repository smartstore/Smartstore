using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Smartstore.Admin.Models.Catalog;
using Smartstore.Collections;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Products.Utilities;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Events;
using Smartstore.Web.Models;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;
using Smartstore.Web.TagHelpers.Shared;

namespace Smartstore.Admin.Controllers
{
    public partial class ProductController : AdminController
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
        private readonly ICurrencyService _currencyService;
        private readonly IDiscountService _discountService;
        private readonly IPriceLabelService _priceLabelService;
        private readonly Lazy<IProductCloner> _productCloner;
        private readonly Lazy<ICategoryService> _categoryService;
        private readonly Lazy<IManufacturerService> _manufacturerService;
        private readonly Lazy<IProductAttributeService> _productAttributeService;
        private readonly Lazy<IProductAttributeMaterializer> _productAttributeMaterializer;
        private readonly Lazy<IStockSubscriptionService> _stockSubscriptionService;
        private readonly Lazy<IShoppingCartService> _shoppingCartService;
        private readonly Lazy<IProductAttributeFormatter> _productAttributeFormatter;
        private readonly Lazy<IDownloadService> _downloadService;
        private readonly Lazy<ICatalogSearchService> _catalogSearchService;
        private readonly Lazy<ProductUrlHelper> _productUrlHelper;
        private readonly Lazy<IRuleProviderFactory> _ruleProviderFactory;
        private readonly AdminAreaSettings _adminAreaSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly MeasureSettings _measureSettings;
        private readonly SeoSettings _seoSettings;
        private readonly MediaSettings _mediaSettings;
        private readonly SearchSettings _searchSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly IEventPublisher _eventPublisher;

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
            ICurrencyService currencyService,
            IDiscountService discountService,
            IPriceLabelService priceLabelService,
            Lazy<IProductCloner> productCloner,
            Lazy<ICategoryService> categoryService,
            Lazy<IManufacturerService> manufacturerService,
            Lazy<IProductAttributeService> productAttributeService,
            Lazy<IProductAttributeMaterializer> productAttributeMaterializer,
            Lazy<IStockSubscriptionService> stockSubscriptionService,
            Lazy<IShoppingCartService> shoppingCartService,
            Lazy<IProductAttributeFormatter> productAttributeFormatter,
            Lazy<IDownloadService> downloadService,
            Lazy<ICatalogSearchService> catalogSearchService,
            Lazy<ProductUrlHelper> productUrlHelper,
            Lazy<IRuleProviderFactory> ruleProviderFactory,
            AdminAreaSettings adminAreaSettings,
            CatalogSettings catalogSettings,
            MeasureSettings measureSettings,
            SeoSettings seoSettings,
            MediaSettings mediaSettings,
            SearchSettings searchSettings,
            ShoppingCartSettings shoppingCartSettings,
            IEventPublisher eventPublisher)
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
            _currencyService = currencyService;
            _discountService = discountService;
            _priceLabelService = priceLabelService;
            _productAttributeService = productAttributeService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _stockSubscriptionService = stockSubscriptionService;
            _shoppingCartService = shoppingCartService;
            _productAttributeFormatter = productAttributeFormatter;
            _downloadService = downloadService;
            _catalogSearchService = catalogSearchService;
            _productUrlHelper = productUrlHelper;
            _ruleProviderFactory = ruleProviderFactory;
            _adminAreaSettings = adminAreaSettings;
            _catalogSettings = catalogSettings;
            _measureSettings = measureSettings;
            _seoSettings = seoSettings;
            _mediaSettings = mediaSettings;
            _searchSettings = searchSettings;
            _shoppingCartSettings = shoppingCartSettings;
            _eventPublisher = eventPublisher;
        }

        #region Product list / create / edit / delete

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> List(ProductListModel model)
        {
            await PrepareProductListModelAsync(model);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products.FindByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.DeleteProduct, T("ActivityLog.DeleteProduct"), product.Name);
            NotifySuccess(T("Admin.Catalog.Products.Deleted"));

            return RedirectToAction(nameof(List));
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
        public async Task<IActionResult> Create(ProductModel model, bool continueEditing, IFormCollection form)
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

                await MapModelToProductAsync(model, product, form);

                product.StockQuantity = 10000;
                product.OrderMinimumQuantity = 1;
                product.OrderMaximumQuantity = 50;
                product.QuantityStep = 1;
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

                await UpdateDataOfExistingProductAsync(product, model, false);

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
                return continueEditing ? RedirectToAction(nameof(Edit), new { id = product.Id }) : RedirectToAction(nameof(List));
            }

            // If we got this far something failed. Redisplay form.
            await PrepareProductModelAsync(model, null, false, true);

            return View(model);
        }

        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _db.Products
                .AsSplitQuery()
                .Include(x => x.ProductTags)
                .Include(x => x.AppliedDiscounts)
                .FindByIdAsync(id);

            if (product == null)
            {
                NotifyWarning(T("Products.NotFound", id));
                return RedirectToAction(nameof(List));
            }

            if (product.Deleted)
            {
                NotifyWarning(T("Products.Deleted", id));
                return RedirectToAction(nameof(List));
            }

            var model = await MapperFactory.MapAsync<Product, ProductModel>(product);
            await PrepareProductModelAsync(model, product, false, false);

            await AddLocalesAsync(model.Locales, async (locale, languageId) =>
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
            var product = await _db.Products
                .AsSplitQuery()
                .Include(x => x.AppliedDiscounts)
                .Include(x => x.ProductTags)
                .FindByIdAsync(model.Id);

            if (product == null)
            {
                NotifyWarning(T("Products.NotFound", model.Id));
                return RedirectToAction(nameof(List));
            }

            if (product.Deleted)
            {
                NotifyWarning(T("Products.Deleted", model.Id));
                return RedirectToAction(nameof(List));
            }

            await UpdateDataOfProductDownloadsAsync(model);

            if (ModelState.IsValid)
            {
                await MapModelToProductAsync(model, product, form);
                await UpdateDataOfExistingProductAsync(product, model, true);

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditProduct, T("ActivityLog.EditProduct"), product.Name);

                NotifySuccess(T("Admin.Catalog.Products.Updated"));
                return continueEditing ? RedirectToAction(nameof(Edit), new { id = product.Id }) : RedirectToAction(nameof(List));
            }

            // If we got this far something failed. Redisplay form.
            await PrepareProductModelAsync(model, product, false, true);

            return View(model);
        }

        #endregion

        #region Misc 

        /// <summary>
        /// (AJAX) Gets a list of all products.
        /// </summary>
        /// <param name="page">Zero based page index.</param>
        /// <param name="term">Optional search term.</param>
        /// <param name="selectedIds">Selected product identifiers.</param>
        public async Task<IActionResult> AllProducts(int page, string term, string selectedIds, string disabledIds)
        {
            const int pageSize = 100;

            var hasMoreData = false;
            var idsSelected = selectedIds.ToIntArray();
            var idsDisabled = disabledIds.ToIntArray();
            IList<Product> products = null;

            if (term.HasValue())
            {
                // Perform a search by SKU, MPN or GTIN first.
                var (product, _) = await _productService.GetProductByCodeAsync(term, true);
                if (product != null)
                {
                    products = new List<Product> { product };
                }
            }

            if (products.IsNullOrEmpty())
            {
                // If no products were found by unique identifiers, perform a full text search.
                var skip = page * pageSize;
                var fields = new List<string> { "name" };

                if (_searchSettings.SearchFields.Contains("sku"))
                {
                    fields.Add("sku");
                }
                if (_searchSettings.SearchFields.Contains("shortdescription"))
                {
                    fields.Add("shortdescription");
                }

                var searchQuery = new CatalogSearchQuery(fields.ToArray(), term);

                if (_searchSettings.UseCatalogSearchInBackend)
                {
                    searchQuery = searchQuery
                        .Slice(skip, pageSize)
                        .SortBy(ProductSortingEnum.NameAsc);

                    var searchResult = await _catalogSearchService.Value.SearchAsync(searchQuery);
                    var hits = await searchResult.GetHitsAsync();

                    hasMoreData = hits.HasNextPage;
                    products = hits;
                }
                else
                {
                    var query = _catalogSearchService.Value.PrepareQuery(searchQuery);
                    var count = await query.CountAsync();

                    hasMoreData = (page + 1) * pageSize < count;

                    products = await query
                        .Select(x => new Product
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Sku = x.Sku
                        })
                        .OrderBy(x => x.Name)
                        .Skip(skip)
                        .Take(pageSize)
                        .ToListAsync();
                }
            }

            var items = products.Select(x => new ChoiceListItem
            {
                Id = x.Id.ToString(),
                Text = x.Name,
                Hint = x.Sku,
                Selected = idsSelected.Contains(x.Id),
                Disabled = idsDisabled.Contains(x.Id)
            })
            .ToList();

            return new JsonResult(new
            {
                hasMoreData,
                results = items
            });
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

                var product = await _db.Products
                    .AsSplitQuery()
                    .Include(x => x.AppliedDiscounts)
                    .Include(x => x.ProductTags)
                    .FindByIdAsync(id, false);

                var model = await MapperFactory.MapAsync<Product, ProductModel>(product);

                await PrepareProductModelAsync(model, product, false, false);

                await AddLocalesAsync(model.Locales, async (locale, languageId) =>
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

        [HttpPost, ActionName("List")]
        [FormValueRequired("go-to-product")]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> GoToProduct(ProductListModel model)
        {
            var productCode = model.ProductCode;

            if (productCode.HasValue())
            {
                var products = await _db.Products
                    .IgnoreQueryFilters()
                    .ApplyProductCodeFilter(productCode)
                    .Select(x => new { x.Id, x.Deleted })
                    .ToListAsync();

                if (products.Count > 0)
                {
                    var notDeleted = products.FirstOrDefault(x => !x.Deleted);
                    if (notDeleted != null)
                    {
                        return RedirectToAction(nameof(Edit), new { id = notDeleted.Id });
                    }
                    else
                    {
                        NotifyWarning(T("Products.Deleted", products[0].Id));
                    }
                }
                else
                {
                    var query =
                        from ac in _db.ProductVariantAttributeCombinations.ApplyProductCodeFilter(productCode)
                        join p in _db.Products.AsNoTracking().IgnoreQueryFilters() on ac.ProductId equals p.Id into acp
                        from p in acp.DefaultIfEmpty()
                        select new { Combination = ac, Product = p };

                    var pvac = await query.FirstOrDefaultAsync();

                    if (pvac?.Combination != null)
                    {
                        if (pvac.Product == null)
                        {
                            NotifyWarning(T("Products.NotFound", pvac.Combination.ProductId));
                        }
                        else if (pvac.Product.Deleted)
                        {
                            NotifyWarning(T("Products.Deleted", pvac.Combination.ProductId));
                        }
                        else
                        {
                            return RedirectToAction(nameof(Edit), new { id = pvac.Combination.ProductId });
                        }
                    }
                }
            }

            // Not found.
            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Create)]
        public async Task<IActionResult> CopyProduct(ProductModel model)
        {
            var copyModel = model.CopyProductModel;

            try
            {
                Product clone = null;
                // Lets just load this untracked as nearly all navigation properties are needed in order to copy successfully.
                // We just eager load the most common properties.
                var product = await _db.Products
                    .AsSplitQuery()
                    .Include(x => x.ProductCategories)
                    .Include(x => x.ProductManufacturers)
                    .Include(x => x.ProductSpecificationAttributes)
                    .Include(x => x.ProductVariantAttributes)
                    .Include(x => x.ProductVariantAttributeCombinations)
                    .FindByIdAsync(copyModel.Id);

                var name = copyModel.Name.NullEmpty() ?? T("Admin.Common.CopyOf", product.Name);
                var numCopies = Math.Min(100, copyModel.NumberOfCopies);

                for (var i = 1; i <= numCopies; ++i)
                {
                    var newName = numCopies > 1 ? $"{name} {i}" : name;
                    clone = await _productCloner.Value.CloneProductAsync(product, newName, copyModel.Published);

                    await _eventPublisher.PublishAsync(new ProductClonedEvent(product, clone));
                }

                if (clone != null)
                {
                    NotifySuccess(T("Admin.Common.TaskSuccessfullyProcessed"));
                    return RedirectToAction(nameof(Edit), new { id = clone.Id });
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                NotifyError(ex.ToAllMessages());
            }

            return RedirectToAction(nameof(Edit), new { id = copyModel.Id });
        }

        [HttpPost]
        public async Task<IActionResult> GetBasePrice(int productId, string basePriceMeasureUnit, decimal basePriceAmount, int basePriceBaseAmount)
        {
            var product = await _db.Products.FindByIdAsync(productId);
            string basePrice = string.Empty;

            if (basePriceAmount != decimal.Zero)
            {
                var basePriceValue = Convert.ToDecimal(product.Price / basePriceAmount * basePriceBaseAmount);
                var basePriceFormatted = _currencyService.ConvertFromPrimaryCurrency(basePriceValue, _workContext.WorkingCurrency).ToString();
                var unit = $"{basePriceBaseAmount} {basePriceMeasureUnit}";

                basePrice = T("Admin.Catalog.Products.Fields.BasePriceInfo", $"{basePriceAmount:G29} {basePriceMeasureUnit}", basePriceFormatted, unit);
            }

            return Json(new { Result = true, BasePrice = basePrice });
        }

        #endregion

        #region Product categories

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductCategoryList(int productId)
        {
            var productCategories = await _categoryService.Value.GetProductCategoriesByProductIdsAsync(new[] { productId }, true);

            var rows = await productCategories
                .SelectAwait(async x =>
                {
                    var node = await _categoryService.Value.GetCategoryTreeAsync(x.CategoryId, true);
                    var categoryPath = node != null ? _categoryService.Value.GetCategoryPath(node, null, "<span class='badge badge-secondary'>{0}</span>") : string.Empty;

                    return new ProductModel.ProductCategoryModel
                    {
                        Id = x.Id,
                        Category = categoryPath,
                        ProductId = x.ProductId,
                        CategoryId = x.CategoryId,
                        IsFeaturedProduct = x.IsFeaturedProduct,
                        DisplayOrder = x.DisplayOrder,
                        IsSystemMapping = x.IsSystemMapping,
                        EditUrl = Url.Action("Edit", "Category", new { id = x.CategoryId })
                    };
                })
                .AsyncToList();

            return Json(new GridModel<ProductModel.ProductCategoryModel>
            {
                Rows = rows,
                Total = rows.Count,
            });
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
                    model.CategoryId.ToString(),
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
            var categoryChanged = model.CategoryId != productCategory.CategoryId;

            if (categoryChanged)
            {
                var alreadyAssigned = await _db.ProductCategories.AnyAsync(x => x.CategoryId == model.CategoryId && x.ProductId == model.ProductId);

                if (alreadyAssigned)
                {
                    NotifyError(T("Admin.Catalog.Products.Categories.NoDuplicatesAllowed"));
                    return Json(new { success = false });
                }
            }

            productCategory.CategoryId = model.CategoryId;
            productCategory.IsFeaturedProduct = model.IsFeaturedProduct;
            productCategory.DisplayOrder = model.DisplayOrder;

            try
            {
                if (categoryChanged)
                {
                    var mru = new TrimmedBuffer<string>(
                        _workContext.CurrentCustomer.GenericAttributes.MostRecentlyUsedCategories,
                        model.CategoryId.ToString(),
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
                var toDelete = await _db.ProductCategories.GetManyAsync(ids);
                _db.ProductCategories.RemoveRange(toDelete);
                numDeleted = await _db.SaveChangesAsync();
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

            var rows = productManufacturers
                .Select(x => new ProductModel.ProductManufacturerModel
                {
                    Id = x.Id,
                    Manufacturer = x.Manufacturer.Name,
                    ProductId = x.ProductId,
                    ManufacturerId = x.ManufacturerId,
                    IsFeaturedProduct = x.IsFeaturedProduct,
                    DisplayOrder = x.DisplayOrder,
                    EditUrl = Url.Action("Edit", "Manufacturer", new { id = x.ManufacturerId })
                })
                .ToList();

            return Json(new GridModel<ProductModel.ProductManufacturerModel>
            {
                Rows = rows,
                Total = rows.Count
            });
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
                    model.ManufacturerId.ToString(),
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
            var manufacturerChanged = model.ManufacturerId != productManufacturer.ManufacturerId;

            if (manufacturerChanged)
            {
                var alreadyAssigned = await _db.ProductManufacturers.AnyAsync(x => x.ManufacturerId == model.ManufacturerId && x.ProductId == model.ProductId);

                if (alreadyAssigned)
                {
                    NotifyError(T("Admin.Catalog.Products.Manufacturers.NoDuplicatesAllowed"));
                    return Json(new { success = false });
                }
            }

            productManufacturer.ManufacturerId = model.ManufacturerId;
            productManufacturer.IsFeaturedProduct = model.IsFeaturedProduct;
            productManufacturer.DisplayOrder = model.DisplayOrder;

            try
            {
                if (manufacturerChanged)
                {
                    var mru = new TrimmedBuffer<string>(
                        _workContext.CurrentCustomer.GenericAttributes.MostRecentlyUsedManufacturers,
                        model.ManufacturerId.ToString(),
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
                var toDelete = await _db.ProductManufacturers.GetManyAsync(ids);
                _db.ProductManufacturers.RemoveRange(toDelete);
                numDeleted = await _db.SaveChangesAsync();
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

                var pictureIds = new HashSet<int>(pictures.ToIntArray());
                var displayOrder = 1;

                foreach (var id in pictureIds)
                {
                    var productPicture = files.Where(x => x.Id == id).FirstOrDefault();
                    if (productPicture != null)
                    {
                        // Same value for display order as in MediaImporter.
                        productPicture.DisplayOrder = displayOrder;

                        // Add all relevant data of product picture to response.
                        dynamic file = new
                        {
                            productPicture.DisplayOrder,
                            productPicture.MediaFileId,
                            EntityMediaId = productPicture.Id
                        };

                        response.Add(file);

                        if (files.Count == 1 && productPicture.Product.MainPictureId == null)
                        {
                            // Fix missing MainPictureId here because ProductMediaFileHook not executed in this case.
                            productPicture.Product.MainPictureId = productPicture.MediaFileId;
                        }
                    }
                    ++displayOrder;
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

            if (ids.Length == 0)
            {
                throw new ArgumentException("Missing picture identifiers.");
            }

            var success = false;
            var product = await _db.Products
                .Include(x => x.ProductMediaFiles)
                .FindByIdAsync(entityId);

            if (product == null)
            {
                throw new ArgumentException(T("Products.NotFound", entityId));
            }

            var response = new List<dynamic>();
            var existingFileIds = product.ProductMediaFiles.Select(x => x.MediaFileId).ToList();
            var displayOrder = product.ProductMediaFiles.Count > 0 ? product.ProductMediaFiles.Max(x => x.DisplayOrder) : 0;
            var files = (await _mediaService.GetFilesByIdsAsync(ids, MediaLoadFlags.AsNoTracking)).ToDictionary(x => x.Id);

            foreach (var id in ids)
            {
                var exists = existingFileIds.Contains(id);

                // No duplicate assignments!
                if (!exists)
                {
                    var productPicture = new ProductMediaFile
                    {
                        ProductId = entityId,
                        MediaFileId = id,
                        DisplayOrder = ++displayOrder
                    };

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
            var tierPrices = await _db.TierPrices
                .Where(x => x.ProductId == productId)
                .OrderBy(x => x.Quantity)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

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

            var tierPricesModel = tierPrices
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
                        Price1 = x.Price,
                        CalculationMethod = x.CalculationMethod switch
                        {
                            TierPriceCalculationMethod.Fixed => T("Admin.Product.Price.Tierprices.Fixed").Value,
                            TierPriceCalculationMethod.Adjustment => T("Admin.Product.Price.Tierprices.Adjustment").Value,
                            TierPriceCalculationMethod.Percental => T("Admin.Product.Price.Tierprices.Percental").Value,
                            _ => x.CalculationMethod.ToString(),
                        }
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

            var model = new GridModel<ProductModel.TierPriceModel>
            {
                Rows = tierPricesModel,
                Total = tierPrices.TotalCount
            };

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
                var toDelete = await _db.TierPrices.GetManyAsync(ids);
                _db.TierPrices.RemoveRange(toDelete);
                numDeleted = await _db.SaveChangesAsync();
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

            return Json(new { success = true, Message = T("Admin.Common.TaskSuccessfullyProcessed").Value });
        }

        private async Task UpdateDataOfProductDownloadsAsync(ProductModel model)
        {
            var testVersions = (new[] { model.DownloadFileVersion, model.NewVersion }).Where(x => x.HasValue());
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

            if (model.SampleDownloadId != model.OldSampleDownloadId && model.SampleDownloadId != 0 && !isUrlDownload)
            {
                // Insert sample download if a new file was uploaded.
                model.SampleDownloadId = await InsertSampleDownloadAsync(model.SampleDownloadId, model.Id);
            }
            else if (isUrlDownload)
            {
                var download = await _db.Downloads.FindByIdAsync((int)model.SampleDownloadId);
                download.IsTransient = false;
                await _db.SaveChangesAsync();
            }
        }

        private async Task InsertProductDownloadAsync(int? fileId, int entityId, string fileVersion = "")
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

        private async Task<int?> InsertSampleDownloadAsync(int? fileId, int entityId)
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

        /// <summary>
        /// (AJAX) Gets a paged list of product tags.
        /// </summary>
        /// <param name="selectedNames">Names of selected tags.</param>
        public async Task<IActionResult> AllProductTags(string term, string selectedNames, int page = 1)
        {
            const int pageSize = 500;
            var skip = page * pageSize;

            var query = _db.ProductTags.AsNoTracking();

            if (term.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Name, term);
            }

            var tags = await query
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToPagedList(page - 1, pageSize)
                .LoadAsync();

            var results = tags.Select(x => new ChoiceListItem
            {
                Id = x,
                Text = x,
                Selected = selectedNames?.Contains(x) ?? false
            })
                .ToList();

            return new JsonResult(new
            {
                results,
                pagination = new { more = tags.HasNextPage }
            });
        }

        [Permission(Permissions.Catalog.Product.Read)]
        public IActionResult ProductTags()
        {
            return View(new ProductTagListModel());
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> ProductTagsList(GridCommand command, ProductTagListModel model)
        {
            var query = _db.ProductTags.AsNoTracking();

            if (model.SearchName.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Name, model.SearchName);
            }
            if (model.SearchPublished.HasValue)
            {
                query = query.Where(x => x.Published == model.SearchPublished.Value);
            }

            var tags = await query
                .OrderBy(x => x.Name)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var rows = await tags
                .SelectAwait(async x => new ProductTagModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    Published = x.Published,
                    ProductCount = await _productTagService.CountProductsByTagIdAsync(x.Id)
                })
                .AsyncToList();

            return Json(new GridModel<ProductTagModel>
            {
                Rows = rows,
                Total = await tags.GetTotalCountAsync()
            });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditTag)]
        public async Task<IActionResult> ProductTagsDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                var toDelete = await _db.ProductTags.GetManyAsync(ids);
                _db.ProductTags.RemoveRange(toDelete);
                numDeleted = await _db.SaveChangesAsync();
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

                foreach (var localized in model.Locales)
                {
                    await _localizedEntityService.ApplyLocalizedValueAsync(productTag, x => x.Name, localized.Name, localized.LanguageId);
                }

                await _db.SaveChangesAsync();

                ViewBag.RefreshPage = true;
                ViewBag.btnId = btnId;
                ViewBag.formId = formId;
            }

            return View(model);
        }

        #endregion

        #region Low stock reports

        [Permission(Permissions.Catalog.Product.Read)]
        public IActionResult LowStockReport()
        {
            return View();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Permission(Permissions.Catalog.Product.Read)]
        public async Task<IActionResult> LowStockReportList(GridCommand command)
        {
            var productIdQuery =
                from p in _db.Products
                join ac in _db.ProductVariantAttributeCombinations on p.Id equals ac.ProductId into pac
                from ac in pac.DefaultIfEmpty()
                where (p.ManageInventoryMethodId == (int)ManageInventoryMethod.ManageStock && p.StockQuantity <= p.MinStockQuantity) ||
                    (p.ManageInventoryMethodId == (int)ManageInventoryMethod.ManageStockByAttributes && ac.StockQuantity <= 0)
                select p.Id;

            var distinctQuery = productIdQuery
                .Distinct()
                .SelectMany(key => _db.Products
                    .AsNoTracking()
                    .Where(x => x.Id == key));

            var products = await distinctQuery
                .OrderBy(x => x.StockQuantity)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var rows = await products.MapAsync(Services.MediaService);

            return Json(new GridModel<ProductOverviewModel>
            {
                Rows = rows,
                Total = await products.GetTotalCountAsync()
            });
        }

        #endregion

        #region Hidden normalizers

        [Permission(Permissions.Catalog.Product.Update)]
        public async Task<IActionResult> FixProductMainPictureIds(DateTime? ifModifiedSinceUtc = null)
        {
            var count = await ProductPictureHelper.FixProductMainPictureIds(_db, ifModifiedSinceUtc);

            return Content("Fixed {0} ids.".FormatInvariant(count));
        }

        #endregion

        #region Utilities

        private async Task PrepareProductListModelAsync(ProductListModel model)
        {
            model.DisplayProductPictures = _adminAreaSettings.DisplayProductPictures;
            model.IsSingleStoreMode = Services.StoreContext.IsSingleStoreMode();

            foreach (var c in (await _categoryService.Value.GetCategoryTreeAsync(includeHidden: true)).FlattenNodes(false))
            {
                model.AvailableCategories.Add(new() { Text = c.GetCategoryNameIndented(), Value = c.Id.ToString() });
            }

            foreach (var m in await _db.Manufacturers.AsNoTracking().ApplyStandardFilter(true).Select(x => new { x.Name, x.Id }).ToListAsync())
            {
                model.AvailableManufacturers.Add(new() { Text = m.Name, Value = m.Id.ToString() });
            }

            model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList(false).ToList();

            var deliveryTimes = await _db.DeliveryTimes
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            ViewBag.DeliveryTimes = deliveryTimes
                .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
                .ToList();
        }

        private async Task PrepareProductModelAsync(ProductModel model, Product product, bool setPredefinedValues, bool excludeProperties)
        {
            Guard.NotNull(model);

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
                model.ProductUrl = await GetEntityPublicUrlAsync(product);

                model.NumberOfOrders = await _db.Orders
                    .Where(x => x.OrderItems.Any(oi => oi.ProductId == product.Id))
                    .Select(x => x.Id)
                    .Distinct()
                    .CountAsync();

                var maxDisplayOrder = (await _db.ProductSpecificationAttributes
                    .Where(x => x.ProductId == product.Id)
                    .MaxAsync(x => (int?)x.DisplayOrder)) ?? 0;

                model.AddSpecificationAttributeModel.DisplayOrder = ++maxDisplayOrder;

                // Downloads.
                var productDownloads = await _db.Downloads
                    .AsNoTracking()
                    .Include(x => x.MediaFile)
                    .ApplyEntityFilter(product)
                    .ApplyVersionFilter(string.Empty)
                    .ToListAsync();

                var idsOrderedByVersion = productDownloads
                    .Select(x => new { x.Id, Version = SemanticVersion.Parse(x.FileVersion.HasValue() ? x.FileVersion : "1.0.0") })
                    .OrderByDescending(x => x.Version)
                    .Select(x => x.Id);

                productDownloads = productDownloads.OrderBySequence(idsOrderedByVersion).ToList();

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
                if (currentDownload?.MediaFile != null)
                {
                    model.DownloadThumbUrl = await _mediaService.GetUrlAsync(currentDownload.MediaFile.Id, _mediaSettings.CartThumbPictureSize, null, true);
                    currentDownload.DownloadUrl = Url.Action("DownloadFile", "Download", new { downloadId = currentDownload.Id });
                    model.CurrentFile = await _mediaService.GetFileByIdAsync(currentDownload.MediaFile.Id);
                }

                model.DownloadFileVersion = (currentDownload?.FileVersion).EmptyNull();
                model.OldSampleDownloadId = model.SampleDownloadId;

                // Media files.
                var file = await _mediaService.GetFileByIdAsync(product.MainPictureId ?? 0);
                model.PictureThumbnailUrl = _mediaService.GetUrl(file, _mediaSettings.CartThumbPictureSize);
                model.NoThumb = file == null;

                await PrepareProductFileModelAsync(model);
                model.AddPictureModel.PictureId = product.MainPictureId ?? 0;

                model.ProductTagNames = product.ProductTags.Select(x => x.Name).ToArray();
                
                // Instance always required because of validation.
                model.GroupedProductConfiguration ??= new();
                if (product.ProductType == ProductType.GroupedProduct && product.GroupedProductConfiguration != null)
                {
                    MiniMapper.Map(product.GroupedProductConfiguration, model.GroupedProductConfiguration);
                }

                ViewBag.SelectedProductTags = model.ProductTagNames
                    .Select(x => new SelectListItem { Value = x, Text = x, Selected = true })
                    .ToList();

                ViewBag.ProductTagsUrl = Url.Action(nameof(AllProductTags), new { selectedNames = string.Join(',', model.ProductTagNames.Select(x => x)) });
            }
            else
            {
                ViewBag.SelectedProductTags = new List<SelectListItem>();
                ViewBag.ProductTagsUrl = Url.Action(nameof(AllProductTags));
            }

            var measure = await _db.MeasureWeights.FindByIdAsync(_measureSettings.BaseWeightId, false);
            var dimension = await _db.MeasureDimensions.FindByIdAsync(_measureSettings.BaseDimensionId, false);

            model.BaseWeightIn = measure?.GetLocalized(x => x.Name) ?? string.Empty;
            model.BaseDimensionIn = dimension?.GetLocalized(x => x.Name) ?? string.Empty;

            model.NumberOfAvailableProductAttributes = await _db.ProductAttributes.CountAsync();
            model.NumberOfAvailableManufacturers = await _db.Manufacturers.CountAsync();
            model.NumberOfAvailableCategories = await _db.Categories.CountAsync();
            model.PrimaryStoreCurrencyCode = _currencyService.PrimaryCurrency.CurrencyCode;

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

            ViewBag.AvailableProductTemplates = templates
                .Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                })
                .ToList();

            // Tax categories.
            var taxCategories = await _db.TaxCategories
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            ViewBag.AvailableTaxCategories = taxCategories
                .Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString(),
                    Selected = product != null && !setPredefinedValues && x.Id == product.TaxCategoryId
                })
                .ToList();

            // Do not pre-select a tax category that is not stored.
            if (product != null && product.TaxCategoryId == 0)
            {
                ViewBag.AvailableTaxCategories.Insert(0, new SelectListItem { Text = T("Common.PleaseSelect"), Value = string.Empty, Selected = true });
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

            ViewBag.AvailableQuantityUnits = quantityUnits
                .Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString(),
                    Selected = product != null && !setPredefinedValues && x.Id == product.QuantityUnitId.GetValueOrDefault()
                })
                .ToList();

            // BasePrice aka PAnGV
            var measureUnitKeys = await _db.MeasureWeights.AsNoTracking().OrderBy(x => x.DisplayOrder).Select(x => x.SystemKeyword).ToListAsync();
            var measureDimensionKeys = await _db.MeasureDimensions.AsNoTracking().OrderBy(x => x.DisplayOrder).Select(x => x.SystemKeyword).ToListAsync();
            var measureUnits = new HashSet<string>(measureUnitKeys.Concat(measureDimensionKeys), StringComparer.OrdinalIgnoreCase);

            if (product != null && !setPredefinedValues && product.BasePriceMeasureUnit.HasValue())
            {
                measureUnits.Add(product.BasePriceMeasureUnit);
            }

            ViewBag.AvailableMeasureUnits = measureUnits
                .Select(x => new SelectListItem
                {
                    Text = x,
                    Value = x,
                    Selected = product != null && !setPredefinedValues && x.EqualsNoCase(product.BasePriceMeasureUnit)
                })
                .ToList();

            ViewBag.SpecificationAttributesCount = await _db.SpecificationAttributes.CountAsync();

            if (product != null && !excludeProperties)
            {
                model.SelectedDiscountIds = product.AppliedDiscounts.Select(d => d.Id).ToArray();
            }

            var inventoryMethods = ((ManageInventoryMethod[])Enum.GetValues(typeof(ManageInventoryMethod))).Where(
                x => model.ProductTypeId != (int)ProductType.BundledProduct || x != ManageInventoryMethod.ManageStockByAttributes
            );

            ViewBag.AvailableManageInventoryMethods = inventoryMethods
                .Select(x => new SelectListItem
                {
                    Value = ((int)x).ToString(),
                    Text = x.GetLocalizedEnum(),
                    Selected = (int)x == model.ManageInventoryMethodId
                })
                .ToList();

            var priceLabels = await _db.PriceLabels
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            ViewBag.PriceLabels = priceLabels
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.GetLocalized(x => x.ShortName),
                    Selected = !setPredefinedValues && product != null && x.Id == product.ComparePriceLabelId.GetValueOrDefault()
                })
                .ToList();

            ViewBag.DefaultComparePriceLabelName =
                _priceLabelService.GetDefaultComparePriceLabel()?.GetLocalized(x => x.ShortName)?.Value ??
                T("Common.Unspecified").Value;

            ViewBag.CartQuantityInfo = T("Admin.Catalog.Products.CartQuantity.Info",
                _shoppingCartSettings.MaxQuantityInputDropdownItems.ToString("N0"),
                Url.Action("ShoppingCart", "Setting"));

            var headerFields = model.GroupedProductConfiguration?.HeaderFields ?? [];
            ViewBag.AssociatedProductsHeaderFields = new List<SelectListItem>
            {
                new() { Value = AssociatedProductHeader.Image, Text = T("Common.Image"), Selected = headerFields.Contains(AssociatedProductHeader.Image) },
                new() { Value = AssociatedProductHeader.Sku, Text = T("Admin.Catalog.Products.Fields.Sku"), Selected = headerFields.Contains(AssociatedProductHeader.Sku) },
                new() { Value = AssociatedProductHeader.Price, Text = T("Admin.Catalog.Products.Fields.Price"), Selected = headerFields.Contains(AssociatedProductHeader.Price) },
                new() { Value = AssociatedProductHeader.Weight, Text = T("Admin.Catalog.Products.Fields.Weight"), Selected = headerFields.Contains(AssociatedProductHeader.Weight) },
                new() { Value = AssociatedProductHeader.Dimensions, Text = T("Admin.Configuration.Measures.Dimensions"), Selected = headerFields.Contains(AssociatedProductHeader.Dimensions) }
            };

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
                model.OrderMaximumQuantity = 50;
                model.QuantityStep = 1;
                model.HideQuantityControl = false;
                model.UnlimitedDownloads = true;
                model.IsShippingEnabled = true;
                model.AllowCustomerReviews = true;
                model.Published = true;
                model.HasPreviewPicture = false;
            }
        }

        private async Task PrepareProductFileModelAsync(ProductModel model)
        {
            Guard.NotNull(model);

            var productFiles = await _db.ProductMediaFiles
                .AsNoTracking()
                .Include(x => x.MediaFile)
                .ApplyProductFilter(model.Id)
                .ToListAsync();

            model.ProductMediaFiles = productFiles
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
                ViewBag.Title = T("Admin.Catalog.Products.BundleItems.EditOf").Value;
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
                .Include(x => x.ProductAttribute)
                .ApplyProductFilter(new[] { bundleItem.ProductId })
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            foreach (var attribute in attributes)
            {
                var attributeModel = new ProductBundleItemAttributeModel
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

                    attributeModel.Values.Add(new SelectListItem
                    {
                        Text = attributeValue.Name,
                        Value = attributeValue.Id.ToString(),
                        Selected = (filteredValue != null)
                    });

                    if (filteredValue != null)
                    {
                        attributeModel.PreSelect.Add(new SelectListItem
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
                        attributeModel.PreSelect.Insert(0, new SelectListItem { Text = T("Admin.Common.PleaseSelect") });
                    }

                    model.Attributes.Add(attributeModel);
                }
            }
        }

        private async Task SaveFilteredAttributesAsync(ProductBundleItem bundleItem)
        {
            var form = Request.Form;

            var toDelete = await _db.ProductBundleItemAttributeFilter
                .Where(x => x.BundleItemId == bundleItem.Id)
                .ToListAsync();

            _db.ProductBundleItemAttributeFilter.RemoveRange(toDelete);
            await _db.SaveChangesAsync();

            var allFilterKeys = form.Keys.Where(x => x.HasValue() && x.StartsWith(ProductBundleItemAttributeModel.AttributeControlPrefix));

            foreach (var key in allFilterKeys)
            {
                int attributeId = key[ProductBundleItemAttributeModel.AttributeControlPrefix.Length..].ToInt();
                string preSelectId = form[ProductBundleItemAttributeModel.PreSelectControlPrefix + attributeId.ToString()].ToString().EmptyNull();

                foreach (var valueId in form[key].ToString().SplitSafe(','))
                {
                    var attributeFilter = new ProductBundleItemAttributeFilter
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

        #endregion

        #region Update[...]

        private async Task MapModelToProductAsync(ProductModel model, Product product, IFormCollection form)
        {
            if (model.LoadedTabs == null || model.LoadedTabs.Length == 0)
            {
                model.LoadedTabs = new string[] { "Info" };
            }

            foreach (var tab in model.LoadedTabs)
            {
                switch (tab.ToLowerInvariant())
                {
                    case "info":
                        UpdateProductGeneralInfo(product, model);
                        break;
                    case "inventory":
                        UpdateProductInventory(product, model);
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

            await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, product, form));
        }

        private void UpdateProductGeneralInfo(Product product, ProductModel model)
        {
            var p = product;
            var m = model;

            p.ProductTypeId = m.ProductTypeId;
            p.Visibility = m.Visibility;
            p.Condition = m.Condition;
            p.ProductTemplateId = m.ProductTemplateId;
            p.Name = m.Name;
            p.ShortDescription = m.ShortDescription;
            p.FullDescription = m.FullDescription;
            p.Sku = m.Sku;
            p.ManufacturerPartNumber = m.ManufacturerPartNumber;
            p.Gtin = m.Gtin;
            p.AdminComment = m.AdminComment;

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

            p.AvailableStartDateTimeUtc = m.AvailableStartDateTimeUtc.HasValue
                ? Services.DateTimeHelper.ConvertToUtcTime(m.AvailableStartDateTimeUtc.Value)
                : null;

            p.AvailableEndDateTimeUtc = m.AvailableEndDateTimeUtc.HasValue
                ? Services.DateTimeHelper.ConvertToUtcTime(m.AvailableEndDateTimeUtc.Value)
                : null;

            if (p.ProductType == ProductType.GroupedProduct)
            {
                var config = MiniMapper.Map<GroupedProductConfigurationModel, GroupedProductConfiguration>(model.GroupedProductConfiguration);
                p.GroupedProductConfiguration = config;
            }
        }

        private async Task UpdateProductDownloadsAsync(Product product, ProductModel model)
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

        private void UpdateProductInventory(Product product, ProductModel model)
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
            p.AllowedQuantities = m.AllowedQuantities;
        }

        private async Task UpdateProductBundleItemsAsync(Product product, ProductModel model)
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

        private async Task UpdateProductPriceAsync(Product product, ProductModel model)
        {
            var p = product;
            var m = model;

            p.Price = m.Price;
            p.ComparePrice = m.ComparePrice ?? 0;
            p.ComparePriceLabelId = m.ComparePriceLabelId == 0 ? null : m.ComparePriceLabelId;
            p.ProductCost = m.ProductCost ?? 0;
            p.SpecialPrice = m.SpecialPrice;

            p.SpecialPriceStartDateTimeUtc = m.SpecialPriceStartDateTimeUtc.HasValue
                ? Services.DateTimeHelper.ConvertToUtcTime(m.SpecialPriceStartDateTimeUtc.Value)
                : null;

            p.SpecialPriceEndDateTimeUtc = m.SpecialPriceEndDateTimeUtc.HasValue
                ? Services.DateTimeHelper.ConvertToUtcTime(m.SpecialPriceEndDateTimeUtc.Value)
                : null;

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
            await _discountService.ApplyDiscountsAsync(product, model.SelectedDiscountIds, DiscountType.AssignedToSkus);
        }

        private static void UpdateProductAttributes(Product product, ProductModel model)
        {
            product.AttributeCombinationRequired = model.AttributeCombinationRequired;
            product.AttributeChoiceBehaviour = model.AttributeChoiceBehaviour;
        }

        private async Task UpdateProductSeoAsync(Product product, ProductModel model)
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

        private static void UpdateProductPictures(Product product, ProductModel model)
        {
            product.HasPreviewPicture = model.HasPreviewPicture;
        }

        private async Task UpdateDataOfExistingProductAsync(Product product, ProductModel model, bool editMode)
        {
            var p = product;
            var m = model;

            //var seoTabLoaded = m.LoadedTabs.Contains("SEO", StringComparer.OrdinalIgnoreCase);

            // SEO.
            var slugResult = await _urlService.SaveSlugAsync(p, m.SeName, p.GetDisplayName(), true);
            m.SeName = slugResult.Slug;

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

                await _urlService.SaveSlugAsync(p, localized.SeName, localized.Name, false, localized.LanguageId);
            }

            await _storeMappingService.ApplyStoreMappingsAsync(p, model.SelectedStoreIds);
            await _aclService.ApplyAclMappingsAsync(p, model.SelectedCustomerRoleIds);

            await _db.SaveChangesAsync();

            await _productTagService.UpdateProductTagsAsync(p, m.ProductTagNames);
        }

        #endregion
    }
}