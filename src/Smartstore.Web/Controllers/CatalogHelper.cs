using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Smartstore.Caching;
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
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
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
        private readonly IRuleProviderFactory _ruleProviderFactory;
        private readonly ITaxService _taxService;
        private readonly ITaxCalculator _taxCalculator;
        private readonly ICurrencyService _currencyService;
        private readonly IMediaService _mediaService;
        private readonly ILocalizationService _localizationService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IPriceLabelService _priceLabelService;
        private readonly IStockSubscriptionService _stockSubscriptionService;
        private readonly MediaSettings _mediaSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly PriceSettings _priceSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly CaptchaSettings _captchaSettings;
        private readonly TaxSettings _taxSettings;
        private readonly PerformanceSettings _performanceSettings;
        private readonly MeasureSettings _measureSettings;
        private readonly SearchSettings _searchSettings;
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
        private readonly bool _isMultiLanguageEnvironment;

        public CatalogHelper(
            SmartDbContext db,
            ICommonServices services,
            IMenuService menuService,
            IProductService productService,
            IProductAttributeService productAttributeService,
            IProductAttributeMaterializer productAttributeMaterializer,
            IProductAttributeFormatter productAttributeFormatter,
            IRuleProviderFactory ruleProviderFactory,
            ITaxService taxService,
            ITaxCalculator taxCalculator,
            ICurrencyService currencyService,
            IMediaService mediaService,
            IPriceCalculationService priceCalculationService,
            IPriceLabelService priceLabelService,
            IStockSubscriptionService stockSubscriptionService,
            MediaSettings mediaSettings,
            CatalogSettings catalogSettings,
            PriceSettings priceSettings,
            CustomerSettings customerSettings,
            CaptchaSettings captchaSettings,
            MeasureSettings measureSettings,
            SearchSettings searchSettings,
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
            IProductTagService productTagService,
            ILanguageService languageService)
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
            _ruleProviderFactory = ruleProviderFactory;
            _taxService = taxService;
            _taxCalculator = taxCalculator;
            _currencyService = currencyService;
            _mediaService = mediaService;
            _localizationService = _services.Localization;
            _priceCalculationService = priceCalculationService;
            _priceLabelService = priceLabelService;
            _stockSubscriptionService = stockSubscriptionService;
            _measureSettings = measureSettings;
            _searchSettings = searchSettings;
            _taxSettings = taxSettings;
            _performanceSettings = performanceSettings;
            _deliveryTimeService = deliveryTimeService;
            _mediaSettings = mediaSettings;
            _catalogSettings = catalogSettings;
            _priceSettings = priceSettings;
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
            _isMultiLanguageEnvironment = languageService.IsMultiLanguageEnvironment();
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
                var manufacturerFileIds = brands
                    .Select(x => x.Manufacturer.MediaFileId ?? 0)
                    .Where(x => x > 0)
                    .Distinct()
                    .ToArray();

                if (manufacturerFileIds.Length > 0)
                {
                    mediaFileLookup = (await _db.MediaFiles
                        .AsNoTracking()
                        .Where(x => manufacturerFileIds.Contains(x.Id))
                        .ToListAsync())
                        .Select(_mediaService.ConvertMediaFile)
                        .ToDictionary(x => x.Id);
                }

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

            var model = new ImageModel(file, _mediaSettings.ManufacturerThumbPictureSize)
            {
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
                        Image = new ImageModel(file, _mediaSettings.ManufacturerThumbPictureSize)
                        {
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
                .SelectAwait(async c =>
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

                    model.Image = new ImageModel(file, thumbSize)
                    {
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
            if (menu == null)
            {
                return;
            }

            var currentNode = await menu.ResolveCurrentNodeAsync(context);

            currentNode?.Trail.Where(x => !x.IsRoot).Each(x => breadcrumb.Track(x.Value));

            // Add trail of parent product if product has no category assigned.
            if (product != null && !(breadcrumb.Trail?.Any() ?? false) && product.ParentGroupedProductId != 0)
            {
                var parentProduct = await _db.Products
                    .AsNoTracking()
                    .SelectSummary()
                    .FirstOrDefaultAsync(x => x.Id == product.ParentGroupedProductId);

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

            var model = new ImageModel(file, _mediaSettings.CategoryThumbPictureSize)
            {
                Title = file?.File?.GetLocalized(x => x.Title)?.Value.NullEmpty() ?? string.Format(T("Media.Category.ImageLinkTitleFormat"), localizedName),
                Alt = file?.File?.GetLocalized(x => x.Alt)?.Value.NullEmpty() ?? string.Format(T("Media.Category.ImageAlternateTextFormat"), localizedName),
                NoFallback = _catalogSettings.HideCategoryDefaultPictures
            };

            _services.DisplayControl.Announce(file?.File);

            return model;
        }

        #endregion

        #region Cargo Data

        protected async Task<DeliveryTime> GetDeliveryTimeAsync(int id)
        {
            if (id == 0) return null;

            var key = $"CatalogHelper.DeliveryTime.{id}";
            return await _httpRequest.HttpContext.GetItemAsync(key, async () => 
            {
                return await _db.DeliveryTimes.FindByIdAsync(id, true);
            });
        }

        protected async Task<MeasureWeight> GetMeasureWeightAsync(int id)
        {
            if (id == 0) return null;

            var key = $"CatalogHelper.MeasureWeight.{id}";
            return await _httpRequest.HttpContext.GetItemAsync(key, async () =>
            {
                return await _db.MeasureWeights.FindByIdAsync(id, true);
            });
        }

        protected async Task<MeasureDimension> GetMeasureDimensionAsync(int id)
        {
            if (id == 0) return null;

            var key = $"CatalogHelper.MeasureDimension.{id}";
            return await _httpRequest.HttpContext.GetItemAsync(key, async () =>
            {
                return await _db.MeasureDimensions.FindByIdAsync(id, true);
            });
        }

        #endregion
    }
}
