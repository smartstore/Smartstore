using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Caching;
using Smartstore.Core;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Catalog.Search.Modelling;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.DataExchange.Export;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Diagnostics;
using Smartstore.Net;
using Smartstore.Web.Infrastructure.Hooks;
using Smartstore.Web.Models.Catalog;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.Media;

namespace Smartstore.Web.Controllers
{
    public partial class CatalogHelper
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly ICacheManager _cache;
        private readonly IMenuService _menuService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IProductService _productService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly ITaxService _taxService;
        private readonly ICurrencyService _currencyService;
        private readonly IMediaService _mediaService;
        private readonly ILocalizationService _localizationService;
        private readonly IPriceCalculationService _priceCalculationService;
        //private readonly IPriceFormatter _priceFormatter;
        //private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IStockSubscriptionService _stockSubscriptionService;
        private readonly IDownloadService _downloadService;
        private readonly MediaSettings _mediaSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly CaptchaSettings _captchaSettings;
        private readonly TaxSettings _taxSettings;
        private readonly PerformanceSettings _performanceSettings;
        private readonly IMeasureService _measureService;
        //private readonly IQuantityUnitService _quantityUnitService;
        private readonly MeasureSettings _measureSettings;
        private readonly IDeliveryTimeService _deliveryTimeService;
        private readonly Lazy<IDataExporter> _dataExporter;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly ICatalogSearchQueryFactory _catalogSearchQueryFactory;
        private readonly HttpRequest _httpRequest;
        private readonly IUrlHelper _urlHelper;
        private readonly ProductUrlHelper _productUrlHelper;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IUrlService _urlService;
        private readonly ILinkResolver _linkResolver;
        private readonly SocialSettings _socialSettings;
        private readonly ContactDataSettings _contactDataSettings;
        private readonly IProductTagService _productTagService;
        

        public CatalogHelper(
            SmartDbContext db,
            ICommonServices services,
            IMenuService menuService,
            IManufacturerService manufacturerService,
            IProductService productService,
            IProductAttributeService productAttributeService,
            IProductAttributeMaterializer productAttributeMaterializer,
            IProductAttributeFormatter productAttributeFormatter,
            ITaxService taxService,
            ICurrencyService currencyService,
            IMediaService mediaService,
            IPriceCalculationService priceCalculationService,
            //IPriceFormatter priceFormatter,
            //ISpecificationAttributeService specificationAttributeService,
            IDateTimeHelper dateTimeHelper,
            IStockSubscriptionService stockSubscriptionService,
            IDownloadService downloadService,
            MediaSettings mediaSettings,
            CatalogSettings catalogSettings,
            CustomerSettings customerSettings,
            CaptchaSettings captchaSettings,
            IMeasureService measureService,
            //IQuantityUnitService quantityUnitService,
            MeasureSettings measureSettings,
            TaxSettings taxSettings,
            PerformanceSettings performanceSettings,
            IDeliveryTimeService deliveryTimeService,
            Lazy<IDataExporter> dataExporter,
            ICatalogSearchService catalogSearchService,
            ICatalogSearchQueryFactory catalogSearchQueryFactory,
            IUrlHelper urlHelper,
            ProductUrlHelper productUrlHelper,
            ILocalizedEntityService localizedEntityService,
            IUrlService urlService,
            ILinkResolver linkResolver,
            SocialSettings socialSettings,
            ContactDataSettings contactDataSettings,
            IProductTagService productTagService)
        {
            _db = db;
            _services = services;
            _workContext = services.WorkContext;
            _storeContext = services.StoreContext;
            _cache = services.Cache;
            _menuService = menuService;
            _manufacturerService = manufacturerService;
            _productService = productService;
            _productAttributeService = productAttributeService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _productAttributeFormatter = productAttributeFormatter;
            _taxService = taxService;
            _currencyService = currencyService;
            _mediaService = mediaService;
            _localizationService = _services.Localization;
            _priceCalculationService = priceCalculationService;
            //_priceFormatter = priceFormatter;
            //_specificationAttributeService = specificationAttributeService;
            _dateTimeHelper = dateTimeHelper;
            _stockSubscriptionService = stockSubscriptionService;
            _downloadService = downloadService;
            _measureService = measureService;
            //_quantityUnitService = quantityUnitService;
            _measureSettings = measureSettings;
            _taxSettings = taxSettings;
            _performanceSettings = performanceSettings;
            _deliveryTimeService = deliveryTimeService;
            _mediaSettings = mediaSettings;
            _catalogSettings = catalogSettings;
            _customerSettings = customerSettings;
            _captchaSettings = captchaSettings;
            _dataExporter = dataExporter;
            _catalogSearchService = catalogSearchService;
            _catalogSearchQueryFactory = catalogSearchQueryFactory;
            _urlHelper = urlHelper;
            _productUrlHelper = productUrlHelper;
            _localizedEntityService = localizedEntityService;
            _urlService = urlService;
            _linkResolver = linkResolver;
            _httpRequest = _urlHelper.ActionContext.HttpContext.Request;
            _socialSettings = socialSettings;
            _contactDataSettings = contactDataSettings;
            _productTagService = productTagService;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;
        public DbQuerySettings QuerySettings { get; set; } = DbQuerySettings.Default;

        #region Brand

        public async Task<BrandModel> PrepareBrandModelAsync(Manufacturer manufacturer)
        {
            if (manufacturer == null)
                return null;

            var model = new BrandModel
            {
                Id = manufacturer.Id,
                Name = manufacturer.GetLocalized(x => x.Name),
                Description = manufacturer.GetLocalized(x => x.Description, detectEmptyHtml: true),
                BottomDescription = manufacturer.GetLocalized(x => x.BottomDescription, detectEmptyHtml: true),
                MetaKeywords = manufacturer.GetLocalized(x => x.MetaKeywords),
                MetaDescription = manufacturer.GetLocalized(x => x.MetaDescription),
                MetaTitle = manufacturer.GetLocalized(x => x.MetaTitle),
                SeName = await manufacturer.GetActiveSlugAsync()
            };

            model.MetaProperties = PrepareMetaPropertiesBrand(model);

            return model;
        }

        public async Task<List<BrandOverviewModel>> PrepareBrandOverviewModelAsync(
            ICollection<ProductManufacturer> brands,
            IDictionary<int, BrandOverviewModel> cachedModels = null,
            bool withPicture = false)
        {
            var model = new List<BrandOverviewModel>();
            cachedModels ??= new Dictionary<int, BrandOverviewModel>();
            IDictionary<int, MediaFileInfo> mediaFileLookup = null;

            if (withPicture)
            {
                mediaFileLookup = brands
                    .Select(x => x.Manufacturer.MediaFile)
                    .Where(x => x != null)
                    .Distinct()
                    .Select(x => _mediaService.ConvertMediaFile(x))
                    .ToDictionarySafe(x => x.Id);
            }

            foreach (var pm in brands)
            {
                var manufacturer = pm.Manufacturer;

                if (!cachedModels.TryGetValue(manufacturer.Id, out BrandOverviewModel item))
                {
                    item = new BrandOverviewModel
                    {
                        Id = manufacturer.Id,
                        Name = manufacturer.GetLocalized(x => x.Name),
                        Description = manufacturer.GetLocalized(x => x.Description, true),
                        SeName = manufacturer.GetActiveSlug()
                    };

                    if (withPicture)
                    {
                        item.Image = await PrepareBrandImageModelAsync(manufacturer, item.Name, mediaFileLookup);
                    }

                    cachedModels.Add(item.Id, item);
                }

                model.Add(item);
            }

            return model;
        }

        public async Task<ImageModel> PrepareBrandImageModelAsync(Manufacturer brand, string localizedName, IDictionary<int, MediaFileInfo> fileLookup = null)
        {
            MediaFileInfo file = null;

            if (fileLookup != null)
            {
                fileLookup.TryGetValue(brand.MediaFileId ?? 0, out file);
            }
            else
            {
                if (brand.MediaFile != null)
                {
                    file = _mediaService.ConvertMediaFile(brand.MediaFile);
                }
                else if (brand.MediaFileId.HasValue)
                {
                    file = await _mediaService.GetFileByIdAsync(brand.MediaFileId.Value, MediaLoadFlags.AsNoTracking);
                }          
            }

            var model = new ImageModel
            {
                File = file,
                ThumbSize = _mediaSettings.ManufacturerThumbPictureSize,
                Title = file?.File?.GetLocalized(x => x.Title)?.Value.NullEmpty() ?? string.Format(T("Media.Manufacturer.ImageLinkTitleFormat"), localizedName),
                Alt = file?.File?.GetLocalized(x => x.Alt)?.Value.NullEmpty() ?? string.Format(T("Media.Manufacturer.ImageAlternateTextFormat"), localizedName),
                NoFallback = _catalogSettings.HideManufacturerDefaultPictures
            };

            _services.DisplayControl.Announce(file?.File);

            return model;
        }

        public async Task<BrandNavigationModel> PrepareBrandNavigationModelAsync(int brandItemsToDisplay)
        {
            var storeId = _services.StoreContext.CurrentStore.Id;
            var storeToken = QuerySettings.IgnoreMultiStore ? "0" : storeId.ToString();
            var rolesToken = QuerySettings.IgnoreAcl ? "0" : _services.WorkContext.CurrentCustomer.GetRolesIdent();

            var settingsKeyPart = string.Join(",",
                _catalogSettings.ShowManufacturersOnHomepage,
                _catalogSettings.ShowManufacturerPictures,
                _catalogSettings.HideManufacturerDefaultPictures,
                _mediaSettings.ManufacturerThumbPictureSize).ToLower();

            var cacheKey = string.Format(ModelCacheInvalidator.MANUFACTURER_NAVIGATION_MODEL_KEY,
                settingsKeyPart,
                _services.WorkContext.WorkingLanguage.Id,
                storeToken,
                rolesToken,
                brandItemsToDisplay);

            var cacheModel = await _services.CacheFactory.GetMemoryCache().GetAsync(cacheKey, async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromHours(6));

                var manufacturers = await _db.Manufacturers
                    .AsNoTracking()
                    .ApplyStandardFilter(false, _workContext.CurrentCustomer.GetRoleIds(), storeId)
                    .Take(brandItemsToDisplay + 1)
                    .ToListAsync();

                var files = new Dictionary<int, MediaFileInfo>();

                if (_catalogSettings.ShowManufacturerPictures)
                {
                    var fileIds = manufacturers
                        .Select(x => x.MediaFileId ?? 0)
                        .Where(x => x != 0)
                        .Distinct()
                        .ToArray();
                    files = (await _mediaService.GetFilesByIdsAsync(fileIds)).ToDictionarySafe(x => x.Id);
                }

                var model = new BrandNavigationModel
                {
                    DisplayBrands = _catalogSettings.ShowManufacturersOnHomepage,
                    DisplayImages = _catalogSettings.ShowManufacturerPictures,
                    DisplayAllBrandsLink = manufacturers.Count > brandItemsToDisplay,
                    HideBrandDefaultPictures = _catalogSettings.HideManufacturerDefaultPictures,
                    BrandThumbImageSize = _mediaSettings.ManufacturerThumbPictureSize
                };

                if (model.DisplayAllBrandsLink)
                {
                    brandItemsToDisplay -= 1;
                }

                foreach (var manufacturer in manufacturers.Take(brandItemsToDisplay))
                {
                    files.TryGetValue(manufacturer.MediaFileId ?? 0, out var file);

                    var name = manufacturer.GetLocalized(x => x.Name);

                    model.Brands.Add(new BrandBriefInfoModel
                    {
                        Id = manufacturer.Id,
                        Name = name,
                        SeName = manufacturer.GetActiveSlug(),
                        DisplayOrder = manufacturer.DisplayOrder,
                        Image = new ImageModel
                        {
                            File = file,
                            ThumbSize = _mediaSettings.ManufacturerThumbPictureSize,
                            Alt = file?.File?.GetLocalized(x => x.Alt)?.Value.NullEmpty() ?? name,
                            Title = file?.File?.GetLocalized(x => x.Title)?.Value.NullEmpty() ?? name,
                            NoFallback = _catalogSettings.HideManufacturerDefaultPictures
                        }
                    });
                }

                return model;
            });

