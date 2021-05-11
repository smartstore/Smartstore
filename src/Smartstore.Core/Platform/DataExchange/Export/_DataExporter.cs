using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.DataExchange.Export.Internal;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Domain;
using Smartstore.Threading;

namespace Smartstore.Core.DataExchange.Export
{
    public partial class DataExporter : IDataExporter
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductService _productService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly ICurrencyService _currencyService;
        private readonly IMediaService _mediaService;
        private readonly ILanguageService _languageService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IUrlService _urlService;
        private readonly ProductUrlHelper _productUrlHelper;
        private readonly ITaxCalculator _taxCalculator;

        private readonly CatalogSettings _catalogSettings;
        private readonly MediaSettings _mediaSettings;
        private readonly SeoSettings _seoSettings;
        private readonly CustomerSettings _customerSettings;

        public DataExporter(
            SmartDbContext db,
            ICommonServices services,
            IPriceCalculationService priceCalculationService,
            IProductService productService,
            IProductAttributeMaterializer productAttributeMaterializer,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            ICurrencyService currencyService,
            IMediaService mediaService,
            ILanguageService languageService,
            ILocalizedEntityService localizedEntityService,
            IUrlService urlService,
            ProductUrlHelper productUrlHelper,
            ITaxCalculator taxCalculator,
            CatalogSettings catalogSettings,
            MediaSettings mediaSettings,
            SeoSettings seoSettings,
            CustomerSettings customerSettings)
        {
            _db = db;
            _services = services;
            _priceCalculationService = priceCalculationService;
            _productService = productService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _currencyService = currencyService;
            _mediaService = mediaService;
            _languageService = languageService;
            _localizedEntityService = localizedEntityService;
            _urlService = urlService;
            _productUrlHelper = productUrlHelper;
            _taxCalculator = taxCalculator;

            _catalogSettings = catalogSettings;
            _mediaSettings = mediaSettings;
            _seoSettings = seoSettings;
            _customerSettings = customerSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        /// <summary>
        /// The name of the public export folder.
        /// </summary>
        public static string PublicFolder => "Exchange";

        /// <summary>
        /// The page size for loading data from database during export.
        /// </summary>
        public static int PageSize => 100;

        public async Task<DataExportResult> ExportAsync(DataExportRequest request, CancellationToken cancellationToken)
        {
            var ctx = new DataExporterContext(request, false, cancellationToken);

            if (!(request?.Profile?.Enabled ?? false))
            {
                return ctx.Result;
            }

            var lockKey = $"dataexporter:profile:{request.Profile.Id}";
            if (AsyncLock.IsLockHeld(lockKey))
            {
                ctx.Result.LastError = $"The execution of the profile \"{request.Profile.Name.NaIfEmpty()}\" (ID {request.Profile.Id}) is locked.";
                return ctx.Result;
            }

            using (await AsyncLock.KeyedAsync(lockKey, cancellationToken: cancellationToken))
            {
                //...
            }

            cancellationToken.ThrowIfCancellationRequested();

            return ctx.Result;
        }

        public Task<DataExportPreviewResult> PreviewAsync(DataExportRequest request, int pageIndex)
        {
            throw new NotImplementedException();
        }

        private async Task<List<Store>> Init(DataExporterContext ctx)
        {
            List<Store> result = null;
            var priceDisplay = ctx.Projection.PriceType ?? _catalogSettings.PriceDisplayType;

            ctx.ContextCurrency = (await _db.Currencies.FindByIdAsync(ctx.Projection.CurrencyId ?? 0, false)) ?? _services.WorkContext.WorkingCurrency;
            ctx.ContextCustomer = (await _db.Customers.FindByIdAsync(ctx.Projection.CustomerId ?? 0, false)) ?? _services.WorkContext.CurrentCustomer;
            ctx.ContextLanguage = (await _db.Languages.FindByIdAsync(ctx.Projection.LanguageId ?? 0, false)) ?? _services.WorkContext.WorkingLanguage;

            ctx.Stores = _services.StoreContext.GetAllStores().ToDictionarySafe(x => x.Id, x => x);
            ctx.Languages = await _db.Languages.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x);

            // TODO: (mg) (core) should be done later when batch context is available.
            ctx.PriceCalculationOptions = _priceCalculationService.CreateDefaultOptions(false, ctx.ContextCustomer, ctx.ContextCurrency, ctx.ProductBatchContext);
            ctx.PriceCalculationOptions.DetermineLowestPrice = priceDisplay == PriceDisplayType.LowestPrice;
            ctx.PriceCalculationOptions.DeterminePreselectedPrice = priceDisplay == PriceDisplayType.PreSelectedPrice;
            ctx.PriceCalculationOptions.ApplyPreselectedAttributes = priceDisplay == PriceDisplayType.PreSelectedPrice;

            if (ctx.Projection.ConvertNetToGrossPrices)
            {
                ctx.PriceCalculationOptions.TaxInclusive = true;
            }

            if (!ctx.IsPreview)
            {
                // Get all translations and slugs for global entities in one go.
                ctx.Translations[nameof(Currency)] = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(nameof(Currency), null);
                ctx.Translations[nameof(Country)] = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(nameof(Country), null);
                ctx.Translations[nameof(StateProvince)] = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(nameof(StateProvince), null);
                ctx.Translations[nameof(DeliveryTime)] = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(nameof(DeliveryTime), null);
                ctx.Translations[nameof(QuantityUnit)] = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(nameof(QuantityUnit), null);
                ctx.Translations[nameof(Manufacturer)] = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(nameof(Manufacturer), null);
                ctx.Translations[nameof(Category)] = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(nameof(Category), null);

                ctx.Slugs[nameof(Category)] = await _urlService.GetUrlRecordCollectionAsync(nameof(Category), null, null);
                ctx.Slugs[nameof(Manufacturer)] = await _urlService.GetUrlRecordCollectionAsync(nameof(Manufacturer), null, null);
            }

            if (!ctx.IsPreview && ctx.Request.Profile.PerStore)
            {
                result = new List<Store>(ctx.Stores.Values.Where(x => x.Id == ctx.Filter.StoreId || ctx.Filter.StoreId == 0));
            }
            else
            {
                int? storeId = ctx.Filter.StoreId == 0 ? ctx.Projection.StoreId : ctx.Filter.StoreId;
                ctx.Store = ctx.Stores.Values.FirstOrDefault(x => x.Id == (storeId ?? _services.StoreContext.CurrentStore.Id));

                result = new List<Store> { ctx.Store };
            }

            // Get record stats for each store.
            foreach (var store in result)
            {
                ctx.Store = store;

                IQueryable<BaseEntity> query = null;

                switch (ctx.Request.Provider.Value.EntityType)
                {
                    case ExportEntityType.Product:
                        //query = GetProductQuery(ctx, ctx.Request.Profile.Offset, int.MaxValue);
                        break;
                    case ExportEntityType.Order:
                        //query = GetOrderQuery(ctx, ctx.Request.Profile.Offset, int.MaxValue);
                        break;
                    case ExportEntityType.Manufacturer:
                        //query = GetManufacturerQuery(ctx, ctx.Request.Profile.Offset, int.MaxValue);
                        break;
                    case ExportEntityType.Category:
                        //query = GetCategoryQuery(ctx, ctx.Request.Profile.Offset, int.MaxValue);
                        break;
                    case ExportEntityType.Customer:
                        query = GetCustomerQuery(ctx.Request.Profile.Offset, int.MaxValue, ctx);
                        break;
                    case ExportEntityType.NewsLetterSubscription:
                        query = await GetNewsletterSubscriptionQuery(ctx.Request.Profile.Offset, int.MaxValue, ctx);
                        break;
                    case ExportEntityType.ShoppingCartItem:
                        query = GetShoppingCartItemQuery(ctx.Request.Profile.Offset, int.MaxValue, ctx);
                        break;
                }

                var stats = new RecordStats
                {
                    TotalRecords = query.Count()
                };

                if (!ctx.IsPreview)
                {
                    stats.MaxId = query.Max(x => (int?)x.Id) ?? 0;
                }

                ctx.StatsPerStore[store.Id] = stats;
            }

            return result;
        }

