using Dasync.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Smartstore.Caching;
using Smartstore.Collections;
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
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Diagnostics;
using Smartstore.Web.Infrastructure.Hooks;
using Smartstore.Web.Models.Catalog;
using Smartstore.Web.Models.Catalog.Mappers;
using Smartstore.Web.Models.Media;

namespace Smartstore.Web.Controllers
{
    public partial class CatalogHelper
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IWorkContext _workContext;
        private readonly ICacheManager _cache;
        private readonly IMenuService _menuService;
        private readonly IProductService _productService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly ITaxService _taxService;
        private readonly ITaxCalculator _taxCalculator;
        private readonly ICurrencyService _currencyService;
        private readonly IMediaService _mediaService;
        private readonly ILocalizationService _localizationService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IStockSubscriptionService _stockSubscriptionService;
        private readonly MediaSettings _mediaSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly CaptchaSettings _captchaSettings;
        private readonly TaxSettings _taxSettings;
        private readonly PerformanceSettings _performanceSettings;
        private readonly MeasureSettings _measureSettings;
        private readonly IDeliveryTimeService _deliveryTimeService;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly ICatalogSearchQueryFactory _catalogSearchQueryFactory;
        private readonly HttpRequest _httpRequest;
        private readonly IUrlHelper _urlHelper;
        private readonly ProductUrlHelper _productUrlHelper;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IUrlService _urlService;
        private readonly ILinkResolver _linkResolver;
        private readonly ContactDataSettings _contactDataSettings;
        private readonly IProductTagService _productTagService;        

        public CatalogHelper(
            SmartDbContext db,
            ICommonServices services,
            IMenuService menuService,
            IProductService productService,
            IProductAttributeService productAttributeService,
            IProductAttributeMaterializer productAttributeMaterializer,
            IProductAttributeFormatter productAttributeFormatter,
            ITaxService taxService,
            ITaxCalculator taxCalculator,
            ICurrencyService currencyService,
            IMediaService mediaService,
            IPriceCalculationService priceCalculationService,
            IStockSubscriptionService stockSubscriptionService,
            MediaSettings mediaSettings,
            CatalogSettings catalogSettings,
            CustomerSettings customerSettings,
            CaptchaSettings captchaSettings,
            MeasureSettings measureSettings,
            TaxSettings taxSettings,
            PerformanceSettings performanceSettings,
            IDeliveryTimeService deliveryTimeService,
            ICatalogSearchService catalogSearchService,
            ICatalogSearchQueryFactory catalogSearchQueryFactory,
            IUrlHelper urlHelper,
            ProductUrlHelper productUrlHelper,
            ILocalizedEntityService localizedEntityService,
            IUrlService urlService,
            ILinkResolver linkResolver,
            ContactDataSettings contactDataSettings,
            IProductTagService productTagService)
        {
            _db = db;
            _services = services;
            _workContext = services.WorkContext;
            _cache = services.Cache;
            _menuService = menuService;
            _productService = productService;
            _productAttributeService = productAttributeService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _productAttributeFormatter = productAttributeFormatter;
            _taxService = taxService;
            _taxCalculator = taxCalculator;
            _currencyService = currencyService;
            _mediaService = mediaService;
            _localizationService = _services.Localization;
            _priceCalculationService = priceCalculationService;
            _stockSubscriptionService = stockSubscriptionService;
            _measureSettings = measureSettings;
            _taxSettings = taxSettings;
            _performanceSettings = performanceSettings;
            _deliveryTimeService = deliveryTimeService;
            _mediaSettings = mediaSettings;
            _catalogSettings = catalogSettings;
            _customerSettings = customerSettings;
            _captchaSettings = captchaSettings;
            _catalogSearchService = catalogSearchService;
            _catalogSearchQueryFactory = catalogSearchQueryFactory;
            _urlHelper = urlHelper;
            _productUrlHelper = productUrlHelper;
            _localizedEntityService = localizedEntityService;
            _urlService = urlService;
            _linkResolver = linkResolver;
            _httpRequest = _urlHelper.ActionContext.HttpContext.Request;
            _contactDataSettings = contactDataSettings;
            _productTagService = productTagService;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
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

            model.MetaProperties = await model.MapMetaPropertiesAsync();
            
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
                Title = file?.File?.GetLocalized(x => x.Title)?.Value.NullEmpty() ?? T("Media.Manufacturer.ImageLinkTitleFormat", localizedName),
                Alt = file?.File?.GetLocalized(x => x.Alt)?.Value.NullEmpty() ?? T("Media.Manufacturer.ImageAlternateTextFormat", localizedName),
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

            model.MetaProperties = await model.MapMetaPropertiesAsync();

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

        public async Task<ProductDetailsModel> MapProductDetailsPageModelAsync(Product product, ProductVariantQuery query)
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
                BatchContext = _productService.CreateProductBatchContext(new[] { product }, store, customer, false),
                DisplayPrices = await _services.Permissions.AuthorizeAsync(Permissions.Catalog.DisplayPrice)
            };