            return cacheModel;
        }

        #endregion

        #region Category

        public async Task<CategoryModel> PrepareCategoryModelAsync(Category category)
        {
            if (category == null)
                return null;

            var model = new CategoryModel
            {
                Id = category.Id,
                Name = category.GetLocalized(x => x.Name),
                FullName = category.GetLocalized(x => x.FullName),
                Description = category.GetLocalized(x => x.Description, detectEmptyHtml: true),
                BottomDescription = category.GetLocalized(x => x.BottomDescription, detectEmptyHtml: true),
                MetaKeywords = category.GetLocalized(x => x.MetaKeywords),
                MetaDescription = category.GetLocalized(x => x.MetaDescription),
                MetaTitle = category.GetLocalized(x => x.MetaTitle),
                SeName = await category.GetActiveSlugAsync()
            };

            model.MetaProperties = PrepareMetaPropertiesCategory(model);

            return model;
        }

        public async Task<List<CategorySummaryModel>> MapCategorySummaryModelAsync(IEnumerable<Category> categories, int thumbSize)
        {
            Guard.NotNull(categories, nameof(categories));

            var fileIds = categories
                .Select(x => x.MediaFileId ?? 0)
                .Where(x => x != 0)
                .Distinct()
                .ToArray();

            var files = (await _mediaService.GetFilesByIdsAsync(fileIds)).ToDictionarySafe(x => x.Id);

            return await categories
                .SelectAsync(async c =>
                {
                    var name = c.GetLocalized(y => y.Name);
                    var model = new CategorySummaryModel
                    {
                        Id = c.Id,
                        Name = name
                    };

                    _services.DisplayControl.Announce(c);

                    // Generate URL.
                    if (c.ExternalLink.HasValue())
                    {
                        var link = await _linkResolver.ResolveAsync(c.ExternalLink);
                        if (link.Status == LinkStatus.Ok)
                        {
                            model.Url = link.Link;
                        }
                    }

                    if (model.Url.IsEmpty())
                    {
                        model.Url = _urlHelper.RouteUrl(nameof(Category), new { SeName = c.GetActiveSlug() });
                    }

                    files.TryGetValue(c.MediaFileId ?? 0, out var file);

                    model.Image = new ImageModel
                    {
                        File = file,
                        ThumbSize = thumbSize,
                        Title = file?.File?.GetLocalized(x => x.Title)?.Value.NullEmpty() ?? string.Format(T("Media.Category.ImageLinkTitleFormat"), name),
                        Alt = file?.File?.GetLocalized(x => x.Alt)?.Value.NullEmpty() ?? string.Format(T("Media.Category.ImageAlternateTextFormat"), name),
                        NoFallback = _catalogSettings.HideCategoryDefaultPictures
                    };

                    _services.DisplayControl.Announce(file?.File);

                    return model;
                })
                .AsyncToList();
        }

        public async Task<IEnumerable<int>> GetChildCategoryIdsAsync(int parentCategoryId, bool deep = true)
        {
            var root = await _menuService.GetRootNodeAsync("Main");
            var node = root.SelectNodeById(parentCategoryId) ?? root.SelectNode(x => x.Value.EntityId == parentCategoryId);
            if (node != null)
            {
                var children = deep ? node.Flatten(false) : node.Children.Select(x => x.Value);
                var ids = children.Select(x => x.EntityId);
                return ids;
            }

            return Enumerable.Empty<int>();
        }

        public async Task GetBreadcrumbAsync(IBreadcrumb breadcrumb, ActionContext context, Product product = null)
        {
            var menu = await _menuService.GetMenuAsync("Main");
            var currentNode = await menu.ResolveCurrentNodeAsync(context);

            if (currentNode != null)
            {
                currentNode.Trail.Where(x => !x.IsRoot).Each(x => breadcrumb.Track(x.Value));
            }

            // Add trail of parent product if product has no category assigned.
            if (product != null && !(breadcrumb.Trail?.Any() ?? false))
            {
                var parentProduct = await _db.Products.FindByIdAsync(product.ParentGroupedProductId, false);
                if (parentProduct != null)
                {
                    var routeData = new RouteData();
                    routeData.Values.Add("currentProductId", parentProduct.Id);
                    var actionContext = new ActionContext(context);

                    currentNode = await menu.ResolveCurrentNodeAsync(actionContext);
                    if (currentNode != null)
                    {
                        currentNode.Trail.Where(x => !x.IsRoot).Each(x => breadcrumb.Track(x.Value));
                        var parentName = parentProduct.GetLocalized(x => x.Name);

                        breadcrumb.Track(new MenuItem
                        {
                            Text = parentName,
                            Rtl = parentName.CurrentLanguage.Rtl,
                            EntityId = parentProduct.Id,
                            Url = _urlHelper.RouteUrl("Product", new { SeName = await parentProduct.GetActiveSlugAsync() })
                        });
                    }
                }
            }
        }

        public async Task<ImageModel> PrepareCategoryImageModelAsync(Category category, string localizedName, IDictionary<int, MediaFileInfo> fileLookup = null)
        {
            MediaFileInfo file = null;

            if (fileLookup != null)
            {
                fileLookup.TryGetValue(category.MediaFileId ?? 0, out file);
            }
            else
            {
                if (category.MediaFile != null)
                {
                    file = _mediaService.ConvertMediaFile(category.MediaFile);
                }
                else if (category.MediaFileId.HasValue)
                {
                    file = await _mediaService.GetFileByIdAsync(category.MediaFileId.Value, MediaLoadFlags.AsNoTracking);
                }
            }

            var model = new ImageModel
            {
                File = file,
                ThumbSize = _mediaSettings.CategoryThumbPictureSize,
                Title = file?.File?.GetLocalized(x => x.Title)?.Value.NullEmpty() ?? string.Format(T("Media.Category.ImageLinkTitleFormat"), localizedName),
                Alt = file?.File?.GetLocalized(x => x.Alt)?.Value.NullEmpty() ?? string.Format(T("Media.Category.ImageAlternateTextFormat"), localizedName),
                NoFallback = _catalogSettings.HideCategoryDefaultPictures
            };

            _services.DisplayControl.Announce(file?.File);

            return model;
        }

        #endregion

        #region Product

        public Task<ProductDetailsModel> MapProductDetailsPageModelAsync(Product product, ProductVariantQuery query)
        {
            Guard.NotNull(product, nameof(product));

            var customer = _services.WorkContext.CurrentCustomer;
            var store = _services.StoreContext.CurrentStore;
            var modelContext = new ProductDetailsModelContext
            {
                Product = product,
                VariantQuery = query,
                Customer = customer,
                Store = store,
                Currency = _services.WorkContext.WorkingCurrency,
                BatchContext = _productService.CreateProductBatchContext(new[] { product }, store, customer, false)
            };

            return MapProductDetailsPageModelAsync(modelContext);
        }

        protected internal virtual async Task<ProductDetailsModel> MapProductDetailsPageModelAsync(ProductDetailsModelContext modelContext)
        {
            Guard.NotNull(modelContext, nameof(modelContext));

            var product = modelContext.Product;
            var query = modelContext.VariantQuery;
            var batchContext = modelContext.BatchContext;
            var isAssociatedProduct = modelContext.IsAssociatedProduct;
            var isBundleItem = modelContext.ProductBundleItem != null;

            using (_services.Chronometer.Step("PrepareProductDetailsPageModel"))
            {
                var model = new ProductDetailsModel
                {
                    Id = product.Id,
                    Name = product.GetLocalized(x => x.Name),
                    ShortDescription = product.GetLocalized(x => x.ShortDescription),
                    FullDescription = product.GetLocalized(x => x.FullDescription, detectEmptyHtml: true),
                    MetaKeywords = product.GetLocalized(x => x.MetaKeywords),
                    MetaDescription = product.GetLocalized(x => x.MetaDescription),
                    MetaTitle = product.GetLocalized(x => x.MetaTitle),
                    SeName = await product.GetActiveSlugAsync(),
                    ProductType = product.ProductType,
                    VisibleIndividually = product.Visibility != ProductVisibility.Hidden,
                    ReviewCount = product.ApprovedTotalReviews,
                    DisplayAdminLink = await _services.Permissions.AuthorizeAsync(Permissions.System.AccessBackend, batchContext.Customer),
                    Condition = product.Condition,
                    ShowCondition = _catalogSettings.ShowProductCondition,
                    LocalizedCondition = await product.Condition.GetLocalizedEnumAsync(_services.WorkContext.WorkingLanguage.Id, false),
                    ShowSku = _catalogSettings.ShowProductSku,
                    Sku = product.Sku,
                    ShowManufacturerPartNumber = _catalogSettings.ShowManufacturerPartNumber,
                    DisplayProductReviews = _catalogSettings.ShowProductReviewsInProductDetail && product.AllowCustomerReviews,
                    ManufacturerPartNumber = product.ManufacturerPartNumber,
                    ShowGtin = _catalogSettings.ShowGtin,
                    Gtin = product.Gtin,
                    StockAvailability = product.FormatStockMessage(_localizationService),
                    HasSampleDownload = product.IsDownload && product.HasSampleDownload,
                    IsCurrentCustomerRegistered = batchContext.Customer.IsRegistered(),
                    IsAssociatedProduct = isAssociatedProduct,
                    CompareEnabled = !isAssociatedProduct && _catalogSettings.CompareProductsEnabled,
                    TellAFriendEnabled = !isAssociatedProduct && _catalogSettings.EmailAFriendEnabled,
                    AskQuestionEnabled = !isAssociatedProduct && _catalogSettings.AskQuestionEnabled,
                    PriceDisplayStyle = _catalogSettings.PriceDisplayStyle,
                    DisplayTextForZeroPrices = _catalogSettings.DisplayTextForZeroPrices
                };

                #region Bundles / Grouped products

                if (product.ProductType == ProductType.GroupedProduct && !isAssociatedProduct)
                {
                    // Associated products.
                    var searchQuery = new CatalogSearchQuery()
                        .VisibleOnly(batchContext.Customer)
                        .HasStoreId(batchContext.Store.Id)
                        .HasParentGroupedProduct(product.Id);

                    modelContext.AssociatedProducts = await (await _catalogSearchService.SearchAsync(searchQuery)).GetHitsAsync();

                    // Push Ids of associated products to batch context to save roundtrips
                    batchContext.Collect(modelContext.AssociatedProducts.Select(x => x.Id).ToArray());

                    foreach (var associatedProduct in modelContext.AssociatedProducts)
                    {
                        var childModelContext = new ProductDetailsModelContext(modelContext)
                        {
                            Product = associatedProduct,
                            IsAssociatedProduct = true,
                            ProductBundleItem = null
                        };

                        var assciatedProductModel = await MapProductDetailsPageModelAsync(childModelContext);
                        model.AssociatedProducts.Add(assciatedProductModel);
                    }
                }
                else if (product.ProductType == ProductType.BundledProduct && !isBundleItem)
                {
                    // Bundled items.
                    var bundleItems = await batchContext.ProductBundleItems.GetOrLoadAsync(product.Id);

                    if (bundleItems.Count > 0)
                    {
                        modelContext.BundleItemDatas = bundleItems
                            .Where(x => x.Product.CanBeBundleItem())
                            .Select(x => new ProductBundleItemData(x))
                            .ToList();

                        // Push Ids of bundle items to batch context to save roundtrips
                        batchContext.Collect(modelContext.BundleItemDatas.Select(x => x.Item.ProductId).ToArray());

                        foreach (var itemData in modelContext.BundleItemDatas)
                        {
                            var item = itemData.Item;
                            var childModelContext = new ProductDetailsModelContext(modelContext)
                            {
                                Product = item.Product,
                                IsAssociatedProduct = false,
                                ProductBundleItem = itemData
                            };

                            var bundledProductModel = await MapProductDetailsPageModelAsync(childModelContext);

                            bundledProductModel.ShowLegalInfo = false;
                            bundledProductModel.DeliveryTimesPresentation = DeliveryTimesPresentation.None;

                            bundledProductModel.BundleItem.Id = item.Id;
                            bundledProductModel.BundleItem.Quantity = item.Quantity;
                            bundledProductModel.BundleItem.HideThumbnail = item.HideThumbnail;
                            bundledProductModel.BundleItem.Visible = item.Visible;
                            bundledProductModel.BundleItem.IsBundleItemPricing = item.BundleProduct.BundlePerItemPricing;

                            var bundleItemName = item.GetLocalized(x => x.Name);
                            if (bundleItemName.Value.HasValue())
                            {
                                bundledProductModel.Name = bundleItemName;
                            }
                                
                            var bundleItemShortDescription = item.GetLocalized(x => x.ShortDescription);
                            if (bundleItemShortDescription.Value.HasValue())
                            {
                                bundledProductModel.ShortDescription = bundleItemShortDescription;
                            }

                            model.BundledItems.Add(bundledProductModel);
                        }
                    }
                }

                #endregion

                #region Template

                var templateCacheKey = string.Format(ModelCacheInvalidator.PRODUCT_TEMPLATE_MODEL_KEY, product.ProductTemplateId);
                model.ProductTemplateViewPath = await _services.Cache.GetAsync(templateCacheKey, async () =>
                {
                    var template = await _db.ProductTemplates.FindByIdAsync(product.ProductTemplateId, false)
                        ?? await _db.ProductTemplates.AsNoTracking().FirstOrDefaultAsync();

                    return template.ViewPath;
                });

                #endregion

                #region Brands

                // Brands
                if (_catalogSettings.ShowManufacturerPicturesInProductDetail)
                {
                    var brands = _db.IsCollectionLoaded(product, x => x.ProductManufacturers)
                        // TODO: (core) Authorize for and ACL? But does make no sense here?! Hmmm...
                        ? product.ProductManufacturers
                        : await batchContext.ProductManufacturers.GetOrLoadAsync(product.Id);
                    
                    model.Brands = await PrepareBrandOverviewModelAsync(brands, null, true);
                }

                #endregion

                #region Review overview

                model.ReviewOverview.ProductId = product.Id;
                model.ReviewOverview.RatingSum = product.ApprovedRatingSum;
                model.ReviewOverview.TotalReviews = product.ApprovedTotalReviews;
                model.ReviewOverview.AllowCustomerReviews = product.AllowCustomerReviews;

                #endregion

                #region Share code

                // Social share code.
                if (_catalogSettings.ShowShareButton && _catalogSettings.PageShareCode.HasValue())
                {
                    var shareCode = _catalogSettings.PageShareCode;
                    if (_services.WebHelper.IsCurrentConnectionSecured())
                    {
                        // Need to change the addthis link to be https linked when the page is, so that the page doesn't ask about mixed mode when viewed in https...
                        shareCode = shareCode.Replace("http://", "https://");
                    }

                    model.ProductShareCode = shareCode;
                }

                #endregion

                #region Giftcard

                // Get gift card values from query string.
                if (product.IsGiftCard)
                {
                    model.GiftCard.RecipientName = query.GetGiftCardValue(product.Id, 0, "RecipientName");
                    model.GiftCard.RecipientEmail = query.GetGiftCardValue(product.Id, 0, "RecipientEmail");
                    model.GiftCard.SenderName = query.GetGiftCardValue(product.Id, 0, "SenderName");
                    model.GiftCard.SenderEmail = query.GetGiftCardValue(product.Id, 0, "SenderEmail");
                    model.GiftCard.Message = query.GetGiftCardValue(product.Id, 0, "Message");
                }

                #endregion

                #region Stock subscription

                if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock &&
                     product.BackorderMode == BackorderMode.NoBackorders &&
                     product.AllowBackInStockSubscriptions &&
                     product.StockQuantity <= 0)
                {
                    // Out of stock.
                    model.DisplayBackInStockSubscription = true;
                    model.BackInStockAlreadySubscribed = await _stockSubscriptionService.IsSubscribedAsync(product, batchContext.Customer, batchContext.Store.Id);
                }

                #endregion

                // ----> Core mapper <------
                await PrepareProductDetailModelAsync(model, modelContext);

                #region Action items

                if (model.HasSampleDownload)
                {
                    model.ActionItems["sample"] = new ProductDetailsModel.ActionItemModel
                    {
                        Key = "sample",
                        Title = T("Products.DownloadSample"),
                        CssClass = "action-download-sample",
                        IconCssClass = "fa fa-download",
                        Href = _urlHelper.Action("Sample", "Download", new { productId = model.Id }),
                        IsPrimary = true,
                        PrimaryActionColor = "danger"
                    };
                }

                if (!model.AddToCart.DisableWishlistButton && model.ProductType != ProductType.GroupedProduct)
                {
                    model.ActionItems["wishlist"] = new ProductDetailsModel.ActionItemModel
                    {
                        Key = "wishlist",
                        Title = T("ShoppingCart.AddToWishlist.Short"),
                        Tooltip = T("ShoppingCart.AddToWishlist"),
                        CssClass = "ajax-cart-link action-add-to-wishlist",
                        IconCssClass = "icm icm-heart",
                        Href = _urlHelper.Action("AddProduct", "ShoppingCart", new { productId = model.Id, shoppingCartTypeId = (int)ShoppingCartType.Wishlist })
                    };
                }

                if (model.CompareEnabled)
                {
                    model.ActionItems["compare"] = new ProductDetailsModel.ActionItemModel
                    {
                        Key = "compare",
                        Title = T("Common.Shopbar.Compare"),
                        Tooltip = T("Products.Compare.AddToCompareList"),
                        CssClass = "action-compare ajax-cart-link",
                        IconCssClass = "icm icm-repeat",
                        Href = _urlHelper.Action("AddProductToCompare", "Catalog", new { id = model.Id })
                    };
                }

                if (model.AskQuestionEnabled && !model.ProductPrice.CallForPrice)
                {
                    model.ActionItems["ask"] = new ProductDetailsModel.ActionItemModel
                    {
                        Key = "ask",
                        Title = T("Products.AskQuestion.Short"),
                        Tooltip = T("Products.AskQuestion"),
                        CssClass = "action-ask-question",
                        IconCssClass = "icm icm-envelope",
                        Href = _urlHelper.Action("AskQuestion", new { id = model.Id })
                    };
                }

                if (model.TellAFriendEnabled)
                {
                    model.ActionItems["tell"] = new ProductDetailsModel.ActionItemModel
                    {
                        Key = "tell",
                        Title = T("Products.EmailAFriend"),
                        CssClass = "action-bullhorn",
                        IconCssClass = "icm icm-bullhorn",
                        Href = _urlHelper.Action("EmailAFriend", new { id = model.Id })
                    };
                }

                #endregion

                #region Media

                ICollection<int> combinationFileIds = null;
                ProductVariantAttributeCombination combination = null;

                if (modelContext.ProductBundleItem == null)
                {
                    combinationFileIds = await _productAttributeService.GetAttributeCombinationFileIdsAsync(product);
                    combination ??= model.SelectedCombination;
                }

                var productMediaFiles = _db.IsCollectionLoaded(product, x => x.ProductPictures)
                    ? product.ProductPictures
                    : await batchContext.ProductMediaFiles.GetOrLoadAsync(product.Id);

                var files = productMediaFiles
                    .Where(x => x.MediaFile != null)
                    .Select(x => _mediaService.ConvertMediaFile(x.MediaFile))
                    .ToList();

                if (product.HasPreviewPicture && files.Count > 1)
                {
                    files.RemoveAt(0);
                }

                model.MediaGalleryModel = PrepareProductDetailsMediaGalleryModel(
                    files, model.Name, combinationFileIds, isAssociatedProduct, modelContext.ProductBundleItem, combination);

                #endregion

                return model;
            }
        }

        public async Task PrepareProductDetailModelAsync(ProductDetailsModel model, ProductDetailsModelContext modelContext, int selectedQuantity = 1)
        {
            Guard.NotNull(model, nameof(model));
            Guard.NotNull(modelContext, nameof(modelContext));

            var product = modelContext.Product;

            //var preSelectedPriceAdjustmentBase = new Money();
            var preSelectedWeightAdjustment = decimal.Zero;
            //var displayPrices = await _services.Permissions.AuthorizeAsync(Permissions.Catalog.DisplayPrice);
            var isBundle = product.ProductType == ProductType.BundledProduct;
            //var isBundleItemPricing = productBundleItem != null && productBundleItem.Item.BundleProduct.BundlePerItemPricing;
            //var isBundlePricing = productBundleItem != null && !productBundleItem.Item.BundleProduct.BundlePerItemPricing;
            //var bundleItemId = productBundleItem == null ? 0 : productBundleItem.Item.Id;

            var variantAttributes = isBundle
                ? new List<ProductVariantAttribute>() 
                : await modelContext.BatchContext.Attributes.GetOrLoadAsync(product.Id);

            model.IsBundlePart = product.ProductType != ProductType.BundledProduct && modelContext.ProductBundleItem != null;
            model.ProductPrice.DynamicPriceUpdate = _catalogSettings.EnableDynamicPriceUpdate;
            model.ProductPrice.BundleItemShowBasePrice = _catalogSettings.BundleItemShowBasePrice;

            if (!model.ProductPrice.DynamicPriceUpdate)
            {
                selectedQuantity = 1;
            }

            IList<ProductVariantAttributeValue> selectedAttributeValues = null;
            var hasSelectedAttributesValues = false;

            // Bundles don't have attributes.
            if (!isBundle)
            {
                // Attributes
                await PrepareProductAttributesModelAsync(model, modelContext, variantAttributes);

                // Attribute combinations
                (selectedAttributeValues, hasSelectedAttributesValues) = await PrepareProductAttributeCombinationsModel(model, modelContext, variantAttributes);
            }

            // Properties
            await PrepareProductPropertiesModelAsync(model, modelContext, selectedAttributeValues, preSelectedWeightAdjustment, hasSelectedAttributesValues);

            // Price
            await PrepareProductPriceModelAsync(model, modelContext);

            // AddToCart
            await PrepareProductCartModelAsync(model, modelContext, selectedQuantity);

            // GiftCards
            PrepareProductGiftCardsModel(model, modelContext);

            // Specifications
            model.SpecificationAttributes = await PrepareProductSpecificationModelAsync(modelContext);

            // Reviews
            await PrepareProductReviewsModelAsync(model.ProductReviews, product, 10);

            // Tags
            await PrepareProductTagsModelAsync(model, product);

            // Related products
            await PrepareRelatedProductsModelAsync(model, product);

            // Also purchased products
            await PrepareAlsoPurchasedProductsModelAsync(model, product);

            _services.DisplayControl.Announce(product);
        }

        #region PrepareProductDetailModelAsync helper methods

        // TODO: (mh) (core) preSelectedPriceAdjustmentBase & preSelectedWeightAdjustment might have to be return params.
        // Implement along with pricing when available.
        protected async Task PrepareProductAttributesModelAsync(ProductDetailsModel model, ProductDetailsModelContext modelContext, ICollection<ProductVariantAttribute> variantAttributes)
        {
            var product = modelContext.Product;
            var productBundleItem = modelContext.ProductBundleItem;
            var query = modelContext.VariantQuery;
            var bundleItemId = productBundleItem == null ? 0 : productBundleItem.Item.Id;
            var hasSelectedAttributes = query.Variants.Any();

            foreach (var attribute in variantAttributes)
            {
                var pvaModel = new ProductDetailsModel.ProductVariantAttributeModel
                {
                    Id = attribute.Id,
                    ProductId = attribute.ProductId,
                    BundleItemId = bundleItemId,
                    ProductAttributeId = attribute.ProductAttributeId,
                    ProductAttribute = attribute,
                    Alias = attribute.ProductAttribute.Alias,
                    Name = attribute.ProductAttribute.GetLocalized(x => x.Name),
                    Description = attribute.ProductAttribute.GetLocalized(x => x.Description),
                    TextPrompt = attribute.TextPrompt,
                    CustomData = attribute.CustomData,
                    IsRequired = attribute.IsRequired,
                    AttributeControlType = attribute.AttributeControlType,
                    AllowedFileExtensions = _catalogSettings.FileUploadAllowedExtensions
                };

                if (hasSelectedAttributes)
                {
                    var selectedVariant = query.Variants.FirstOrDefault(x =>
                        x.ProductId == product.Id &&
                        x.BundleItemId == bundleItemId &&
                        x.AttributeId == attribute.ProductAttributeId &&
                        x.VariantAttributeId == attribute.Id);

                    if (selectedVariant != null)
                    {
                        switch (attribute.AttributeControlType)
                        {
                            case AttributeControlType.Datepicker:
                                if (selectedVariant.Date.HasValue)
                                {
                                    pvaModel.SelectedDay = selectedVariant.Date.Value.Day;
                                    pvaModel.SelectedMonth = selectedVariant.Date.Value.Month;
                                    pvaModel.SelectedYear = selectedVariant.Date.Value.Year;
                                }
                                break;
                            case AttributeControlType.FileUpload:
                                pvaModel.UploadedFileGuid = selectedVariant.Value;

                                Guid guid;
                                if (selectedVariant.Value.HasValue() && Guid.TryParse(selectedVariant.Value, out guid))
                                {
                                    var downloadFileName = await _db.Downloads
                                        .AsNoTracking()
                                        .Where(x => x.DownloadGuid == guid)
                                        .Select(x => x.MediaFile.Name)
                                        .FirstOrDefaultAsync();

                                    if (downloadFileName != null)
                                    {
                                        pvaModel.UploadedFileName = downloadFileName;
                                    }
                                }
                                break;
                            case AttributeControlType.TextBox:
                            case AttributeControlType.MultilineTextbox:
                                pvaModel.TextValue = selectedVariant.Value;
                                break;
                        }
                    }
                }

                // TODO: obsolete? Alias field is not used for custom values anymore, only for URL as URL variant alias.
                if (attribute.AttributeControlType == AttributeControlType.Datepicker && pvaModel.Alias.HasValue() && RegularExpressions.IsYearRange.IsMatch(pvaModel.Alias))
                {
                    var match = RegularExpressions.IsYearRange.Match(pvaModel.Alias);
                    pvaModel.BeginYear = match.Groups[1].Value.ToInt();
                    pvaModel.EndYear = match.Groups[2].Value.ToInt();
                }
                
                var preSelectedValueId = 0;
                var pvaValues = !attribute.IsListTypeAttribute()
                    ? new List<ProductVariantAttributeValue>()
                    : attribute.ProductVariantAttributeValues.OrderBy(x => x.DisplayOrder).ToList();
                
                foreach (var pvaValue in pvaValues)
                {
                    ProductBundleItemAttributeFilter attributeFilter = null;

                    if (productBundleItem?.Item?.IsFilteredOut(pvaValue, out attributeFilter) ?? false)
                    {
                        continue;
                    }
                    if (preSelectedValueId == 0 && attributeFilter != null && attributeFilter.IsPreSelected)
                    {
                        preSelectedValueId = attributeFilter.AttributeValueId;
                    }

                    var linkedProduct = await _db.Products.FindByIdAsync(pvaValue.LinkedProductId);

                    var pvaValueModel = new ProductDetailsModel.ProductVariantAttributeValueModel
                    {
                        Id = pvaValue.Id,
                        ProductAttributeValue = pvaValue,
                        PriceAdjustment = string.Empty,
                        Name = pvaValue.GetLocalized(x => x.Name),
                        Alias = pvaValue.Alias,
                        Color = pvaValue.Color, // Used with "Boxes" attribute type.
                        IsPreSelected = pvaValue.IsPreSelected
                    };

                    if (linkedProduct != null && linkedProduct.Visibility != ProductVisibility.Hidden)
                    {
                        pvaValueModel.SeName = await linkedProduct.GetActiveSlugAsync();
                    }

                    // TODO: (mh) (core) Implement when price calculation pipeline is ready.
                    // Display price if allowed.
                    //if (displayPrices && !isBundlePricing)
                    //{
                    //    var attributeValuePriceAdjustment = await _priceCalculationService.GetProductVariantAttributeValuePriceAdjustmentAsync(pvaValue, product, customer, null, selectedQuantity);
                    //    var priceAdjustmentBase = await _taxService.GetProductPriceAsync(product, attributeValuePriceAdjustment, out var _);
                    //    var priceAdjustment = _currencyService.ConvertFromPrimaryStoreCurrency(priceAdjustmentBase, currency);

                    //    if (_catalogSettings.ShowVariantCombinationPriceAdjustment && !product.CallForPrice)
                    //    {
                    //        if (priceAdjustmentBase > decimal.Zero)
                    //        {
                    //            pvaValueModel.PriceAdjustment = "+" + _priceFormatter.FormatPrice(priceAdjustment, true, false);
                    //        }
                    //        else if (priceAdjustmentBase < decimal.Zero)
                    //        {
                    //            pvaValueModel.PriceAdjustment = "-" + _priceFormatter.FormatPrice(-priceAdjustment, true, false);
                    //        }
                    //    }

                    //    if (pvaValueModel.IsPreSelected)
                    //    {
                    //        preSelectedPriceAdjustmentBase = decimal.Add(preSelectedPriceAdjustmentBase, priceAdjustmentBase);
                    //        preSelectedWeightAdjustment = decimal.Add(preSelectedWeightAdjustment, pvaValue.WeightAdjustment);
                    //    }

                    //    if (_catalogSettings.ShowLinkedAttributeValueQuantity && pvaValue.ValueType == ProductVariantAttributeValueType.ProductLinkage)
                    //    {
                    //        pvaValueModel.QuantityInfo = pvaValue.Quantity;
                    //    }

                    //    pvaValueModel.PriceAdjustmentValue = priceAdjustment;
                    //}

                    if (_catalogSettings.ShowLinkedAttributeValueImage && pvaValue.ValueType == ProductVariantAttributeValueType.ProductLinkage)
                    {
                        var linkageFile = await _db.ProductMediaFiles
                            .AsNoTracking()
                            .ApplyProductFilter(new[] { pvaValue.LinkedProductId }, 1)
                            .Include(x => x.MediaFile)
                            .FirstOrDefaultAsync();

                        if (linkageFile != null)
                        {
                            pvaValueModel.ImageUrl = _mediaService.GetUrl(linkageFile.MediaFile, _mediaSettings.VariantValueThumbPictureSize, null, false);
                        }
                    }
                    else if (pvaValue.MediaFileId != 0)
                    {
                        pvaValueModel.ImageUrl = await _mediaService.GetUrlAsync(pvaValue.MediaFileId, _mediaSettings.VariantValueThumbPictureSize, null, false);
                    }

                    pvaModel.Values.Add(pvaValueModel);
                }

                // We need selected attributes for initially displayed combination images and multiple selected checkbox values.
                if (query.VariantCombinationId == 0)
                {
                    ProductDetailsModel.ProductVariantAttributeValueModel defaultValue = null;

                    // Value pre-selected by a bundle item filter discards the default pre-selection.
                    if (preSelectedValueId != 0)
                    {
                        pvaModel.Values.Each(x => x.IsPreSelected = false);

                        defaultValue = pvaModel.Values.OfType<ProductDetailsModel.ProductVariantAttributeValueModel>().FirstOrDefault(v => v.Id == preSelectedValueId);

                        if (defaultValue != null)
                        {
                            defaultValue.IsPreSelected = true;
                            query.AddVariant(new ProductVariantQueryItem(defaultValue.Id.ToString())
                            {
                                ProductId = product.Id,
                                BundleItemId = bundleItemId,
                                AttributeId = attribute.ProductAttributeId,
                                VariantAttributeId = attribute.Id,
                                Alias = attribute.ProductAttribute.Alias,
                                ValueAlias = defaultValue.Alias
                            });
                        }
                    }

                    if (defaultValue == null)
                    {
                        foreach (var value in pvaModel.Values.Where(x => x.IsPreSelected))
                        {
                            query.AddVariant(new ProductVariantQueryItem(value.Id.ToString())
                            {
                                ProductId = product.Id,
                                BundleItemId = bundleItemId,
                                AttributeId = attribute.ProductAttributeId,
                                VariantAttributeId = attribute.Id,
                                Alias = attribute.ProductAttribute.Alias,
                                ValueAlias = value.Alias
                            });
                        }
                    }
                }

                model.ProductVariantAttributes.Add(pvaModel);
            }
        }

        protected async Task<(IList<ProductVariantAttributeValue>, bool)> PrepareProductAttributeCombinationsModel(
            ProductDetailsModel model, 
            ProductDetailsModelContext modelContext, 
            ICollection<ProductVariantAttribute> variantAttributes)
        {
            // TODO: (mh) (core) Must be out param as it is needed in subsequent code.
            IList<ProductVariantAttributeValue> selectedAttributeValues = null;
            var hasSelectedAttributesValues = false;

            var product = modelContext.Product;
            var productBundleItem = modelContext.ProductBundleItem;
            var query = modelContext.VariantQuery;
            var bundleItemId = productBundleItem == null ? 0 : productBundleItem.Item.Id;
            var isBundlePricing = productBundleItem != null && !productBundleItem.Item.BundleProduct.BundlePerItemPricing;
            var res = new Dictionary<string, LocalizedString>(StringComparer.OrdinalIgnoreCase)
            {
                { "Products.Availability.IsNotActive", T("Products.Availability.IsNotActive") },
                { "Products.Availability.OutOfStock", T("Products.Availability.OutOfStock") },
                { "Products.Availability.Backordering", T("Products.Availability.Backordering") },
            };

            if (query.Variants.Any() || query.VariantCombinationId != 0)
            {
                // Merge with combination data if there's a match.
                var warnings = new List<string>();
                var checkAvailability = product.AttributeChoiceBehaviour == AttributeChoiceBehaviour.GrayOutUnavailable;
                ProductVariantAttributeSelection attributesSelection;

                if (query.VariantCombinationId != 0)
                {
                    var combination = await _db.ProductVariantAttributeCombinations.FindByIdAsync(query.VariantCombinationId, false);
                    attributesSelection = new ProductVariantAttributeSelection(combination?.RawAttributes ?? string.Empty);
                }
                else
                {
                    var selection = await _productAttributeMaterializer.CreateAttributeSelectionAsync(query, variantAttributes, product.Id, bundleItemId, false);
                    attributesSelection = selection.Selection;
                }

                selectedAttributeValues = await _productAttributeMaterializer.MaterializeProductVariantAttributeValuesAsync(attributesSelection);
                hasSelectedAttributesValues = attributesSelection.AttributesMap.Any();

                if (isBundlePricing)
                {
                    model.AttributeInfo = await _productAttributeFormatter.FormatAttributesAsync(
                        attributesSelection,
                        product,
                        modelContext.Customer,
                        separator: ", ",
                        includePrices: false,
                        includeGiftCardAttributes: false,
                        includeHyperlinks: false);
                }

                model.SelectedCombination = await _productAttributeMaterializer.FindAttributeCombinationAsync(product.Id, attributesSelection);

                if (model.SelectedCombination != null && model.SelectedCombination.IsActive == false)
                {
                    model.IsAvailable = false;
                    model.StockAvailability = res["Products.Availability.IsNotActive"];
                }

                // Required for below product.IsAvailableByStock().
                product.MergeWithCombination(model.SelectedCombination);

                // Explicitly selected values always discards values pre-selected by merchant.
                var selectedValueIds = selectedAttributeValues.Select(x => x.Id).ToArray();

                foreach (var attribute in model.ProductVariantAttributes)
                {
                    var updatePreSelection = selectedValueIds.Any() && selectedValueIds.Intersect(attribute.Values.Select(x => x.Id)).Any();

                    foreach (ProductDetailsModel.ProductVariantAttributeValueModel value in attribute.Values)
                    {
                        if (updatePreSelection)
                        {
                            value.IsPreSelected = selectedValueIds.Contains(value.Id);
                        }

                        if (!_catalogSettings.ShowVariantCombinationPriceAdjustment)
                        {
                            value.PriceAdjustment = string.Empty;
                        }

                        if (checkAvailability)
                        {
                            var availabilityInfo = await _productAttributeMaterializer.IsCombinationAvailableAsync(
                                product,
                                variantAttributes,
                                selectedAttributeValues,
                                value.ProductAttributeValue);

                            if (availabilityInfo != null)
                            {
                                value.IsUnavailable = true;

                                // Set title attribute for unavailable option.
                                if (product.DisplayStockAvailability && availabilityInfo.IsOutOfStock && availabilityInfo.IsActive)
                                {
                                    value.Title = product.BackorderMode == BackorderMode.NoBackorders || product.BackorderMode == BackorderMode.AllowQtyBelow0
                                        ? res["Products.Availability.OutOfStock"]
                                        : res["Products.Availability.Backordering"];
                                }
                                else
                                {
                                    value.Title = res["Products.Availability.IsNotActive"];
                                }
                            }
                        }
                    }
                }
            }

            return (selectedAttributeValues, hasSelectedAttributesValues);
        }

        protected async Task PrepareProductPropertiesModelAsync(
            ProductDetailsModel model, 
            ProductDetailsModelContext modelContext, 
            ICollection<ProductVariantAttributeValue> selectedAttributeValues,
            decimal preSelectedWeightAdjustment,
            bool hasSelectedAttributesValues)
        {
            var product = modelContext.Product;
            var productBundleItem = modelContext.ProductBundleItem;
            var currency = modelContext.Currency;
            var customer = modelContext.Customer;
            var store = modelContext.Store;
            var isBundle = product.ProductType == ProductType.BundledProduct;

            if ((productBundleItem != null && !productBundleItem.Item.BundleProduct.BundlePerItemShoppingCart) ||
                (product.ManageInventoryMethod == ManageInventoryMethod.ManageStockByAttributes && !hasSelectedAttributesValues))
            {
                // Cases where stock inventory is not functional (what ShoppingCartService.GetStandardWarnings and ProductService.AdjustInventory does not handle).
                model.IsAvailable = true;

                // TODO: (mh) (core) Test this!!
                //var hasAttributeCombinations = _services.DbContext.QueryForCollection(product, (Product p) => p.ProductVariantAttributeCombinations).Any();
                model.StockAvailability = !hasSelectedAttributesValues ? product.FormatStockMessage(_localizationService) : string.Empty;
            }
            else if (model.IsAvailable)
            {
                model.IsAvailable = product.IsAvailableByStock();
                model.StockAvailability = product.FormatStockMessage(_localizationService);
            }

            model.Id = product.Id;
            model.Name = product.GetLocalized(x => x.Name);
            model.Condition = product.Condition;
            model.ShowCondition = _catalogSettings.ShowProductCondition;
            model.LocalizedCondition = await product.Condition.GetLocalizedEnumAsync(_services.WorkContext.WorkingLanguage.Id);
            model.ShowSku = _catalogSettings.ShowProductSku;
            model.Sku = product.Sku;
            model.ShortDescription = product.GetLocalized(x => x.ShortDescription);
            model.FullDescription = product.GetLocalized(x => x.FullDescription, detectEmptyHtml: true);
            model.MetaKeywords = product.GetLocalized(x => x.MetaKeywords);
            model.MetaDescription = product.GetLocalized(x => x.MetaDescription);
            model.MetaTitle = product.GetLocalized(x => x.MetaTitle);
            model.SeName = await product.GetActiveSlugAsync();
            model.ShowManufacturerPartNumber = _catalogSettings.ShowManufacturerPartNumber;
            model.ManufacturerPartNumber = product.ManufacturerPartNumber;
            model.ShowDimensions = _catalogSettings.ShowDimensions;
            model.ShowWeight = _catalogSettings.ShowWeight;
            model.ShowGtin = _catalogSettings.ShowGtin;
            model.Gtin = product.Gtin;
            model.HasSampleDownload = product.IsDownload && product.HasSampleDownload;
            model.IsCurrentCustomerRegistered = customer.IsRegistered();
            model.IsBasePriceEnabled = product.BasePriceEnabled && !(isBundle && product.BundlePerItemPricing);
            model.BasePriceInfo = await _priceCalculationService.GetBasePriceInfoAsync(product, customer, currency);
            model.ShowLegalInfo = !model.IsBundlePart && _taxSettings.ShowLegalHintsInProductDetails;
            model.BundleTitleText = product.GetLocalized(x => x.BundleTitleText);
            model.BundlePerItemPricing = product.BundlePerItemPricing;
            model.BundlePerItemShipping = product.BundlePerItemShipping;
            model.BundlePerItemShoppingCart = product.BundlePerItemShoppingCart;

            var taxDisplayType = _services.WorkContext.GetTaxDisplayTypeFor(customer, store.Id);
            string taxInfo = T(taxDisplayType == TaxDisplayType.IncludingTax ? "Tax.InclVAT" : "Tax.ExclVAT");

            var defaultTaxRate = string.Empty;
            if (_taxSettings.DisplayTaxRates)
            {
                var taxRate = await _taxService.GetTaxRateAsync(product, customer: customer);
                if (taxRate != decimal.Zero)
                {
                    var formattedTaxRate = _taxService.FormatTaxRate(taxRate);
                    defaultTaxRate = $"({formattedTaxRate}%)";
                }
            }

            var additionalShippingCosts = string.Empty;
            var addShippingPrice = _currencyService.ConvertFromPrimaryCurrency(product.AdditionalShippingCharge, currency);

            if (addShippingPrice > 0)
            {
                // TODO: (mh) (core) Debug & Test!!!
                //additionalShippingCosts = T("Common.AdditionalShippingSurcharge").Text.FormatInvariant(_priceFormatter.FormatPrice(addShippingPrice, true, false)) + ", ";
                additionalShippingCosts = T("Common.AdditionalShippingSurcharge", _currencyService.ConvertToWorkingCurrency(addShippingPrice)) + ", ";
            }

            if (!product.IsShippingEnabled || (addShippingPrice == 0 && product.IsFreeShipping))
            {
                model.LegalInfo += "{0} {1}, {2}".FormatInvariant(
                    product.IsTaxExempt ? string.Empty : taxInfo,
                    product.IsTaxExempt ? string.Empty : defaultTaxRate,
                    T("Common.FreeShipping"));
            }
            else
            {
                var shippingInfoUrl = await _urlHelper.TopicAsync("ShippingInfo");

                if (shippingInfoUrl.IsEmpty())
                {
                    model.LegalInfo = T("Tax.LegalInfoProductDetail2",
                        product.IsTaxExempt ? string.Empty : taxInfo,
                        product.IsTaxExempt ? string.Empty : defaultTaxRate,
                        additionalShippingCosts);
                }
                else
                {
                    model.LegalInfo = T("Tax.LegalInfoProductDetail",
                        product.IsTaxExempt ? string.Empty : taxInfo,
                        product.IsTaxExempt ? string.Empty : defaultTaxRate,
                        additionalShippingCosts,
                        shippingInfoUrl);
                }
            }

            var dimension = await _db.MeasureDimensions.AsNoTracking().Where(x => x.Id == _measureSettings.BaseDimensionId).FirstOrDefaultAsync();
            var weight = await _db.MeasureWeights.AsNoTracking().Where(x => x.Id == _measureSettings.BaseDimensionId).FirstOrDefaultAsync();
            var dimensionSystemKeyword = dimension?.SystemKeyword ?? string.Empty;
            var weightSystemKeyword = dimension?.SystemKeyword ?? string.Empty;

            model.WeightValue = product.Weight;
            if (!isBundle)
            {
                if (selectedAttributeValues != null)
                {
                    foreach (var attributeValue in selectedAttributeValues)
                    {
                        model.WeightValue = decimal.Add(model.WeightValue, attributeValue.WeightAdjustment);
                    }
                }
                else
                {
                    model.WeightValue = decimal.Add(model.WeightValue, preSelectedWeightAdjustment);
                }
            }

            model.Weight = (model.WeightValue > 0) ? $"{model.WeightValue:N2} {weightSystemKeyword}" : string.Empty;
            model.Height = (product.Height > 0) ? $"{product.Height:N2} {dimensionSystemKeyword}" : string.Empty;
            model.Length = (product.Length > 0) ? $"{product.Length:N2} {dimensionSystemKeyword}" : string.Empty;
            model.Width = (product.Width > 0) ? $"{product.Width:N2} {dimensionSystemKeyword}" : string.Empty;

            if (productBundleItem != null)
            {
                model.ThumbDimensions = _mediaSettings.BundledProductPictureSize;
            }
            else if (modelContext.IsAssociatedProduct)
            {
                model.ThumbDimensions = _mediaSettings.AssociatedProductPictureSize;
            }

            // Delivery Time.
            var deliveryPresentation = _catalogSettings.DeliveryTimesInProductDetail;

            if (model.IsAvailable)
            {
                var deliveryTime = await _deliveryTimeService.GetDeliveryTimeAsync(product, _catalogSettings);
                if (deliveryTime != null)
                {
                    model.DeliveryTimeName = deliveryTime.GetLocalized(x => x.Name);
                    model.DeliveryTimeHexValue = deliveryTime.ColorHexValue;

                    if (deliveryPresentation == DeliveryTimesPresentation.DateOnly || deliveryPresentation == DeliveryTimesPresentation.LabelAndDate)
                    {
                        model.DeliveryTimeDate = _deliveryTimeService.GetFormattedDeliveryDate(deliveryTime);
                    }
                }
            }

            model.IsShippingEnabled = product.IsShippingEnabled;
            model.DeliveryTimesPresentation = deliveryPresentation;
            model.DisplayDeliveryTimeAccordingToStock = product.DisplayDeliveryTimeAccordingToStock(_catalogSettings);

            if (model.DeliveryTimeName.IsEmpty() && deliveryPresentation != DeliveryTimesPresentation.None)
            {
                model.DeliveryTimeName = T("ShoppingCart.NotAvailable");
            }

            var quantityUnit = await _db.QuantityUnits
                .AsNoTracking()
                .ApplyQuantityUnitFilter(product.QuantityUnitId)
                .FirstOrDefaultAsync();

            if (quantityUnit != null)
            {
                model.QuantityUnitName = quantityUnit.GetLocalized(x => x.Name);
            }

            // Back in stock subscriptions.
            if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock &&
                product.BackorderMode == BackorderMode.NoBackorders &&
                product.AllowBackInStockSubscriptions &&
                product.StockQuantity <= 0)
            {
                // Out of stock.
                model.DisplayBackInStockSubscription = true;
                model.BackInStockAlreadySubscribed = await _stockSubscriptionService.IsSubscribedAsync(product, customer, store.Id);
            }
        }

        // TODO: (mh) (core) Reimplement when PricingChain is ready.
        protected async Task PrepareProductPriceModelAsync(ProductDetailsModel model, ProductDetailsModelContext modelContext)
        {
            var product = modelContext.Product;
            var productBundleItem = modelContext.ProductBundleItem;
            var displayPrices = await _services.Permissions.AuthorizeAsync(Permissions.Catalog.DisplayPrice);

            model.ProductPrice.ProductId = product.Id;
            model.ProductPrice.HidePrices = !displayPrices;
            model.ProductPrice.ShowLoginNote = !displayPrices && productBundleItem == null && _catalogSettings.ShowLoginForPriceNote;

            //if (displayPrices)
            //{
            //    if (product.CustomerEntersPrice && !isBundleItemPricing)
            //    {
            //        model.ProductPrice.CustomerEntersPrice = true;
            //    }
            //    else
            //    {
            //        if (product.CallForPrice && !isBundleItemPricing)
            //        {
            //            model.ProductPrice.CallForPrice = true;
            //            model.HotlineTelephoneNumber = _contactDataSettings.HotlineTelephoneNumber.NullEmpty();
            //        }
            //        else
            //        {
            //            var taxRate = decimal.Zero;
            //            var oldPrice = decimal.Zero;
            //            var finalPriceWithoutDiscountBase = decimal.Zero;
            //            var finalPriceWithDiscountBase = decimal.Zero;
            //            var attributesTotalPriceBase = new Money();
            //            var attributesTotalPriceBaseOrig = decimal.Zero;
            //            var finalPriceWithoutDiscount = decimal.Zero;
            //            var finalPriceWithDiscount = decimal.Zero;
            //            //var oldPriceBase = await _taxService.GetProductPriceAsync(product, product.OldPrice, out taxRate);

            //            if (model.ProductPrice.DynamicPriceUpdate && !isBundlePricing)
            //            {
            //                if (selectedAttributeValues != null)
            //                {

            //                    //selectedAttributeValues.Each(x => attributesTotalPriceBase += await _priceCalculationService.GetProductVariantAttributeValuePriceAdjustmentAsync(x,
            //                    //    product, selectedQuantity));

            //                    //selectedAttributeValues.Each(x => attributesTotalPriceBaseOrig += _priceCalculationService.GetProductVariantAttributeValuePriceAdjustment(x,
            //                    //    product, customer, null, 1));
            //                }
            //                else
            //                {
            //                    attributesTotalPriceBase = preSelectedPriceAdjustmentBase;
            //                }
            //            }

            //            if (productBundleItem != null)
            //            {
            //                productBundleItem.AdditionalCharge = attributesTotalPriceBase;
            //            }

            //            finalPriceWithoutDiscountBase = await _priceCalculationService.GetFinalPriceAsync(product, productBundleItems,
            //                customer, attributesTotalPriceBaseOrig, false, selectedQuantity, productBundleItem);

            //            finalPriceWithDiscountBase = _priceCalculationService.GetFinalPrice(product, productBundleItems,
            //                customer, attributesTotalPriceBase, true, selectedQuantity, productBundleItem);

            //            var basePriceAdjustment = finalPriceWithDiscountBase - finalPriceWithoutDiscountBase;

            //            finalPriceWithoutDiscountBase = _taxService.GetProductPrice(product, finalPriceWithoutDiscountBase, out taxRate);
            //            finalPriceWithDiscountBase = _taxService.GetProductPrice(product, finalPriceWithDiscountBase, out taxRate);

            //            oldPrice = _currencyService.ConvertFromPrimaryStoreCurrency(oldPriceBase, currency);

            //            finalPriceWithoutDiscount = _currencyService.ConvertFromPrimaryStoreCurrency(finalPriceWithoutDiscountBase, currency);
            //            finalPriceWithDiscount = _currencyService.ConvertFromPrimaryStoreCurrency(finalPriceWithDiscountBase, currency);

            //            if (productBundleItem == null || isBundleItemPricing)
            //            {
            //                if (oldPriceBase > decimal.Zero && oldPriceBase > finalPriceWithoutDiscountBase)
            //                {
            //                    model.ProductPrice.OldPriceValue = oldPrice;
            //                    model.ProductPrice.OldPrice = _priceFormatter.FormatPrice(oldPrice);
            //                }

            //                model.ProductPrice.Price = _priceFormatter.FormatPrice(finalPriceWithoutDiscount);

            //                if (finalPriceWithoutDiscountBase != finalPriceWithDiscountBase)
            //                {
            //                    model.ProductPrice.PriceWithDiscount = _priceFormatter.FormatPrice(finalPriceWithDiscount);
            //                }
            //            }

            //            if (product.SpecialPriceEndDateTimeUtc.HasValue && product.SpecialPriceEndDateTimeUtc > DateTime.UtcNow)
            //                model.ProductPrice.PriceValidUntilUtc = product.SpecialPriceEndDateTimeUtc.Value.ToString("u");

            //            model.ProductPrice.PriceValue = finalPriceWithoutDiscount;
            //            model.ProductPrice.PriceWithDiscountValue = finalPriceWithDiscount;
            //            model.BasePriceInfo = product.GetBasePriceInfo(
            //                _localizationService,
            //                _priceFormatter,
            //                _currencyService,
            //                _taxService,
            //                _priceCalculationService,
            //                customer,
            //                currency,
            //                attributesTotalPriceBase);

            //            if (!string.IsNullOrWhiteSpace(model.ProductPrice.OldPrice) || !string.IsNullOrWhiteSpace(model.ProductPrice.PriceWithDiscount))
            //            {
            //                model.ProductPrice.NoteWithoutDiscount = T(isBundle && product.BundlePerItemPricing ? "Products.Bundle.PriceWithoutDiscount.Note" : "Products.Price");
            //            }

            //            if ((isBundle && product.BundlePerItemPricing && !string.IsNullOrWhiteSpace(model.ProductPrice.PriceWithDiscount)) || product.HasTierPrices)
            //            {
            //                if (!product.HasTierPrices)
            //                {
            //                    model.ProductPrice.NoteWithDiscount = T("Products.Bundle.PriceWithDiscount.Note");
            //                }

            //                model.BasePriceInfo = product.GetBasePriceInfo(
            //                    _localizationService,
            //                    _priceFormatter,
            //                    _currencyService,
            //                    _taxService,
            //                    _priceCalculationService,
            //                    customer,
            //                    currency,
            //                    basePriceAdjustment);
            //            }

            //            // Calculate saving.
            //            // Discounted price has priority over the old price (avoids differing percentage discount in product lists and detail page).
            //            //var regularPrice = Math.Max(finalPriceWithoutDiscount, oldPrice);
            //            var regularPrice = finalPriceWithDiscount < finalPriceWithoutDiscount
            //                ? finalPriceWithoutDiscount
            //                : oldPrice;

            //            if (regularPrice > 0 && regularPrice > finalPriceWithDiscount)
            //            {
            //                model.ProductPrice.SavingPercent = (float)((regularPrice - finalPriceWithDiscount) / regularPrice) * 100;
            //                model.ProductPrice.SavingAmount = _priceFormatter.FormatPrice(regularPrice - finalPriceWithDiscount, true, false);
            //            }
            //        }
            //    }
            //}
            //else
            //{
            //    model.ProductPrice.OldPrice = null;
            //    model.ProductPrice.Price = null;
            //}

        }

        protected async Task PrepareProductCartModelAsync(ProductDetailsModel model, ProductDetailsModelContext modelContext, int selectedQuantity)
        {
            var product = modelContext.Product;
            var productBundleItem = modelContext.ProductBundleItem;
            var customer = modelContext.Customer;
            var currency = modelContext.Currency;
            var displayPrices = await _services.Permissions.AuthorizeAsync(Permissions.Catalog.DisplayPrice);

            model.AddToCart.ProductId = product.Id;
            model.AddToCart.EnteredQuantity = product.OrderMinimumQuantity > selectedQuantity ? product.OrderMinimumQuantity : selectedQuantity;
            model.AddToCart.MinOrderAmount = product.OrderMinimumQuantity;
            model.AddToCart.MaxOrderAmount = product.OrderMaximumQuantity;
            model.AddToCart.QuantityUnitName = model.QuantityUnitName; // TODO: (mc) remove 'QuantityUnitName' from parent model later
            model.AddToCart.QuantityStep = product.QuantityStep > 0 ? product.QuantityStep : 1;
            model.AddToCart.HideQuantityControl = product.HideQuantityControl;
            model.AddToCart.QuantiyControlType = product.QuantiyControlType;
            model.AddToCart.AvailableForPreOrder = product.AvailableForPreOrder;

            // 'add to cart', 'add to wishlist' buttons.
            model.AddToCart.DisableBuyButton = !displayPrices || product.DisableBuyButton ||
                !_services.Permissions.Authorize(Permissions.Cart.AccessShoppingCart);

            model.AddToCart.DisableWishlistButton = !displayPrices || product.DisableWishlistButton
                || product.ProductType == ProductType.GroupedProduct
                || !_services.Permissions.Authorize(Permissions.Cart.AccessWishlist);

            model.AddToCart.CustomerEntersPrice = product.CustomerEntersPrice;
            if (model.AddToCart.CustomerEntersPrice)
            {
                var minimumCustomerEnteredPrice = _currencyService.ConvertFromPrimaryCurrency(product.MinimumCustomerEnteredPrice, currency);
                var maximumCustomerEnteredPrice = _currencyService.ConvertFromPrimaryCurrency(product.MaximumCustomerEnteredPrice, currency);

                // TODO: (mh) (core) CustomerEnteredPrice must also be Money?
                model.AddToCart.CustomerEnteredPrice = minimumCustomerEnteredPrice.Amount;

                model.AddToCart.CustomerEnteredPriceRange = T("Products.EnterProductPrice.Range",
                    _currencyService.ConvertToWorkingCurrency(minimumCustomerEnteredPrice),
                    _currencyService.ConvertToWorkingCurrency(maximumCustomerEnteredPrice));
            }

            // TODO: (mh) (core) Create Product extension or mayby helper method to get AllowedQuatities.
            //var allowedQuantities = product.ParseAllowedQuatities();
            //foreach (var qty in allowedQuantities)
            //{
            //    model.AddToCart.AllowedQuantities.Add(new SelectListItem
            //    {
            //        Text = qty.ToString(),
            //        Value = qty.ToString()
            //    });
            //}
        }

        protected void PrepareProductGiftCardsModel(ProductDetailsModel model, ProductDetailsModelContext modelContext)
        {
            model.GiftCard.IsGiftCard = modelContext.Product.IsGiftCard;
            if (model.GiftCard.IsGiftCard)
            {
                model.GiftCard.GiftCardType = modelContext.Product.GiftCardType;
                model.GiftCard.SenderName = modelContext.Customer.GetFullName();
                model.GiftCard.SenderEmail = modelContext.Customer.Email;
            }
        }

        protected async Task PrepareProductTagsModelAsync(ProductDetailsModel model, Product product)
        {
            var storeId = _services.StoreContext.CurrentStore.Id;
            var cacheKey = string.Format(ModelCacheInvalidator.PRODUCTTAG_BY_PRODUCT_MODEL_KEY, product.Id, _services.WorkContext.WorkingLanguage.Id, storeId);
            var cacheModel = await _services.CacheFactory.GetMemoryCache().GetAsync(cacheKey, async () =>
            {
                var productTags = await product.ProductTags
                    .WhereAsync(async x => x.Published && (await _productTagService.CountProductsByTagIdAsync(x.Id, storeId: storeId)) > 0)
                    .AsyncToList();

                var tagModel = await productTags.SelectAsync(async x =>
                    {
                        return new ProductTagModel
                        {
                            Id = x.Id,
                            Name = x.GetLocalized(y => y.Name),
                            Slug = x.BuildSlug(),
                            ProductCount = await _productTagService.CountProductsByTagIdAsync(x.Id, storeId: storeId)
                        };
                    })
                    .AsyncToList();

                return tagModel;
            });

            model.ProductTags = cacheModel;
        }

        protected async Task PrepareRelatedProductsModelAsync(ProductDetailsModel model, Product product)
        {
            var relatedProductIds = await _db.RelatedProducts
                .AsNoTracking()
                .Where(x => x.ProductId1 == product.Id)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => x.ProductId2)
                .ToArrayAsync();

            var products = await _db.Products
                .ApplyAclFilter(_services.WorkContext.CurrentCustomer)
                .ApplyStoreFilter(_services.StoreContext.CurrentStore.Id)
                .Where(x => relatedProductIds.Contains(x.Id))
                .ToListAsync();

            products = products.OrderBySequence(relatedProductIds).ToList();

            var settings = GetBestFitProductSummaryMappingSettings(ProductSummaryViewMode.Grid, x =>
            {
                x.DeliveryTimesPresentation = DeliveryTimesPresentation.None;
            });

            model.RelatedProducts = await MapProductSummaryModelAsync(products, settings);
            model.RelatedProducts.ShowBasePrice = false;
        }

        protected async Task PrepareAlsoPurchasedProductsModelAsync(ProductDetailsModel model, Product product)
        {
            if (!_catalogSettings.ProductsAlsoPurchasedEnabled)
            {
                return;
            }

            var storeId = _services.StoreContext.CurrentStore.Id;

            var alsoPurchasedProductIds = await _services.Cache.GetAsync(string.Format(ModelCacheInvalidator.PRODUCTS_ALSO_PURCHASED_IDS_KEY, product.Id, storeId), async () =>
            {
                return await _db.OrderItems
                    .AsNoTracking()
                    .SelectAlsoPurchasedProductIds(product.Id, _catalogSettings.ProductsAlsoPurchasedNumber, storeId)
                    .ToArrayAsync();
            });

            var products = await _db.Products
                .ApplyAclFilter(_services.WorkContext.CurrentCustomer)
                .ApplyStoreFilter(storeId)
                .Where(x => alsoPurchasedProductIds.Contains(x.Id))
                .ToListAsync();

            products = products.OrderBySequence(alsoPurchasedProductIds).ToList();

            var settings = GetBestFitProductSummaryMappingSettings(ProductSummaryViewMode.Mini);
            model.AlsoPurchased = await MapProductSummaryModelAsync(products, settings);
        }

        #endregion

        public MediaGalleryModel PrepareProductDetailsMediaGalleryModel(
            IList<MediaFileInfo> files,
            string productName,
            ICollection<int> allCombinationImageIds,
            bool isAssociatedProduct,
            ProductBundleItemData bundleItem = null,
            ProductVariantAttributeCombination combination = null)
        {
            var model = new MediaGalleryModel
            {
                ModelName = productName,
                DefaultAlt = T("Media.Product.ImageAlternateTextFormat", productName),
                BoxEnabled = true, // TODO: make a setting for this in the future
                ImageZoomEnabled = _mediaSettings.DefaultPictureZoomEnabled,
                ImageZoomType = _mediaSettings.PictureZoomType,
                ThumbSize = _mediaSettings.ProductThumbPictureSizeOnProductDetailsPage,
                ImageSize = _mediaSettings.ProductDetailsPictureSize
            };

            if (isAssociatedProduct)
            {
                model.ThumbSize = _mediaSettings.AssociatedProductPictureSize;
            }
            else if (bundleItem != null)
            {
                model.ThumbSize = _mediaSettings.BundledProductPictureSize;
            }

            MediaFileInfo defaultFile = null;
            var combiAssignedImages = combination?.GetAssignedMediaIds();

            if (files.Count > 0)
            {
                if (files.Count <= _catalogSettings.DisplayAllImagesNumber)
                {
                    // Show all images.
                    foreach (var file in files)
                    {
                        model.Files.Add(PrepareMediaFileInfo(file, model));

                        if (defaultFile == null && combiAssignedImages != null && combiAssignedImages.Contains(file.Id))
                        {
                            model.GalleryStartIndex = model.Files.Count - 1;
                            defaultFile = file;
                        }
                    }
                }
                else
                {
                    // Images not belonging to any combination...
                    allCombinationImageIds ??= new List<int>();
                    foreach (var file in files.Where(p => !allCombinationImageIds.Contains(p.Id)))
                    {
                        model.Files.Add(PrepareMediaFileInfo(file, model));
                    }

                    // Plus images belonging to selected combination.
                    if (combiAssignedImages != null)
                    {
                        foreach (var file in files.Where(p => combiAssignedImages.Contains(p.Id)))
                        {
                            model.Files.Add(PrepareMediaFileInfo(file, model));

                            if (defaultFile == null)
                            {
                                model.GalleryStartIndex = model.Files.Count - 1;
                                defaultFile = file;
                            }
                        }
                    }
                }

                if (defaultFile == null)
                {
                    model.GalleryStartIndex = 0;
                    defaultFile = files.First();
                }
            }

            if (defaultFile == null && !_catalogSettings.HideProductDefaultPictures)
            {
                var fallbackImageSize = _mediaSettings.ProductDetailsPictureSize;
                if (isAssociatedProduct)
                {
                    fallbackImageSize = _mediaSettings.AssociatedProductPictureSize;
                }
                else if (bundleItem != null)
                {
                    fallbackImageSize = _mediaSettings.BundledProductPictureSize;
                }

                model.FallbackUrl = _mediaService.GetFallbackUrl(fallbackImageSize);
            }

            return model;
        }

        protected async Task<List<ProductSpecificationModel>> PrepareProductSpecificationModelAsync(ProductDetailsModelContext modelContext)
        {
            Guard.NotNull(modelContext, nameof(modelContext));

            var product = modelContext.Product;
            var batchContext = modelContext.BatchContext;

            string cacheKey = string.Format(ModelCacheInvalidator.PRODUCT_SPECS_MODEL_KEY, product.Id, _services.WorkContext.WorkingLanguage.Id);
            return await _services.CacheFactory.GetMemoryCache().GetAsync(cacheKey, async () =>
            {
                List<ProductSpecificationAttribute> attrs;
                
                if (_db.IsCollectionLoaded(product, x => x.ProductSpecificationAttributes))
                {
                    attrs = product.ProductSpecificationAttributes
                        .Where(x =>
                            (x.ShowOnProductPage == null && x.SpecificationAttributeOption?.SpecificationAttribute?.ShowOnProductPage == true) ||
                            (x.ShowOnProductPage == true))
                        .OrderBy(x => x.DisplayOrder)
                        .ToList();
                }
                else
                {
                    attrs = (await batchContext.SpecificationAttributes.GetOrLoadAsync(product.Id))
                        .Where(x =>
                            (x.ShowOnProductPage == null && x.SpecificationAttributeOption?.SpecificationAttribute?.ShowOnProductPage == true) ||
                            (x.ShowOnProductPage == true))
                        .ToList();
                }

                return attrs
                    .Select(x => new ProductSpecificationModel
                    {
                        SpecificationAttributeId = x.SpecificationAttributeOption.SpecificationAttributeId,
                        SpecificationAttributeName = x.SpecificationAttributeOption.SpecificationAttribute.GetLocalized(x => x.Name),
                        SpecificationAttributeOption = x.SpecificationAttributeOption.GetLocalized(x => x.Name)
                    })
                    .ToList();
            });
        }

        public async Task<List<ProductDetailsModel.TierPriceModel>> CreateTierPriceModelAsync(Product product, decimal adjustment = decimal.Zero)
        {
            var model = await product.TierPrices
                .FilterByStore(_services.StoreContext.CurrentStore.Id)
                .FilterForCustomer(_services.WorkContext.CurrentCustomer)
                .ToList()
                .RemoveDuplicatedQuantities()
                .SelectAsync(async (tierPrice) =>
                {
                    var m = new ProductDetailsModel.TierPriceModel
                    {
                        Quantity = tierPrice.Quantity,
                    };

                    if (adjustment != 0 && tierPrice.CalculationMethod == TierPriceCalculationMethod.Percental && _catalogSettings.ApplyTierPricePercentageToAttributePriceAdjustments)
                    {
                        adjustment -= (adjustment / 100 * tierPrice.Price);
                    }
                    else
                    {
                        adjustment = decimal.Zero;
                    }
                    
                    var adjustmentAmount = adjustment == 0 ? (Money?)null : new Money(adjustment, _currencyService.PrimaryCurrency);
                    var priceBase = default(Money);
                    var taxRate = decimal.Zero;
                    var finalPriceBase = await _priceCalculationService.GetFinalPriceAsync(product,
                        adjustmentAmount, 
                        _services.WorkContext.CurrentCustomer, 
                        true, 
                        tierPrice.Quantity, null, null, true);

                    (priceBase, taxRate) = await _taxService.GetProductPriceAsync(product, finalPriceBase);
                    m.Price = _currencyService.ConvertToWorkingCurrency(priceBase);

                    return m;
                })
                .AsyncToList();

            return model;
        }

        public async Task PrepareProductReviewsModelAsync(ProductReviewsModel model, Product product, int? take = null)
        {
            Guard.NotNull(product, nameof(product));
            Guard.NotNull(model, nameof(model));

            model.ProductId = product.Id;
            model.ProductName = product.GetLocalized(x => x.Name);
            model.ProductSeName = await product.GetActiveSlugAsync();

            var collectionLoaded = _db.IsCollectionLoaded(product, x => x.ProductReviews);

            if (!collectionLoaded)
            {
                _db.Attach(product);
            }

            // We need the query for total count resolution.
            var query = _db.Entry(product)
                .Collection(x => x.ProductReviews)
                .Query()
                .Where(x => x.IsApproved);

            if (collectionLoaded)
            {
                model.TotalReviewsCount = product.ProductReviews.Count;
            }
            else
            {
                model.TotalReviewsCount = await query.CountAsync();
            }

            if (model.TotalReviewsCount > 0)
            {
                if (take.HasValue)
                {
                    query = query.Take(take.Value);
                }
                
                var reviews = collectionLoaded ? product.ProductReviews.Take(take ?? int.MaxValue).ToList() : await query
                    .OrderByDescending(y => y.CreatedOnUtc)
                    .Include(x => x.Customer)
                    .ToListAsync();

                foreach (var review in reviews)
                {
                    model.Items.Add(new ProductReviewModel
                    {
                        Id = review.Id,
                        CustomerId = review.CustomerId,
                        CustomerName = review.Customer.FormatUserName(),
                        AllowViewingProfiles = _customerSettings.AllowViewingProfiles && review.Customer != null && !review.Customer.IsGuest(),
                        Title = review.Title,
                        ReviewText = review.ReviewText,
                        Rating = review.Rating,
                        Helpfulness = new ProductReviewHelpfulnessModel
                        {
                            ProductReviewId = review.Id,
                            HelpfulYesTotal = review.HelpfulYesTotal,
                            HelpfulNoTotal = review.HelpfulNoTotal,
                        },
                        WrittenOnStr = _dateTimeHelper.ConvertToUserTime(review.CreatedOnUtc, DateTimeKind.Utc).ToString("D"),
                        WrittenOn = review.CreatedOnUtc
                    });
                }
            }

            model.CanCurrentCustomerLeaveReview = _catalogSettings.AllowAnonymousUsersToReviewProduct || !_services.WorkContext.CurrentCustomer.IsGuest();
            model.DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnProductReviewPage;
        }

        private MediaFileInfo PrepareMediaFileInfo(MediaFileInfo file, MediaGalleryModel model)
        {
            file.Alt = file.File.GetLocalized(x => x.Alt)?.Value.NullEmpty() ?? model.DefaultAlt;
            file.TitleAttribute = file.File.GetLocalized(x => x.Title)?.Value.NullEmpty() ?? model.ModelName;

            _services.DisplayControl.Announce(file.File);

            // Return for chaining
            return file;
        }

        #endregion

        #region Metaproperties

        public MetaPropertiesModel PrepareMetaPropertiesProduct(ProductDetailsModel product)
        {
            var model = new MetaPropertiesModel
            {
                Url = _urlHelper.RouteUrl("Product", new { SeName = product.SeName }, _httpRequest.Scheme),
                Title = product.Name.Value,
                Type = "product"
            };

            var shortDescription = product.ShortDescription.Value.HasValue() ? product.ShortDescription : product.MetaDescription;
            if (shortDescription.Value.HasValue())
            {
                model.Description = shortDescription.Value;
            }

            var fileInfo = product.MediaGalleryModel.Files?.ElementAtOrDefault(product.MediaGalleryModel.GalleryStartIndex);

            PrepareMetaPropertiesModel(model, fileInfo);

            return model;
        }

        public MetaPropertiesModel PrepareMetaPropertiesCategory(CategoryModel category)
        {
            var model = new MetaPropertiesModel
            {
                Url = _urlHelper.RouteUrl("Category", new { category.SeName }, _httpRequest.Scheme),
                Title = category.Name.Value,
                Description = category.MetaDescription?.Value,
                Type = "product"
            };

            var fileInfo = category.Image?.File;
            PrepareMetaPropertiesModel(model, fileInfo);

            return model;
        }

        public MetaPropertiesModel PrepareMetaPropertiesBrand(BrandModel brand)
        {
            var model = new MetaPropertiesModel
            {
                Url = _urlHelper.RouteUrl("Manufacturer", new { brand.SeName }, _httpRequest.Scheme),
                Title = brand.Name.Value,
                Description = brand.MetaDescription?.Value,
                Type = "product"
            };

            var fileInfo = brand.Image?.File;
            PrepareMetaPropertiesModel(model, fileInfo);

            return model;
        }

        public void PrepareMetaPropertiesModel(MetaPropertiesModel model, MediaFileInfo fileInfo)
        {
            model.Site = _urlHelper.RouteUrl("HomePage", null, _httpRequest.Scheme);
            model.SiteName = _storeContext.CurrentStore.Name;

            var imageUrl = fileInfo?.GetUrl();
            if (fileInfo != null && imageUrl.HasValue())
            {
                imageUrl = WebHelper.GetAbsoluteUrl(imageUrl, _httpRequest, true);
                model.ImageUrl = imageUrl;
                model.ImageType = fileInfo.MimeType;

                if (fileInfo.Alt.HasValue())
                {
                    model.ImageAlt = fileInfo.Alt;
                }

                if (fileInfo.Size.Width > 0 && fileInfo.Size.Height > 0)
                {
                    model.ImageWidth = fileInfo.Size.Width;
                    model.ImageHeight = fileInfo.Size.Height;
                }
            }

            model.TwitterSite = _socialSettings.TwitterSite;
            model.FacebookAppId = _socialSettings.FacebookAppId;
        }

        #endregion

        // INFO: (mh) (core) Ported CatalogSearchQuery.GetViewMode() like this because there was no proper place for the old extension method.
        // TODO: (mh) (core) RE: This method does not belong here. Find another place please.
        public ProductSummaryViewMode GetSearchQueryViewMode(CatalogSearchQuery query)
        {
            Guard.NotNull(query, nameof(query));

            if (query.CustomData.Get("ViewMode") is string viewMode && viewMode.EqualsNoCase("list"))
            {
                return ProductSummaryViewMode.List;
            }

            return ProductSummaryViewMode.Grid;
        }
    }
}