        #region Entity queries

        private IQueryable<Customer> GetCustomerQuery(int? skip, int take, DataExporterContext ctx)
        {
            var skipValue = skip.GetValueOrDefault();
            if (skipValue == 0 && ctx.LastId == 0)
            {
                skipValue = Math.Max(ctx.Request.Profile.Offset, 0);
            }

            var query = _db.Customers
                .AsNoTracking()
                .Include(x => x.BillingAddress)
                .Include(x => x.ShippingAddress)
                .Include(x => x.Addresses)
                    .ThenInclude(x => x.Country)
                .Include(x => x.Addresses)
                    .ThenInclude(x => x.StateProvince)
                .Include(x => x.CustomerRoleMappings)
                    .ThenInclude(x => x.CustomerRole)
                .AsQueryable();

            if (ctx.Filter.IsActiveCustomer.HasValue)
            {
                query = query.Where(x => x.Active == ctx.Filter.IsActiveCustomer.Value);
            }

            if (ctx.Filter.IsTaxExempt.HasValue)
            {
                query = query.Where(x => x.IsTaxExempt == ctx.Filter.IsTaxExempt.Value);
            }

            if (ctx.Filter.CustomerRoleIds != null && ctx.Filter.CustomerRoleIds.Any())
            {
                query = query.Where(x => x.CustomerRoleMappings.Select(y => y.CustomerRoleId).Intersect(ctx.Filter.CustomerRoleIds).Any());
            }

            if (ctx.Filter.BillingCountryIds != null && ctx.Filter.BillingCountryIds.Any())
            {
                query = query.Where(x => x.BillingAddress != null && ctx.Filter.BillingCountryIds.Contains(x.BillingAddress.Id));
            }

            if (ctx.Filter.ShippingCountryIds != null && ctx.Filter.ShippingCountryIds.Any())
            {
                query = query.Where(x => x.ShippingAddress != null && ctx.Filter.ShippingCountryIds.Contains(x.ShippingAddress.Id));
            }

            if (ctx.Filter.LastActivityFrom.HasValue)
            {
                var activityFrom = _services.DateTimeHelper.ConvertToUtcTime(ctx.Filter.LastActivityFrom.Value, _services.DateTimeHelper.CurrentTimeZone);
                query = query.Where(x => activityFrom <= x.LastActivityDateUtc);
            }

            if (ctx.Filter.LastActivityTo.HasValue)
            {
                var activityTo = _services.DateTimeHelper.ConvertToUtcTime(ctx.Filter.LastActivityTo.Value, _services.DateTimeHelper.CurrentTimeZone);
                query = query.Where(x => activityTo >= x.LastActivityDateUtc);
            }

            // TODO: (mg) (core) Test these two queries because they probably do not run.
            if (ctx.Filter.HasSpentAtLeastAmount.HasValue)
            {
                query = query
                    .Join(_db.Orders, x => x.Id, y => y.CustomerId, (x, y) => new { Customer = x, Order = y })
                    .GroupBy(x => x.Customer.Id)
                    .Select(x => new
                    {
                        x.FirstOrDefault().Customer,
                        OrderTotal = x.Sum(y => y.Order.OrderTotal)
                    })
                    .Where(x => x.OrderTotal >= ctx.Filter.HasSpentAtLeastAmount.Value)
                    .Select(x => x.Customer);
            }

            if (ctx.Filter.HasPlacedAtLeastOrders.HasValue)
            {
                query = query
                    .Join(_db.Orders, x => x.Id, y => y.CustomerId, (x, y) => new { Customer = x, Order = y })
                    .GroupBy(x => x.Customer.Id)
                    .Select(x => new
                    {
                        Customer = x.FirstOrDefault().Customer,
                        OrderCount = x.Count()
                    })
                    .Where(x => x.OrderCount >= ctx.Filter.HasPlacedAtLeastOrders.Value)
                    .Select(x => x.Customer);
            }

            if (ctx.Request.EntitiesToExport.Any())
            {
                query = query.Where(x => ctx.Request.EntitiesToExport.Contains(x.Id));
            }

            query = query.OrderBy(x => x.Id);

            if (skipValue > 0)
            {
                query = query.Skip(skipValue);
            }
            else if (ctx.LastId > 0)
            {
                query = query.Where(x => x.Id > ctx.LastId);
            }

            if (take != int.MaxValue)
            {
                query = query.Take(take);
            }

            return query;
        }

