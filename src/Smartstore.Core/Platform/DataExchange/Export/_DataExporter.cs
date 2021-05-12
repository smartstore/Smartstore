using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smartstore.Collections;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.DataExchange.Export.Internal;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data.Batching;
using Smartstore.Domain;
using Smartstore.Threading;
using Smartstore.Utilities;

namespace Smartstore.Core.DataExchange.Export
{
    public partial class DataExporter : IDataExporter
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductService _productService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ICatalogSearchService _catalogSearchService;
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
            ICatalogSearchService catalogSearchService,
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
            _catalogSearchService = catalogSearchService;
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
        /// The name of the wwwroot subfolder where export files are to be exported to be publicly accessible.
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

            using (await AsyncLock.KeyedAsync(lockKey, null, cancellationToken))
            {
                var profile = ctx.Request.Profile;

                // TODO: (mg) (core) find out how to log into a single file per profile (with fixed name 'log.txt' and without date stamp).
                // RE: LogFile pathes are rooted to app root path, so to log to file "~/App_Data/Tenants/Default/Export/MyProfile/log.txt",
                // use logger name: "File/App_Data/Tenants/Default/Export/MyProfile/log.txt"
                ctx.Log = _services.LoggerFactory.CreateLogger("File/" + profile.GetExportLogPath());
                ctx.ExecuteContext.Log = ctx.Log;

                try
                {
                    ctx.ProgressInfo = T("Admin.DataExchange.Export.ProgressInfo");

                    if (ctx.Request.Provider?.Value == null)
                        throw new SmartException("Export aborted because the export provider is not valid.");

                    if (!await HasPermission(ctx))
                        throw new SmartException("You do not have permission to perform the selected export.");

                    ctx.Request.CustomData.Each(x => ctx.ExecuteContext.CustomProperties.Add(x.Key, x.Value));

                    if (profile.ProviderConfigData.HasValue())
                    {
                        var configInfo = ctx.Request.Provider.Value.ConfigurationInfo;
                        if (configInfo != null)
                        {
                            ctx.ExecuteContext.ConfigurationData = XmlHelper.Deserialize(profile.ProviderConfigData, configInfo.ModelType);
                        }
                    }

                    //...

                    if (!ctx.IsPreview && ctx.ExecuteContext.Abort != DataExchangeAbortion.Hard)
                    {
                        if (ctx.IsFileBasedExport)
                        {
                            //...
                        }

                        if (profile.EmailAccountId != 0 && !ctx.Supports(ExportFeatures.CanOmitCompletionMail))
                        {
                            //...
                        }
                    }
                }
                catch (Exception ex)
                {
                    ctx.Log.ErrorsAll(ex);
                    ctx.Result.LastError = ex.ToAllMessages(true);
                }
                finally
                {
                    await Finalize(ctx, cancellationToken);
                }

                if (!ctx.IsPreview && ctx.ExecuteContext.Abort != DataExchangeAbortion.Hard)
                {
                    // Post process entities.
                    if (ctx.EntityIdsLoaded.Any() && ctx.Request.Provider.Value.EntityType == ExportEntityType.Order)
                    {
                        try
                        {
                            await UpdateOrderStatus(ctx, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            ctx.Log.ErrorsAll(ex);
                            ctx.Result.LastError = ex.ToAllMessages(true);
                        }
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            return ctx.Result;
        }

        public async Task<DataExportPreviewResult> PreviewAsync(DataExportRequest request, int pageIndex)
        {
            var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(5.0));
            var ctx = new DataExporterContext(request, true, cancellation.Token);
            var skip = Math.Max(ctx.Request.Profile.Offset, 0) + (pageIndex * PageSize);

            var _ = await Init(ctx, cancellation.Token);

            if (!await HasPermission(ctx))
            {
                throw new SmartException(T("Admin.AccessDenied"));
            }

            var result = new DataExportPreviewResult
            {
                TotalRecords = ctx.StatsPerStore.First().Value.TotalRecords
            };

            switch (ctx.Request.Provider.Value.EntityType)
            {
                case ExportEntityType.Product:
                    {
                        var query = ApplyPaging(GetProductQuery(ctx), skip, PageSize, ctx);
                        var data = await query.ToListAsync(cancellation.Token);
                        data.Each(x => result.Data.Add(ToDynamic(x, ctx)));
                    }
                    break;
                case ExportEntityType.Order:
                    {
                        var query = ApplyPaging(GetOrderQuery(ctx), skip, PageSize, ctx);
                        var data = await query.ToListAsync(cancellation.Token);
                        data.Each(x => result.Data.Add(ToDynamic(x, ctx)));
                    }
                    break;
                case ExportEntityType.Manufacturer:
                    {
                        var query = ApplyPaging(GetManufacturerQuery(ctx), skip, PageSize, ctx);
                        var data = await query.ToListAsync(cancellation.Token);
                        data.Each(x => result.Data.Add(ToDynamic(x, ctx)));
                    }
                    break;
                case ExportEntityType.Category:
                    {
                        var query = ApplyPaging(GetCategoryQuery(ctx), skip, PageSize, ctx);
                        var data = await query.ToListAsync(cancellation.Token);
                        data.Each(x => result.Data.Add(ToDynamic(x, ctx)));
                    }
                    break;
                case ExportEntityType.Customer:
                    {
                        var query = ApplyPaging(GetCustomerQuery(ctx), skip, PageSize, ctx);
                        var data = await query.ToListAsync(cancellation.Token);
                        data.Each(x => result.Data.Add(ToDynamic(x)));
                    }
                    break;
                case ExportEntityType.NewsLetterSubscription:
                    {
                        var query = ApplyPaging(GetNewsletterSubscriptionQuery(ctx), skip, PageSize, ctx);
                        var data = await query.ToListAsync(cancellation.Token);
                        data.Each(x => result.Data.Add(ToDynamic(x, ctx)));
                    }
                    break;
                case ExportEntityType.ShoppingCartItem:
                    {
                        var query = ApplyPaging(GetShoppingCartItemQuery(ctx), skip, PageSize, ctx);
                        var data = await query.ToListAsync(cancellation.Token);
                        data.Each(x => result.Data.Add(ToDynamic(x, ctx)));
                    }
                    break;
            }           

            return result;
        }

        private async Task<List<Store>> Init(DataExporterContext ctx, CancellationToken cancellationToken)
        {
            List<Store> result = null;
            var priceDisplay = ctx.Projection.PriceType ?? _catalogSettings.PriceDisplayType;

            ctx.ContextCurrency = (await _db.Currencies.FindByIdAsync(ctx.Projection.CurrencyId ?? 0, false, cancellationToken)) ?? _services.WorkContext.WorkingCurrency;
            ctx.ContextCustomer = (await _db.Customers.FindByIdAsync(ctx.Projection.CustomerId ?? 0, false, cancellationToken)) ?? _services.WorkContext.CurrentCustomer;
            ctx.ContextLanguage = (await _db.Languages.FindByIdAsync(ctx.Projection.LanguageId ?? 0, false, cancellationToken)) ?? _services.WorkContext.WorkingLanguage;

            ctx.Stores = _services.StoreContext.GetAllStores().ToDictionarySafe(x => x.Id, x => x);
            ctx.Languages = await _db.Languages.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x, cancellationToken);

            // TODO: (mg) (core) should be done later when batch context is available.
            //ctx.MasterLanguageId = await _languageService.GetMasterLanguageIdAsync(ctx.Store.Id);
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
                        query = GetProductQuery(ctx);
                        break;
                    case ExportEntityType.Order:
                        query = GetOrderQuery(ctx);
                        break;
                    case ExportEntityType.Manufacturer:
                        query = GetManufacturerQuery(ctx);
                        break;
                    case ExportEntityType.Category:
                        query = GetCategoryQuery(ctx);
                        break;
                    case ExportEntityType.Customer:
                        query = GetCustomerQuery(ctx);
                        break;
                    case ExportEntityType.NewsLetterSubscription:
                        query = GetNewsletterSubscriptionQuery(ctx);
                        break;
                    case ExportEntityType.ShoppingCartItem:
                        query = GetShoppingCartItemQuery(ctx);
                        break;
                }

                query = ApplyPaging(query, ctx.Request.Profile.Offset, int.MaxValue, ctx);


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

        private async Task Finalize(DataExporterContext ctx, CancellationToken cancellationToken)
        {
            var profile = ctx.Request.Profile;

            try
            {
                if (!ctx.IsPreview && profile.Id != 0)
                {
                    ctx.Result.Files = ctx.Result.Files.OrderBy(x => x.RelatedType).ToList();
                    profile.ResultInfo = XmlHelper.Serialize(ctx.Result);

                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                ctx.Log.ErrorsAll(ex);
            }

            DetachAllEntitiesAndClear(ctx);

            try
            {
                ctx.NewsletterSubscriptions.Clear();
                ctx.ProductTemplates.Clear();
                ctx.CategoryTemplates.Clear();
                ctx.Countries.Clear();
                ctx.Languages.Clear();
                ctx.QuantityUnits.Clear();
                ctx.DeliveryTimes.Clear();
                ctx.Stores.Clear();
                ctx.Translations.Clear();
                ctx.Slugs.Clear();

                ctx.TranslationsPerPage?.Clear();
                ctx.SlugsPerPage?.Clear();

                ctx.Request.CustomData.Clear();

                ctx.ExecuteContext.CustomProperties.Clear();
                ctx.ExecuteContext.Log = null;
                ctx.Log = null;
            }
            catch (Exception ex)
            {
                ctx.Log.ErrorsAll(ex);
            }
        }

        #region Entities

        private static IQueryable<TEntity> ApplyPaging<TEntity>(IQueryable<TEntity> query, int? skip, int take, DataExporterContext ctx)
            where TEntity : BaseEntity
        {
            // Skip used for data preview grid (classic paging) or initial query (offset profile setting).
            var skipValue = skip.GetValueOrDefault();
            if (skipValue == 0 && ctx.LastId == 0)
            {
                skipValue = Math.Max(ctx.Request.Profile.Offset, 0);
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

        private IQueryable<Product> GetProductQuery(DataExporterContext ctx)
        {
            if (ctx.Request.ProductQuery != null)
            {
                return ctx.Request.ProductQuery;
            }

            var f = ctx.Filter;
            var createdFrom = f.CreatedFrom.HasValue ? (DateTime?)_services.DateTimeHelper.ConvertToUtcTime(f.CreatedFrom.Value, _services.DateTimeHelper.CurrentTimeZone) : null;
            var createdTo = f.CreatedTo.HasValue ? (DateTime?)_services.DateTimeHelper.ConvertToUtcTime(f.CreatedTo.Value, _services.DateTimeHelper.CurrentTimeZone) : null;
            var priceFrom = f.PriceMinimum.HasValue ? new Money(f.PriceMinimum.Value, ctx.ContextCurrency) : (Money?)null;
            var priceTo = f.PriceMaximum.HasValue ? new Money(f.PriceMaximum.Value, ctx.ContextCurrency) : (Money?)null;

            var searchQuery = new CatalogSearchQuery()
                .WithCurrency(ctx.ContextCurrency)
                .WithLanguage(ctx.ContextLanguage)
                .HasStoreId(ctx.Request.Profile.PerStore ? ctx.Store.Id : f.StoreId)
                .PriceBetween(priceFrom, priceTo)
                .WithStockQuantity(f.AvailabilityMinimum, f.AvailabilityMaximum)
                .CreatedBetween(createdFrom, createdTo);

            if (f.Visibility.HasValue)
                searchQuery = searchQuery.WithVisibility(f.Visibility.Value);

            if (f.IsPublished.HasValue)
                searchQuery = searchQuery.PublishedOnly(f.IsPublished.Value);

            if (f.ProductType.HasValue)
                searchQuery = searchQuery.IsProductType(f.ProductType.Value);

            if (f.ProductTagId.HasValue)
                searchQuery = searchQuery.WithProductTagIds(f.ProductTagId.Value);

            if (f.WithoutManufacturers.HasValue)
                searchQuery = searchQuery.HasAnyManufacturer(!f.WithoutManufacturers.Value);
            else if (f.ManufacturerId.HasValue)
                searchQuery = searchQuery.WithManufacturerIds(f.FeaturedProducts, f.ManufacturerId.Value);

            if (f.WithoutCategories.HasValue)
                searchQuery = searchQuery.HasAnyCategory(!f.WithoutCategories.Value);
            else if (f.CategoryIds != null && f.CategoryIds.Length > 0)
                searchQuery = searchQuery.WithCategoryIds(f.FeaturedProducts, f.CategoryIds);

            if (ctx.Request.EntitiesToExport.Count > 0)
                searchQuery = searchQuery.WithProductIds(ctx.Request.EntitiesToExport.ToArray());
            else
                searchQuery = searchQuery.WithProductId(f.IdMinimum, f.IdMaximum);

            return _catalogSearchService.PrepareQuery(searchQuery);
        }

        private IQueryable<Order> GetOrderQuery(DataExporterContext ctx)
        {
            var storeId = ctx.Request.Profile.PerStore ? ctx.Store.Id : ctx.Filter.StoreId;
            var startDate = ctx.Filter.CreatedFrom.HasValue 
                ? (DateTime?)_services.DateTimeHelper.ConvertToUtcTime(ctx.Filter.CreatedFrom.Value, _services.DateTimeHelper.CurrentTimeZone) 
                : null;
            var endDate = ctx.Filter.CreatedTo.HasValue 
                ? (DateTime?)_services.DateTimeHelper.ConvertToUtcTime(ctx.Filter.CreatedTo.Value, _services.DateTimeHelper.CurrentTimeZone) 
                : null;

            var query = _db.Orders
                .AsNoTracking()
                .Where(x => x.StoreId == storeId);

            if (ctx.Projection.CustomerId.HasValue)
            {
                // That's actually wrong because it is a projection and not a filter.
                query = query.Where(x => x.CustomerId == ctx.Projection.CustomerId.Value);
            }

            query = query
                .ApplyDateFilter(startDate, endDate)
                .ApplyStatusFilter(ctx.Filter.OrderStatusIds, ctx.Filter.PaymentStatusIds, ctx.Filter.ShippingStatusIds);

            if (ctx.Request.EntitiesToExport.Any())
            {
                query = query.Where(x => ctx.Request.EntitiesToExport.Contains(x.Id));
            }

            return query;
        }

        private IQueryable<Manufacturer> GetManufacturerQuery(DataExporterContext ctx)
        {
            var storeId = ctx.Request.Profile.PerStore ? ctx.Store.Id : 0;
            var query = _db.Manufacturers.AsNoTracking();

            if (ctx.Request.EntitiesToExport.Any())
            {
                query = query.Where(x => ctx.Request.EntitiesToExport.Contains(x.Id));
            }

            return query;
        }

        private IQueryable<Category> GetCategoryQuery(DataExporterContext ctx)
        {
            var storeId = ctx.Request.Profile.PerStore ? ctx.Store.Id : 0;
            var query = _db.Categories.AsNoTracking();

            if (ctx.Request.EntitiesToExport.Any())
            {
                query = query.Where(x => ctx.Request.EntitiesToExport.Contains(x.Id));
            }

            return query;
        }

        private IQueryable<Customer> GetCustomerQuery(DataExporterContext ctx)
        {
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

            if (ctx.Filter.HasSpentAtLeastAmount.HasValue)
            {
                var subQuery =
                    from o in _db.Orders.AsNoTracking()
                    group o by o.CustomerId into grp
                    where grp.Sum(y => y.OrderTotal) >= ctx.Filter.HasSpentAtLeastAmount.Value
                    select grp.Key;

                query = query.Where(x => subQuery.Contains(x.Id));
            }

            if (ctx.Filter.HasPlacedAtLeastOrders.HasValue)
            {
                var subQuery =
                    from o in _db.Orders.AsNoTracking()
                    group o by o.CustomerId into grp
                    where grp.Count() >= ctx.Filter.HasPlacedAtLeastOrders.Value
                    select grp.Key;

                query = query.Where(x => subQuery.Contains(x.Id));
            }

            if (ctx.Request.EntitiesToExport.Any())
            {
                query = query.Where(x => ctx.Request.EntitiesToExport.Contains(x.Id));
            }

            return query;
        }

        private IQueryable<NewsletterSubscription> GetNewsletterSubscriptionQuery(DataExporterContext ctx)
        {
            var storeId = ctx.Request.Profile.PerStore ? ctx.Store.Id : ctx.Filter.StoreId;
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
                query = ctx.Filter.WorkingLanguageId == ctx.MasterLanguageId
                    ? query.Where(x => x.Subscription.WorkingLanguageId == 0 || x.Subscription.WorkingLanguageId == ctx.Filter.WorkingLanguageId)
                    : query.Where(x => x.Subscription.WorkingLanguageId == ctx.Filter.WorkingLanguageId);
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

            return query.Select(x => x.Subscription);
        }

        private IQueryable<ShoppingCartItem> GetShoppingCartItemQuery(DataExporterContext ctx)
        {
            var storeId = ctx.Request.Profile.PerStore ? ctx.Store.Id : ctx.Filter.StoreId;

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

            return query;
        }

        private async Task<IEnumerable<Product>> LoadProducts(DataExporterContext ctx, CancellationToken cancellationToken)
        {
            DetachAllEntitiesAndClear(ctx);

            var stats = ctx.StatsPerStore[ctx.Store.Id];
            if (ctx.LastId >= stats.MaxId)
            {
                return null;
            }

            var query = ApplyPaging(GetProductQuery(ctx), null, PageSize, ctx);
            var products = await query.ToListAsync(cancellationToken);
            if (!products.Any())
            {
                return null;
            }

            var result = new List<Product>();
            Multimap<int, Product> associatedProductsMap = null;

            if (ctx.Projection.NoGroupedProducts)
            {
                var groupedProductIds = products
                    .Where(x => x.ProductType == ProductType.GroupedProduct)
                    .Select(x => x.Id)
                    .ToArray();

                var associatedProducts = await _db.Products
                    .AsNoTracking()
                    .ApplyAssociatedProductsFilter(groupedProductIds, true)
                    .ToListAsync();

                associatedProductsMap = associatedProducts.ToMultimap(x => x.ParentGroupedProductId, x => x);
            }

            foreach (var product in products)
            {
                if (product.ProductType == ProductType.SimpleProduct || product.ProductType == ProductType.BundledProduct)
                {
                    // We use ctx.EntityIdsPerSegment to avoid exporting products multiple times per segment\file (cause of associated products).
                    if (ctx.EntityIdsPerSegment.Add(product.Id))
                    {
                        result.Add(product);
                    }
                }
                else if (product.ProductType == ProductType.GroupedProduct)
                {
                    if (ctx.Projection.NoGroupedProducts)
                    {
                        if (associatedProductsMap.ContainsKey(product.Id))
                        {
                            foreach (var associatedProduct in associatedProductsMap[product.Id])
                            {
                                if (ctx.Projection.OnlyIndividuallyVisibleAssociated && associatedProduct.Visibility == ProductVisibility.Hidden)
                                {
                                    continue;
                                }
                                if (ctx.Filter.IsPublished.HasValue && ctx.Filter.IsPublished.Value != associatedProduct.Published)
                                {
                                    continue;
                                }

                                if (ctx.EntityIdsPerSegment.Add(associatedProduct.Id))
                                {
                                    result.Add(associatedProduct);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (ctx.EntityIdsPerSegment.Add(product.Id))
                        {
                            result.Add(product);
                        }
                    }
                }
            }

            ctx.LastId = products.Last().Id;
            SetProgress(products.Count, ctx);

            return result;
        }

        private async Task<IEnumerable<Order>> LoadOrders(DataExporterContext ctx, CancellationToken cancellationToken)
        {
            DetachAllEntitiesAndClear(ctx);

            var stats = ctx.StatsPerStore[ctx.Store.Id];
            if (ctx.LastId >= stats.MaxId)
            {
                return null;
            }

            var query = ApplyPaging(GetOrderQuery(ctx), null, PageSize, ctx);
            var orders = await query.ToListAsync(cancellationToken);
            if (!orders.Any())
            {
                return null;
            }

            if (ctx.Projection.OrderStatusChange != ExportOrderStatusChange.None)
            {
                ctx.SetLoadedEntityIds(orders.Select(x => x.Id));
            }

            ctx.LastId = orders.Last().Id;
            SetProgress(orders.Count, ctx);

            return orders;
        }

        #endregion

        #region Entities NEW

        private IQueryable<BaseEntity> GetEntitiesQuery(DataExporterContext ctx)
        {
            var f = ctx.Filter;
            var entityType = ctx.Request.Provider.Value.EntityType;
            var storeId = ctx.Request.Profile.PerStore ? ctx.Store.Id : f.StoreId;
            var timeZone = _services.DateTimeHelper.CurrentTimeZone;

            var createdFrom = f.CreatedFrom.HasValue ? (DateTime?)_services.DateTimeHelper.ConvertToUtcTime(f.CreatedFrom.Value, timeZone) : null;
            var createdTo = f.CreatedTo.HasValue ? (DateTime?)_services.DateTimeHelper.ConvertToUtcTime(f.CreatedTo.Value, timeZone) : null;

            var activityFrom = f.LastActivityFrom.HasValue ? (DateTime?)_services.DateTimeHelper.ConvertToUtcTime(f.LastActivityFrom.Value, timeZone) : null;
            var activityTo = f.LastActivityTo.HasValue ? (DateTime?)_services.DateTimeHelper.ConvertToUtcTime(f.LastActivityTo.Value, timeZone) : null;
            IQueryable<BaseEntity> result = null;

            if (entityType == ExportEntityType.Product)
            {
                if (ctx.Request.ProductQuery != null)
                    return ctx.Request.ProductQuery;

                var priceFrom = f.PriceMinimum.HasValue ? new Money(f.PriceMinimum.Value, ctx.ContextCurrency) : (Money?)null;
                var priceTo = f.PriceMaximum.HasValue ? new Money(f.PriceMaximum.Value, ctx.ContextCurrency) : (Money?)null;

                var searchQuery = new CatalogSearchQuery()
                    .WithCurrency(ctx.ContextCurrency)
                    .WithLanguage(ctx.ContextLanguage)
                    .HasStoreId(storeId)
                    .PriceBetween(priceFrom, priceTo)
                    .WithStockQuantity(f.AvailabilityMinimum, f.AvailabilityMaximum)
                    .CreatedBetween(createdFrom, createdTo);

                if (f.Visibility.HasValue)
                    searchQuery = searchQuery.WithVisibility(f.Visibility.Value);

                if (f.IsPublished.HasValue)
                    searchQuery = searchQuery.PublishedOnly(f.IsPublished.Value);

                if (f.ProductType.HasValue)
                    searchQuery = searchQuery.IsProductType(f.ProductType.Value);

                if (f.ProductTagId.HasValue)
                    searchQuery = searchQuery.WithProductTagIds(f.ProductTagId.Value);

                if (f.WithoutManufacturers.HasValue)
                    searchQuery = searchQuery.HasAnyManufacturer(!f.WithoutManufacturers.Value);
                else if (f.ManufacturerId.HasValue)
                    searchQuery = searchQuery.WithManufacturerIds(f.FeaturedProducts, f.ManufacturerId.Value);

                if (f.WithoutCategories.HasValue)
                    searchQuery = searchQuery.HasAnyCategory(!f.WithoutCategories.Value);
                else if (f.CategoryIds != null && f.CategoryIds.Length > 0)
                    searchQuery = searchQuery.WithCategoryIds(f.FeaturedProducts, f.CategoryIds);

                if (ctx.Request.EntitiesToExport.Any())
                    searchQuery = searchQuery.WithProductIds(ctx.Request.EntitiesToExport.ToArray());
                else
                    searchQuery = searchQuery.WithProductId(f.IdMinimum, f.IdMaximum);

                return _catalogSearchService.PrepareQuery(searchQuery);
            }
            else if (entityType == ExportEntityType.Order)
            {
                var query = _db.Orders
                    .AsNoTracking()
                    .Where(x => x.StoreId == storeId);

                // That's actually wrong because it is a projection and not a filter.
                if (ctx.Projection.CustomerId.HasValue)
                    query = query.Where(x => x.CustomerId == ctx.Projection.CustomerId.Value);

                result = query
                    .ApplyDateFilter(createdFrom, createdTo)
                    .ApplyStatusFilter(f.OrderStatusIds, f.PaymentStatusIds, f.ShippingStatusIds);
            }
            else if (entityType == ExportEntityType.Manufacturer)
            {
                result = _db.Manufacturers.AsNoTracking();
            }
            else if (entityType == ExportEntityType.Category)
            {
                result = _db.Categories.AsNoTracking();
            }
            else if (entityType == ExportEntityType.Customer)
            {
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

                if (f.IsActiveCustomer.HasValue)
                    query = query.Where(x => x.Active == f.IsActiveCustomer.Value);

                if (f.IsTaxExempt.HasValue)
                    query = query.Where(x => x.IsTaxExempt == f.IsTaxExempt.Value);

                if (f.CustomerRoleIds?.Any() ?? false)
                    query = query.Where(x => x.CustomerRoleMappings.Select(y => y.CustomerRoleId).Intersect(f.CustomerRoleIds).Any());

                if (f.BillingCountryIds?.Any() ?? false)
                    query = query.Where(x => x.BillingAddress != null && f.BillingCountryIds.Contains(x.BillingAddress.Id));

                if (f.ShippingCountryIds?.Any() ?? false)
                    query = query.Where(x => x.ShippingAddress != null && f.ShippingCountryIds.Contains(x.ShippingAddress.Id));

                if (activityFrom.HasValue)
                    query = query.Where(x => activityFrom <= x.LastActivityDateUtc);

                if (activityTo.HasValue)
                    query = query.Where(x => activityTo >= x.LastActivityDateUtc);

                if (f.HasSpentAtLeastAmount.HasValue)
                {
                    var subQuery =
                        from o in _db.Orders.AsNoTracking()
                        group o by o.CustomerId into grp
                        where grp.Sum(y => y.OrderTotal) >= f.HasSpentAtLeastAmount.Value
                        select grp.Key;

                    query = query.Where(x => subQuery.Contains(x.Id));
                }

                if (ctx.Filter.HasPlacedAtLeastOrders.HasValue)
                {
                    var subQuery =
                        from o in _db.Orders.AsNoTracking()
                        group o by o.CustomerId into grp
                        where grp.Count() >= f.HasPlacedAtLeastOrders.Value
                        select grp.Key;

                    query = query.Where(x => subQuery.Contains(x.Id));
                }

                result = query;
            }
            else if (entityType == ExportEntityType.NewsLetterSubscription)
            {
                var query =
                    from ns in _db.NewsletterSubscriptions.AsNoTracking()
                    join c in _db.Customers.AsNoTracking() on ns.Email equals c.Email into customers
                    from c in customers.DefaultIfEmpty()
                    select new NewsletterSubscriber
                    {
                        Subscription = ns,
                        Customer = c
                    };

                if (storeId > 0)
                    query = query.Where(x => x.Subscription.StoreId == storeId);

                if (f.IsActiveSubscriber.HasValue)
                    query = query.Where(x => x.Subscription.Active == f.IsActiveSubscriber.Value);

                if (f.WorkingLanguageId.GetValueOrDefault() > 0)
                {
                    query = f.WorkingLanguageId == ctx.MasterLanguageId
                        ? query.Where(x => x.Subscription.WorkingLanguageId == 0 || x.Subscription.WorkingLanguageId == f.WorkingLanguageId)
                        : query.Where(x => x.Subscription.WorkingLanguageId == f.WorkingLanguageId);
                }

                if (createdFrom.HasValue)
                    query = query.Where(x => createdFrom <= x.Subscription.CreatedOnUtc);

                if (createdTo.HasValue)
                    query = query.Where(x => createdTo >= x.Subscription.CreatedOnUtc);

                if (f.CustomerRoleIds?.Any() ?? false)
                    query = query.Where(x => x.Customer.CustomerRoleMappings.Select(y => y.CustomerRoleId).Intersect(f.CustomerRoleIds).Any());

                result = query.Select(x => x.Subscription);
            }
            else if (entityType == ExportEntityType.ShoppingCartItem)
            {
                var query = _db.ShoppingCartItems
                    .AsNoTracking()
                    .Include(x => x.Customer)
                    .ThenInclude(x => x.CustomerRoleMappings)
                    .ThenInclude(x => x.CustomerRole)
                    .Include(x => x.Product)
                    .AsQueryable();

                if (storeId > 0)
                    query = query.Where(x => x.StoreId == storeId);

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

                if (f.IsActiveCustomer.HasValue)
                    query = query.Where(x => x.Customer.Active == f.IsActiveCustomer.Value);

                if (f.IsTaxExempt.HasValue)
                    query = query.Where(x => x.Customer.IsTaxExempt == f.IsTaxExempt.Value);

                if (f.CustomerRoleIds?.Any() ?? false)
                    query = query.Where(x => x.Customer.CustomerRoleMappings.Select(y => y.CustomerRoleId).Intersect(f.CustomerRoleIds).Any());

                if (activityFrom.HasValue)
                    query = query.Where(x => activityFrom <= x.Customer.LastActivityDateUtc);

                if (activityTo.HasValue)
                    query = query.Where(x => activityTo >= x.Customer.LastActivityDateUtc);

                if (createdFrom.HasValue)
                    query = query.Where(x => createdFrom <= x.CreatedOnUtc);

                if (createdTo.HasValue)
                    query = query.Where(x => createdTo >= x.CreatedOnUtc);

                if (ctx.Projection.NoBundleProducts)
                    query = query.Where(x => x.Product.ProductTypeId != (int)ProductType.BundledProduct);
                else
                    query = query.Where(x => x.BundleItemId == null);

                result = query;
            }
            else
            {
                throw new SmartException($"Unsupported entity type '{entityType}'.");
            }

            if (ctx.Request.EntitiesToExport.Any())
            {
                result = result.Where(x => ctx.Request.EntitiesToExport.Contains(x.Id));
            }

            return result;
        }

        private Task<IEnumerable<BaseEntity>> LoadEntities<TEntity>(DataExporterContext ctx, CancellationToken cancellationToken)
        {
            DetachAllEntitiesAndClear(ctx);

            var stats = ctx.StatsPerStore[ctx.Store.Id];
            if (ctx.LastId >= stats.MaxId)
            {
                return null;
            }

            var query = GetEntitiesQuery(ctx);



            return null;
        }

        #endregion

        #region Utilities

        private async Task UpdateOrderStatus(DataExporterContext ctx, CancellationToken cancellationToken)
        {
            var num = 0;
            int? newOrderStatusId = null;

            if (ctx.Projection.OrderStatusChange == ExportOrderStatusChange.Processing)
            {
                newOrderStatusId = (int)OrderStatus.Processing;
            }
            else if (ctx.Projection.OrderStatusChange == ExportOrderStatusChange.Complete)
            {
                newOrderStatusId = (int)OrderStatus.Complete;
            }

            if (newOrderStatusId.HasValue)
            {
                foreach (var chunk in ctx.EntityIdsLoaded.Slice(200))
                {
                    num += await _db.Orders
                        .Where(x => chunk.Contains(x.Id))
                        .BatchUpdateAsync(x => new Order { OrderStatusId = newOrderStatusId.Value }, cancellationToken);
                }

                ctx.Log.Info($"Updated order status for {num} order(s).");
            }
        }

        private async Task<LocalizedPropertyCollection> CreateTranslationCollection(string keyGroup, IEnumerable<BaseEntity> entities)
        {
            if (entities == null || !entities.Any())
            {
                return new LocalizedPropertyCollection(keyGroup, null, Enumerable.Empty<LocalizedProperty>());
            }

            return await _localizedEntityService.GetLocalizedPropertyCollectionAsync(keyGroup, entities.Select(x => x.Id).Distinct().ToArray());
        }

        private async Task<UrlRecordCollection> CreateUrlRecordCollection(string entityName, IEnumerable<BaseEntity> entities)
        {
            if (entities == null || !entities.Any())
            {
                return new UrlRecordCollection(entityName, null, Enumerable.Empty<UrlRecord>());
            }

            return await _urlService.GetUrlRecordCollectionAsync(entityName, null, entities.Select(x => x.Id).Distinct().ToArray());
        }

        private async Task<bool> HasPermission(DataExporterContext ctx)
        {
            if (ctx.Request.HasPermission)
            {
                return true;
            }

            var customer = _services.WorkContext.CurrentCustomer;

            if (customer.SystemName == SystemCustomerNames.BackgroundTask)
            {
                return true;
            }

            return await _services.Permissions.AuthorizeAsync(Permissions.Configuration.Export.Execute);
        }

        private void DetachAllEntitiesAndClear(DataExporterContext ctx)
        {
            try
            {
                ctx.AssociatedProductBatchContext?.Clear();

                if (ctx.ProductBatchContext != null)
                {
                    _db.DetachEntities(x =>
                    {
                        return x is Product || x is Discount || x is ProductVariantAttributeCombination || x is ProductVariantAttribute || x is ProductVariantAttributeValue || x is ProductAttribute ||
                               x is MediaFile || x is ProductBundleItem || x is ProductBundleItemAttributeFilter || x is ProductCategory || x is ProductManufacturer || x is Category || x is Manufacturer ||
                               x is ProductMediaFile || x is ProductTag || x is ProductSpecificationAttribute || x is SpecificationAttributeOption || x is SpecificationAttribute || x is TierPrice || x is ProductReview ||
                               x is ProductReviewHelpfulness || x is DeliveryTime || x is QuantityUnit || x is Download || x is MediaStorage || x is GenericAttribute || x is UrlRecord;
                    });

                    ctx.ProductBatchContext.Clear();
                }

                if (ctx.OrderBatchContext != null)
                {
                    _db.DetachEntities(x =>
                    {
                        return x is Order || x is Address || x is GenericAttribute || x is Customer ||
                               x is OrderItem || x is RewardPointsHistory || x is Shipment || x is ProductVariantAttributeCombination;
                    });

                    ctx.OrderBatchContext.Clear();
                }

                if (ctx.CategoryBatchContext != null)
                {
                    _db.DetachEntities(x =>
                    {
                        return x is Category || x is MediaFile || x is ProductCategory;
                    });

                    ctx.CategoryBatchContext.Clear();
                }

                if (ctx.ManufacturerBatchContext != null)
                {
                    _db.DetachEntities(x =>
                    {
                        return x is Manufacturer || x is MediaFile || x is ProductManufacturer;
                    });

                    ctx.ManufacturerBatchContext.Clear();
                }

                if (ctx.CustomerBatchContext != null)
                {
                    _db.DetachEntities(x =>
                    {
                        return x is Customer || x is GenericAttribute || x is CustomerContent;
                    });

                    ctx.CustomerBatchContext.Clear();
                }

                switch (ctx.Request.Provider.Value.EntityType)
                {
                    case ExportEntityType.ShoppingCartItem:
                        _db.DetachEntities(x =>
                        {
                            return x is ShoppingCartItem || x is Customer || x is Product || x is ProductVariantAttributeCombination;
                        });
                        break;
                    case ExportEntityType.NewsLetterSubscription:
                        _db.DetachEntities(x =>
                        {
                            return x is NewsletterSubscription || x is Customer;
                        });
                        break;
                }
            }
            catch (Exception ex)
            {
                ctx.Log.Warn(ex, "Detaching entities failed.");
            }
        }

        private static void SetProgress(string message, DataExporterContext ctx)
        {
            try
            {
                if (!ctx.IsPreview && message.HasValue())
                {
                    ctx.Request.ProgressValueSetter.Invoke(0, 0, message);
                }
            }
            catch { }
        }

        private static void SetProgress(int loadedRecords, DataExporterContext ctx)
        {
            try
            {
                if (!ctx.IsPreview && loadedRecords > 0)
                {
                    var totalRecords = ctx.StatsPerStore.Sum(x => x.Value.TotalRecords);

                    if (ctx.Request.Profile.Limit > 0 && totalRecords > ctx.Request.Profile.Limit)
                    {
                        totalRecords = ctx.Request.Profile.Limit;
                    }

                    ctx.RecordCount = Math.Min(ctx.RecordCount + loadedRecords, totalRecords);
                    var msg = ctx.ProgressInfo.FormatInvariant(ctx.RecordCount.ToString("N0"), totalRecords.ToString("N0"));
                    ctx.Request.ProgressValueSetter.Invoke(ctx.RecordCount, totalRecords, msg);
                }
            }
            catch { }
        }

        #endregion
    }
}