            return await MapProductDetailsPageModelAsync(modelContext);
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
                    var bundleItems = await batchContext.ProductBundleItems.GetOrLoadAsync(product.Id);
                    bundleItems = bundleItems.Where(x => x.Product.CanBeBundleItem()).ToList();

                    // Push Ids of bundle items to batch context to save roundtrips
                    batchContext.Collect(bundleItems.Select(x => x.ProductId).ToArray());

                    foreach (var bundleItem in bundleItems)
                    {
                        var childModelContext = new ProductDetailsModelContext(modelContext)
                        {
                            Product = bundleItem.Product,
                            IsAssociatedProduct = false,
                            ProductBundleItem = bundleItem
                        };

                        var bundledProductModel = await MapProductDetailsPageModelAsync(childModelContext);

                        bundledProductModel.ShowLegalInfo = false;
                        bundledProductModel.DeliveryTimesPresentation = DeliveryTimesPresentation.None;

                        bundledProductModel.BundleItem.Id = bundleItem.Id;
                        bundledProductModel.BundleItem.Quantity = bundleItem.Quantity;
                        bundledProductModel.BundleItem.HideThumbnail = bundleItem.HideThumbnail;
                        bundledProductModel.BundleItem.Visible = bundleItem.Visible;
                        bundledProductModel.BundleItem.IsBundleItemPricing = bundleItem.BundleProduct.BundlePerItemPricing;

                        var bundleItemName = bundleItem.GetLocalized(x => x.Name);
                        if (bundleItemName.Value.HasValue())
                        {
                            bundledProductModel.Name = bundleItemName;
                        }
                                
                        var bundleItemShortDescription = bundleItem.GetLocalized(x => x.ShortDescription);
                        if (bundleItemShortDescription.Value.HasValue())
                        {
                            bundledProductModel.ShortDescription = bundleItemShortDescription;
                        }

                        model.BundledItems.Add(bundledProductModel);
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

            model.WeightValue = product.Weight;
            model.IsBundlePart = product.ProductType != ProductType.BundledProduct && modelContext.ProductBundleItem != null;
            model.ProductPrice.BundleItemShowBasePrice = _catalogSettings.BundleItemShowBasePrice;

            // Attributes and attribute combination.
            await PrepareProductAttributesModelAsync(model, modelContext, selectedQuantity);

            // General properties.
            await PrepareProductPropertiesModelAsync(model, modelContext);

            // Price.
            await PrepareProductPriceModelAsync(model, modelContext, selectedQuantity);

            // AddToCart
            PrepareProductCartModel(model, modelContext, selectedQuantity);

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

        protected internal async Task PrepareProductAttributesModelAsync(ProductDetailsModel model, ProductDetailsModelContext modelContext, int selectedQuantity)
        {
            var product = modelContext.Product;

            if (product.ProductType == ProductType.BundledProduct)
            {
                // Bundles don't have attributes.
                return;
            }

            var query = modelContext.VariantQuery;
            var productBundleItem = modelContext.ProductBundleItem;
            var bundleItemId = productBundleItem?.Id ?? 0;
            var isBundlePricing = productBundleItem != null && !productBundleItem.BundleProduct.BundlePerItemPricing;
            var attributes = await modelContext.BatchContext.Attributes.GetOrLoadAsync(product.Id);
            var pricingOptions = _priceCalculationService.CreateDefaultOptions(false, modelContext.Customer, null, modelContext.BatchContext);
            var linkedProducts = new Dictionary<int, Product>();
            var linkedMediaFiles = new Multimap<int, ProductMediaFile>();
            var preselectedWeightAdjustment = 0m;

            // Key: ProductVariantAttributeValue.Id, value: attribute price adjustment.
            var priceAdjustments = modelContext.DisplayPrices && !isBundlePricing
                ? await _priceCalculationService.CalculateAttributePriceAdjustmentsAsync(product, null, selectedQuantity, pricingOptions)
                : new Dictionary<int, CalculatedPriceAdjustment>();

            var linkedProductIds = attributes
                .SelectMany(x => x.ProductVariantAttributeValues)
                .Where(x => x.ValueType == ProductVariantAttributeValueType.ProductLinkage && x.LinkedProductId != 0)
                .Select(x => x.LinkedProductId)
                .Distinct()
                .ToArray();

            if (linkedProductIds.Any())
            {
                linkedProducts = await _db.Products
                    .AsNoTracking()
                    .Where(x => linkedProductIds.Contains(x.Id) && x.Visibility != ProductVisibility.Hidden)
                    .ToDictionaryAsync(x => x.Id);

                if (_catalogSettings.ShowLinkedAttributeValueImage)
                {
                    linkedMediaFiles = (await _db.ProductMediaFiles
                        .AsNoTracking()
                        .Include(x => x.MediaFile)
                        .Where(x => linkedProductIds.Contains(x.ProductId))
                        .OrderBy(x => x.ProductId)
                        .ThenBy(x => x.DisplayOrder)
                        .ToListAsync())
                        .ToMultimap(x => x.ProductId, x => x);
                }
            }


            foreach (var attribute in attributes)
            {
                var preSelectedValueId = 0;
                var attributeValues = attribute.IsListTypeAttribute()
                    ? attribute.ProductVariantAttributeValues.OrderBy(x => x.DisplayOrder).ToList()
                    : new List<ProductVariantAttributeValue>();

                var attributeModel = new ProductDetailsModel.ProductVariantAttributeModel
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

                // Copy queried variant data (entered by customer) to model.
                if (query.Variants.Any())
                {
                    var selectedAttribute = query.Variants.FirstOrDefault(x =>
                        x.ProductId == product.Id &&
                        x.BundleItemId == bundleItemId &&
                        x.AttributeId == attribute.ProductAttributeId &&
                        x.VariantAttributeId == attribute.Id);

                    if (selectedAttribute != null)
                    {
                        switch (attribute.AttributeControlType)
                        {
                            case AttributeControlType.Datepicker:
                                if (selectedAttribute.Date.HasValue)
                                {
                                    attributeModel.SelectedDay = selectedAttribute.Date.Value.Day;
                                    attributeModel.SelectedMonth = selectedAttribute.Date.Value.Month;
                                    attributeModel.SelectedYear = selectedAttribute.Date.Value.Year;
                                }
                                break;
                            case AttributeControlType.FileUpload:
                                attributeModel.UploadedFileGuid = selectedAttribute.Value;

                                if (selectedAttribute.Value.HasValue() && Guid.TryParse(selectedAttribute.Value, out var guid))
                                {
                                    attributeModel.UploadedFileName = await _db.Downloads
                                        .AsNoTracking()
                                        .Where(x => x.DownloadGuid == guid)
                                        .Select(x => x.MediaFile.Name)
                                        .FirstOrDefaultAsync();
                                }
                                break;
                            case AttributeControlType.TextBox:
                            case AttributeControlType.MultilineTextbox:
                                attributeModel.TextValue = selectedAttribute.Value;
                                break;
                        }
                    }
                }

                // TODO: obsolete? Alias field is not used for custom values anymore, only for URL as URL variant alias.
                if (attribute.AttributeControlType == AttributeControlType.Datepicker && attributeModel.Alias.HasValue() && RegularExpressions.IsYearRange.IsMatch(attributeModel.Alias))
                {
                    var match = RegularExpressions.IsYearRange.Match(attributeModel.Alias);
                    attributeModel.BeginYear = match.Groups[1].Value.ToInt();
                    attributeModel.EndYear = match.Groups[2].Value.ToInt();
                }

                foreach (var value in attributeValues)
                {
                    ProductBundleItemAttributeFilter attributeFilter = null;
                    if (productBundleItem?.IsFilteredOut(value, out attributeFilter) ?? false)
                    {
                        continue;
                    }
                    if (preSelectedValueId == 0 && attributeFilter != null && attributeFilter.IsPreSelected)
                    {
                        preSelectedValueId = attributeFilter.AttributeValueId;
                    }

                    var valueModel = new ProductDetailsModel.ProductVariantAttributeValueModel
                    {
                        Id = value.Id,
                        ProductAttributeValue = value,
                        PriceAdjustment = string.Empty,
                        Name = value.GetLocalized(x => x.Name),
                        Alias = value.Alias,
                        Color = value.Color, // Used with "Boxes" attribute type.
                        IsPreSelected = value.IsPreSelected
                    };

                    if (value.ValueType == ProductVariantAttributeValueType.ProductLinkage &&
                        linkedProducts.TryGetValue(value.LinkedProductId, out var linkedProduct))
                    {
                        valueModel.SeName = await linkedProduct.GetActiveSlugAsync();
                    }

                    if (modelContext.DisplayPrices && !isBundlePricing)
                    {
                        if (priceAdjustments.TryGetValue(value.Id, out var priceAdjustment))
                        {
                            valueModel.PriceAdjustmentValue = priceAdjustment.Price.Amount;

                            if (_catalogSettings.ShowVariantCombinationPriceAdjustment && !product.CallForPrice)
                            {
                                if (priceAdjustment.Price > 0)
                                {
                                    valueModel.PriceAdjustment = $" (+{priceAdjustment.Price})";
                                }
                                else if (priceAdjustment.Price < 0)
                                {
                                    valueModel.PriceAdjustment = $" (-{priceAdjustment.Price * -1})";
                                }
                            }
                        }

                        if (valueModel.IsPreSelected)
                        {
                            preselectedWeightAdjustment += value.WeightAdjustment;
                        }

                        if (_catalogSettings.ShowLinkedAttributeValueQuantity && value.ValueType == ProductVariantAttributeValueType.ProductLinkage)
                        {
                            valueModel.QuantityInfo = value.Quantity;
                        }
                    }

                    if (_catalogSettings.ShowLinkedAttributeValueImage && value.ValueType == ProductVariantAttributeValueType.ProductLinkage)
                    {
                        var file = linkedMediaFiles.ContainsKey(value.LinkedProductId)
                            ? linkedMediaFiles[value.LinkedProductId].FirstOrDefault()?.MediaFile
                            : null;                        
                        if (file != null)
                        {
                            valueModel.ImageUrl = _mediaService.GetUrl(file, _mediaSettings.VariantValueThumbPictureSize, null, false);
                        }
                    }
                    else if (value.MediaFileId != 0)
                    {
                        valueModel.ImageUrl = await _mediaService.GetUrlAsync(value.MediaFileId, _mediaSettings.VariantValueThumbPictureSize, null, false);
                    }

                    attributeModel.Values.Add(valueModel);
                }

                // Add selected attributes for initially displayed combination images and multiple selected checkbox values.
                if (query.VariantCombinationId == 0)
                {
                    ProductDetailsModel.ProductVariantAttributeValueModel defaultValue = null;

                    if (preSelectedValueId != 0)
                    {
                        // Value preselected by a bundle item filter discards the default preselection.
                        attributeModel.Values.Each(x => x.IsPreSelected = false);

                        defaultValue = attributeModel.Values.OfType<ProductDetailsModel.ProductVariantAttributeValueModel>().FirstOrDefault(v => v.Id == preSelectedValueId);
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
                        // Apply attributes preselected by merchant.
                        foreach (var value in attributeModel.Values.Where(x => x.IsPreSelected))
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

                model.ProductVariantAttributes.Add(attributeModel);
            }

            if (query.Variants.Any() || query.VariantCombinationId != 0)
            {
                // Apply attribute combination if any.
                await PrepareProductAttributeCombinationsModelAsync(model, modelContext);               
            }
            else
            {
                // Apply weight adjustment of preselected attributes.
                model.WeightValue += preselectedWeightAdjustment;
            }            
        }

        protected async Task PrepareProductAttributeCombinationsModelAsync(ProductDetailsModel model, ProductDetailsModelContext modelContext)
        {
            var product = modelContext.Product;
            var query = modelContext.VariantQuery;
            var productBundleItem = modelContext.ProductBundleItem;
            var bundleItemId = productBundleItem?.Id ?? 0;
            var isBundlePricing = productBundleItem != null && !productBundleItem.BundleProduct.BundlePerItemPricing;
            var checkAvailability = product.AttributeChoiceBehaviour == AttributeChoiceBehaviour.GrayOutUnavailable;
            var attributes = await modelContext.BatchContext.Attributes.GetOrLoadAsync(product.Id);

            var res = new Dictionary<string, LocalizedString>(StringComparer.OrdinalIgnoreCase)
            {
                { "Products.Availability.IsNotActive", T("Products.Availability.IsNotActive") },
                { "Products.Availability.OutOfStock", T("Products.Availability.OutOfStock") },
                { "Products.Availability.Backordering", T("Products.Availability.Backordering") },
            };

            if (query.VariantCombinationId != 0)
            {
                var combination = await _db.ProductVariantAttributeCombinations.FindByIdAsync(query.VariantCombinationId, false);
                modelContext.SelectedAttributes = new ProductVariantAttributeSelection(combination?.RawAttributes);
            }
            else
            {
                var (selection, _) = await _productAttributeMaterializer.CreateAttributeSelectionAsync(query, attributes, product.Id, bundleItemId);
                modelContext.SelectedAttributes = selection;
            }

            var selectedValues = await _productAttributeMaterializer.MaterializeProductVariantAttributeValuesAsync(modelContext.SelectedAttributes);
            //var hasSelectedValues = modelContext.AttributeSelection.AttributesMap.Any();

            if (isBundlePricing)
            {
                model.AttributeInfo = await _productAttributeFormatter.FormatAttributesAsync(
                    modelContext.SelectedAttributes,
                    product,
                    modelContext.Customer,
                    separator: ", ",
                    includePrices: false,
                    includeGiftCardAttributes: false,
                    includeHyperlinks: false);
            }

            model.SelectedCombination = await _productAttributeMaterializer.FindAttributeCombinationAsync(product.Id, modelContext.SelectedAttributes);

            if (model.SelectedCombination != null && !model.SelectedCombination.IsActive)
            {
                model.IsAvailable = false;
                model.StockAvailability = res["Products.Availability.IsNotActive"];
            }

            // INFO: MergeWithCombination is required to apply attribute combination prices
            // and for product.IsAvailableByStock() call in PrepareProductPropertiesModelAsync.
            product.MergeWithCombination(model.SelectedCombination);

            // Explicitly selected values always discards values preselected by merchant.
            var selectedValueIds = selectedValues.Select(x => x.Id).ToArray();

            foreach (var attribute in model.ProductVariantAttributes)
            {
                var updatePreselection = selectedValueIds.Any() && selectedValueIds.Intersect(attribute.Values.Select(x => x.Id)).Any();

                foreach (ProductDetailsModel.ProductVariantAttributeValueModel value in attribute.Values)
                {
                    if (updatePreselection)
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
                            attributes,
                            selectedValues,
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

            // Apply weight adjustments.
            selectedValues.Each(x => model.WeightValue += x.WeightAdjustment);
        }

        protected async Task PrepareProductPropertiesModelAsync(ProductDetailsModel model, ProductDetailsModelContext modelContext)
        {
            var store = modelContext.Store;
            var customer = modelContext.Customer;
            var currency = modelContext.Currency;
            var product = modelContext.Product;
            var productBundleItem = modelContext.ProductBundleItem;
            var isBundle = product.ProductType == ProductType.BundledProduct;
            var hasSelectedAttributes = modelContext.SelectedAttributes?.AttributesMap?.Any() ?? false;

            if ((productBundleItem != null && !productBundleItem.BundleProduct.BundlePerItemShoppingCart) ||
                (product.ManageInventoryMethod == ManageInventoryMethod.ManageStockByAttributes && !hasSelectedAttributes))
            {
                // Cases where stock inventory is not functional (what ShoppingCartService.GetStandardWarnings and ProductService.AdjustInventory does not handle).
                model.IsAvailable = true;

                _db.IsCollectionLoaded(product, x => x.ProductVariantAttributeCombinations, out var collectionEntry);
                var hasAttributeCombinations = await collectionEntry
                    .Query()
                    .AnyAsync();

                model.StockAvailability = !hasAttributeCombinations ? product.FormatStockMessage(_localizationService) : string.Empty;
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
            model.ShowLegalInfo = !model.IsBundlePart && _taxSettings.ShowLegalHintsInProductDetails;
            model.BundleTitleText = product.GetLocalized(x => x.BundleTitleText);
            model.BundlePerItemPricing = product.BundlePerItemPricing;
            model.BundlePerItemShipping = product.BundlePerItemShipping;
            model.BundlePerItemShoppingCart = product.BundlePerItemShoppingCart;

            var basePricePricingOptions = _priceCalculationService.CreateDefaultOptions(false, customer, currency, modelContext.BatchContext);
            model.BasePriceInfo = await _priceCalculationService.GetBasePriceInfoAsync(product, basePricePricingOptions);

            var taxDisplayType = _services.WorkContext.GetTaxDisplayTypeFor(customer, store.Id);
            string taxInfo = T(taxDisplayType == TaxDisplayType.IncludingTax ? "Tax.InclVAT" : "Tax.ExclVAT");

            var defaultTaxRate = string.Empty;
            if (_taxSettings.DisplayTaxRates)
            {
                // INFO: (mh) (core) Taxrate always contains a Rate of 0 because not all TaxProviders are implemented yet.
                var taxRate = await _taxService.GetTaxRateAsync(product, customer: customer);
                if (taxRate.Rate != 0)
                {
                    var formattedTaxRate = _taxService.FormatTaxRate(taxRate.Rate);
                    defaultTaxRate = $"({formattedTaxRate}%)";
                }
            }

            var additionalShippingCosts = string.Empty;
            var addShippingPrice = _currencyService.ConvertFromPrimaryCurrency(product.AdditionalShippingCharge, currency);

            if (addShippingPrice > 0)
            {
                additionalShippingCosts = T("Common.AdditionalShippingSurcharge", addShippingPrice.ToString(true)) + ", ";
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

            var dimension = await _db.MeasureDimensions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == _measureSettings.BaseDimensionId);
            var weight = await _db.MeasureWeights.AsNoTracking().FirstOrDefaultAsync(x => x.Id == _measureSettings.BaseDimensionId);
            var dimensionSystemKeyword = dimension?.SystemKeyword ?? string.Empty;
            var weightSystemKeyword = dimension?.SystemKeyword ?? string.Empty;

            // INFO: model.WeightValue already calculated in previous helper methods.
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

            var quantityUnit = await _db.QuantityUnits.GetQuantityUnitByIdAsync(product.QuantityUnitId ?? 0, _catalogSettings.ShowDefaultQuantityUnit);
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

        protected async Task PrepareProductPriceModelAsync(ProductDetailsModel model, ProductDetailsModelContext modelContext, int selectedQuantity)
        {
            var customer = modelContext.Customer;
            var currency = modelContext.Currency;
            var product = modelContext.Product;
            var productBundleItem = modelContext.ProductBundleItem;
            var bundleItemId = productBundleItem?.Id;
            var isBundleItemPricing = productBundleItem != null && productBundleItem.BundleProduct.BundlePerItemPricing;
            var isBundlePricing = productBundleItem != null && !productBundleItem.BundleProduct.BundlePerItemPricing;
            var isBundle = product.ProductType == ProductType.BundledProduct;

            model.ProductPrice.ProductId = product.Id;
            model.ProductPrice.HidePrices = !modelContext.DisplayPrices;
            model.ProductPrice.ShowLoginNote = !modelContext.DisplayPrices && productBundleItem == null && _catalogSettings.ShowLoginForPriceNote;

            if (!modelContext.DisplayPrices)
            {
                model.ProductPrice.OldPrice = new();
                model.ProductPrice.Price = new();
                return;
            }

            if (product.CustomerEntersPrice && !isBundleItemPricing)
            {
                model.ProductPrice.CustomerEntersPrice = true;
                return;
            }

            if (product.CallForPrice && !isBundleItemPricing)
            {
                model.ProductPrice.CallForPrice = true;
                model.HotlineTelephoneNumber = _contactDataSettings.HotlineTelephoneNumber.NullEmpty();
                return;
            }

            var applyDiscountNote = false;
            var taxFormat = _currencyService.GetTaxFormat();
            var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, customer, currency, modelContext.BatchContext);
            var calculationContext = new PriceCalculationContext(product, selectedQuantity, calculationOptions)
            {
                AssociatedProducts = modelContext.AssociatedProducts,
                BundleItem = productBundleItem
            };

            // Apply price adjustments of attributes.
            if (!isBundlePricing)
            {
                if (modelContext.SelectedAttributes != null)
                {
                    // Apply price adjustments of selected attributes.
                    calculationContext.AddSelectedAttributes(modelContext.SelectedAttributes, product.Id, bundleItemId);
                }
                else if (isBundle && product.BundlePerItemPricing && modelContext.VariantQuery.Variants.Any())
                {
                    // Apply price adjustments of selected bundle items attributes.
                    // INFO: bundles themselves don't have attributes, that's why modelContext.SelectedAttributes is null.
                    calculationContext.BundleItems = await modelContext.BatchContext.ProductBundleItems.GetOrLoadAsync(product.Id);

                    modelContext.BatchContext.Collect(calculationContext.BundleItems.Select(x => x.ProductId).ToArray());

                    foreach (var bundleItem in calculationContext.BundleItems)
                    {
                        var bundleItemAttributes = await modelContext.BatchContext.Attributes.GetOrLoadAsync(bundleItem.ProductId);
                        var (selection, _) = await _productAttributeMaterializer.CreateAttributeSelectionAsync(modelContext.VariantQuery, bundleItemAttributes, bundleItem.ProductId, bundleItem.Id, false);

                        calculationContext.AddSelectedAttributes(selection, bundleItem.ProductId, bundleItem.Id);

                        // Apply attribute combination price if any.
                        await _productAttributeMaterializer.MergeWithCombinationAsync(bundleItem.Product, selection);
                    }
                }
                else
                {
                    // Apply price adjustments of attributes preselected by merchant.
                    calculationContext.Options.ApplyPreselectedAttributes = true;
                }
            }

            var priceWithDiscount = await _priceCalculationService.CalculatePriceAsync(calculationContext);

            // INFO: original code difference. Adjustments of priceWithoutDiscount were calculated with quantity of 1. Why? Makes little sense to me.
            // Be careful, options are shallow copied! 'calculationOptions.IgnoreDiscounts = true' would not work.
            calculationContext.Options.IgnoreDiscounts = true;
            var priceWithoutDiscount = await _priceCalculationService.CalculatePriceAsync(calculationContext);

            var oldPriceBase = await _taxCalculator.CalculateProductTaxAsync(product, product.OldPrice, null, customer, currency);
            var oldPrice = _currencyService.ConvertFromPrimaryCurrency(oldPriceBase.Price, currency);

            if (productBundleItem == null || isBundleItemPricing)
            {
                if (oldPrice > 0 && oldPrice > priceWithoutDiscount.FinalPrice)
                {
                    model.ProductPrice.OldPrice = oldPrice.WithPostFormat(taxFormat);
                }

                applyDiscountNote = priceWithoutDiscount.FinalPrice != priceWithDiscount.FinalPrice;
            }

            if (product.SpecialPriceEndDateTimeUtc.HasValue && product.SpecialPriceEndDateTimeUtc > DateTime.UtcNow)
            {
                model.ProductPrice.PriceValidUntilUtc = product.SpecialPriceEndDateTimeUtc.Value.ToString("u");
            }

            model.ProductPrice.Price = priceWithoutDiscount.FinalPrice;
            model.ProductPrice.PriceWithDiscount = priceWithDiscount.FinalPrice;
            model.BasePriceInfo = _priceCalculationService.GetBasePriceInfo(product, priceWithDiscount.FinalPrice, currency);

            if (model.ProductPrice.OldPrice > 0 || applyDiscountNote)
            {
                model.ProductPrice.NoteWithoutDiscount = T(isBundle && product.BundlePerItemPricing ? "Products.Bundle.PriceWithoutDiscount.Note" : "Products.Price");
            }

            if (isBundle && product.BundlePerItemPricing && applyDiscountNote && !product.HasTierPrices)
            {
                model.ProductPrice.NoteWithDiscount = T("Products.Bundle.PriceWithDiscount.Note");
            }

            // Calculate saving.
            var savings = priceWithDiscount.PriceSaving;
            if (savings.HasSaving)
            {
                model.ProductPrice.SavingAmount = savings.SavingAmount.Value;
                model.ProductPrice.SavingPercent = savings.SavingPercent;
            }

            model.TierPrices = await CreateTierPriceModelAsync(modelContext, product);
        }

        protected void PrepareProductCartModel(ProductDetailsModel model, ProductDetailsModelContext modelContext, int selectedQuantity)
        {
            var product = modelContext.Product;
            var currency = modelContext.Currency;
            var displayPrices = modelContext.DisplayPrices;

            model.AddToCart.ProductId = product.Id;
            model.AddToCart.EnteredQuantity = product.OrderMinimumQuantity > selectedQuantity ? product.OrderMinimumQuantity : selectedQuantity;
            model.AddToCart.MinOrderAmount = product.OrderMinimumQuantity;
            model.AddToCart.MaxOrderAmount = product.OrderMaximumQuantity;
            model.AddToCart.QuantityUnitName = model.QuantityUnitName; // TODO: (mc) remove 'QuantityUnitName' from parent model later
            model.AddToCart.QuantityStep = product.QuantityStep > 0 ? product.QuantityStep : 1;
            model.AddToCart.HideQuantityControl = product.HideQuantityControl;
            model.AddToCart.QuantityControlType = product.QuantityControlType;
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

                model.AddToCart.CustomerEnteredPrice = minimumCustomerEnteredPrice.Amount;
                model.AddToCart.CustomerEnteredPriceRange = T("Products.EnterProductPrice.Range",
                    _currencyService.ConvertToWorkingCurrency(minimumCustomerEnteredPrice),
                    _currencyService.ConvertToWorkingCurrency(maximumCustomerEnteredPrice));
            }

            var allowedQuantities = product.ParseAllowedQuantities();
            foreach (var qty in allowedQuantities)
            {
                model.AddToCart.AllowedQuantities.Add(new SelectListItem
                {
                    Text = qty.ToString(),
                    Value = qty.ToString()
                });
            }
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

            model.ProductTags = await _services.CacheFactory.GetMemoryCache().GetAsync(cacheKey, async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromHours(3));

                await _db.LoadCollectionAsync(product, x => x.ProductTags);

                var models = new List<ProductTagModel>();
                var productCountsMap = await _productTagService.GetProductCountsMapAsync(null, storeId);

                foreach (var tag in product.ProductTags.Where(x => x.Published))
                {
                    if (productCountsMap.TryGetValue(tag.Id, out var productCount) && productCount > 0)
                    {
                        models.Add(new ProductTagModel
                        {
                            Id = tag.Id,
                            Name = tag.GetLocalized(x => x.Name),
                            Slug = tag.BuildSlug(),
                            ProductCount = productCount
                        });
                    }
                }

                return models.OrderBy(x => x.Name).ToList();
            });
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
            ProductBundleItem bundleItem = null,
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

        protected async Task<List<ProductDetailsModel.TierPriceModel>> CreateTierPriceModelAsync(ProductDetailsModelContext modelContext, Product product)
        {
            var tierPrices = product.TierPrices
                .FilterByStore(modelContext.Store.Id)
                .FilterForCustomer(modelContext.Customer)
                .OrderBy(x => x.Quantity)
                .ToList()
                .RemoveDuplicatedQuantities();

            if (!tierPrices.Any())
            {
                return new List<ProductDetailsModel.TierPriceModel>();
            }

            var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, modelContext.Customer, modelContext.Currency, modelContext.BatchContext);
            calculationOptions.TaxFormat = null;

            var calculationContext = new PriceCalculationContext(product, 1, calculationOptions)
            {
                AssociatedProducts = modelContext.AssociatedProducts,
                BundleItem = modelContext.ProductBundleItem
            };
            
            calculationContext.AddSelectedAttributes(modelContext.SelectedAttributes, product.Id, modelContext.ProductBundleItem?.Id);

            var model = await tierPrices
                .SelectAsync(async (tierPrice) =>
                {
                    calculationContext.Quantity = tierPrice.Quantity;

                    var price = await _priceCalculationService.CalculatePriceAsync(calculationContext);

                    var tierPriceModel = new ProductDetailsModel.TierPriceModel
                    {
                        Quantity = tierPrice.Quantity,
                        Price = price.FinalPrice
                    };

                    return tierPriceModel;
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

            product = _db.FindTracked<Product>(product.Id) ?? product;
            var collectionLoaded = _db.IsCollectionLoaded(product, x => x.ProductReviews, out var collectionEntry);

            if (!collectionLoaded)
            {
                _db.Attach(product);
            }

            // We need the query for total count resolution.
            var query = collectionEntry
                .Query()
                .Where(x => x.IsApproved);

            model.TotalReviewsCount = collectionLoaded
                ? product.ProductReviews.Count
                : await query.CountAsync();

            if (model.TotalReviewsCount > 0)
            {
                query = query.OrderByDescending(x => x.CreatedOnUtc);

                if (take.HasValue)
                {
                    query = query.Take(take.Value);
                }
                
                var reviews = collectionLoaded 
                    ? product.ProductReviews.Take(take ?? int.MaxValue).ToList() 
                    : await query.Include(x => x.Customer).ToListAsync();

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
                        WrittenOnStr = _services.DateTimeHelper.ConvertToUserTime(review.CreatedOnUtc, DateTimeKind.Utc).ToString("D"),
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
    }
}