        private async Task<IQueryable<NewsletterSubscription>> GetNewsletterSubscriptionQuery(int? skip, int take, DataExporterContext ctx)
        {
            var storeId = ctx.Request.Profile.PerStore ? ctx.Store.Id : ctx.Filter.StoreId;

            var skipValue = skip.GetValueOrDefault();
            if (skipValue == 0 && ctx.LastId == 0)
            {
                skipValue = Math.Max(ctx.Request.Profile.Offset, 0);
            }

            var customerQuery = _db.Customers.AsNoTracking();

            var query =
                from ns in _db.NewsletterSubscriptions.AsNoTracking()
                join c in customerQuery on ns.Email equals c.Email into customers
                from c in customers.DefaultIfEmpty()
                select new NewsletterSubscriber
                {
                    Subscription = ns,
                    Customer = c
                };

            if (storeId > 0)
            {
                query = query.Where(x => x.Subscription.StoreId == storeId);
            }

            if (ctx.Filter.IsActiveSubscriber.HasValue)
            {
                query = query.Where(x => x.Subscription.Active == ctx.Filter.IsActiveSubscriber.Value);
            }

            if (ctx.Filter.WorkingLanguageId != null && ctx.Filter.WorkingLanguageId != 0)
            {
                var isMasterLanguage = ctx.Filter.WorkingLanguageId == (await _languageService.GetMasterLanguageIdAsync(ctx.Store.Id));
                if (isMasterLanguage)
                {
                    query = query.Where(x => x.Subscription.WorkingLanguageId == 0 || x.Subscription.WorkingLanguageId == ctx.Filter.WorkingLanguageId);
                }
                else
                {
                    query = query.Where(x => x.Subscription.WorkingLanguageId == ctx.Filter.WorkingLanguageId);
                }
            }

            if (ctx.Filter.CreatedFrom.HasValue)
            {
                var createdFrom = _services.DateTimeHelper.ConvertToUtcTime(ctx.Filter.CreatedFrom.Value, _services.DateTimeHelper.CurrentTimeZone);
                query = query.Where(x => createdFrom <= x.Subscription.CreatedOnUtc);
            }

            if (ctx.Filter.CreatedTo.HasValue)
            {
                var createdTo = _services.DateTimeHelper.ConvertToUtcTime(ctx.Filter.CreatedTo.Value, _services.DateTimeHelper.CurrentTimeZone);
                query = query.Where(x => createdTo >= x.Subscription.CreatedOnUtc);
            }

            if (ctx.Filter.CustomerRoleIds != null && ctx.Filter.CustomerRoleIds.Any())
            {
                query = query.Where(x => x.Customer.CustomerRoleMappings.Select(y => y.CustomerRoleId).Intersect(ctx.Filter.CustomerRoleIds).Any());
            }

            if (ctx.Request.EntitiesToExport.Any())
            {
                query = query.Where(x => ctx.Request.EntitiesToExport.Contains(x.Subscription.Id));
            }

            query = query.OrderBy(x => x.Subscription.Id);

            if (skipValue > 0)
            {
                query = query.Skip(skipValue);
            }
            else if (ctx.LastId > 0)
            {
                query = query.Where(x => x.Subscription.Id > ctx.LastId);
            }

            if (take != int.MaxValue)
            {
                query = query.Take(take);
            }

            return query.Select(x => x.Subscription);
        }

        private IQueryable<ShoppingCartItem> GetShoppingCartItemQuery(int? skip, int take, DataExporterContext ctx)
        {
            var storeId = ctx.Request.Profile.PerStore ? ctx.Store.Id : ctx.Filter.StoreId;

            var skipValue = skip.GetValueOrDefault();
            if (skipValue == 0 && ctx.LastId == 0)
            {
                skipValue = Math.Max(ctx.Request.Profile.Offset, 0);
            }

            var query = _db.ShoppingCartItems
                .AsNoTracking()
                .Include(x => x.Customer)
                .ThenInclude(x => x.CustomerRoleMappings)
                .ThenInclude(x => x.CustomerRole)
                .Include(x => x.Product)
                .AsQueryable();

            if (storeId > 0)
            {
                query = query.Where(x => x.StoreId == storeId);
            }

            if (ctx.Request.ActionOrigin.EqualsNoCase("CurrentCarts"))
            {
                query = query.Where(x => x.ShoppingCartTypeId == (int)ShoppingCartType.ShoppingCart);
            }
            else if (ctx.Request.ActionOrigin.EqualsNoCase("CurrentWishlists"))
            {
                query = query.Where(x => x.ShoppingCartTypeId == (int)ShoppingCartType.Wishlist);
            }
            else if (ctx.Filter.ShoppingCartTypeId.HasValue)
            {
                query = query.Where(x => x.ShoppingCartTypeId == ctx.Filter.ShoppingCartTypeId.Value);
            }

            if (ctx.Filter.IsActiveCustomer.HasValue)
            {
                query = query.Where(x => x.Customer.Active == ctx.Filter.IsActiveCustomer.Value);
            }

            if (ctx.Filter.IsTaxExempt.HasValue)
            {
                query = query.Where(x => x.Customer.IsTaxExempt == ctx.Filter.IsTaxExempt.Value);
            }

            if (ctx.Filter.CustomerRoleIds != null && ctx.Filter.CustomerRoleIds.Any())
            {
                query = query.Where(x => x.Customer.CustomerRoleMappings.Select(y => y.CustomerRoleId).Intersect(ctx.Filter.CustomerRoleIds).Any());
            }

            if (ctx.Filter.LastActivityFrom.HasValue)
            {
                var activityFrom = _services.DateTimeHelper.ConvertToUtcTime(ctx.Filter.LastActivityFrom.Value, _services.DateTimeHelper.CurrentTimeZone);
                query = query.Where(x => activityFrom <= x.Customer.LastActivityDateUtc);
            }

            if (ctx.Filter.LastActivityTo.HasValue)
            {
                var activityTo = _services.DateTimeHelper.ConvertToUtcTime(ctx.Filter.LastActivityTo.Value, _services.DateTimeHelper.CurrentTimeZone);
                query = query.Where(x => activityTo >= x.Customer.LastActivityDateUtc);
            }

            if (ctx.Filter.CreatedFrom.HasValue)
            {
                var createdFrom = _services.DateTimeHelper.ConvertToUtcTime(ctx.Filter.CreatedFrom.Value, _services.DateTimeHelper.CurrentTimeZone);
                query = query.Where(x => createdFrom <= x.CreatedOnUtc);
            }

            if (ctx.Filter.CreatedTo.HasValue)
            {
                var createdTo = _services.DateTimeHelper.ConvertToUtcTime(ctx.Filter.CreatedTo.Value, _services.DateTimeHelper.CurrentTimeZone);
                query = query.Where(x => createdTo >= x.CreatedOnUtc);
            }

            if (ctx.Projection.NoBundleProducts)
            {
                query = query.Where(x => x.Product.ProductTypeId != (int)ProductType.BundledProduct);
            }
            else
            {
                query = query.Where(x => x.BundleItemId == null);
            }

            if (ctx.Request.EntitiesToExport.Any())
            {
                query = query.Where(x => ctx.Request.EntitiesToExport.Contains(x.Id));
            }

            query = query.OrderBy(x => x.Id);

            if (skipValue > 0)
            {
                query = query.Skip(skipValue);
            }
            else if (ctx.LastId > 0)
            {
                query = query.Where(x => x.Id > ctx.LastId);
            }

            if (take != int.MaxValue)
            {
                query = query.Take(take);
            }

            return query;
        }

        #endregion

        #region Utilities

        #endregion
    }
}
