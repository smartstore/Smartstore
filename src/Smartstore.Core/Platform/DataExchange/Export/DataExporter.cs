using System.IO.Compression;
using System.Net.Http;
using System.Security;
using Microsoft.AspNetCore.Mvc;
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
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Core.Data;
using Smartstore.Core.DataExchange.Export.Deployment;
using Smartstore.Core.DataExchange.Export.Internal;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Data.Caching;
using Smartstore.Engine.Modularity;
using Smartstore.IO;
using Smartstore.Net.Mail;
using Smartstore.Threading;
using Smartstore.Utilities;

namespace Smartstore.Core.DataExchange.Export
{
    public partial class DataExporter : IDataExporter
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IWorkContext _workContext;
        private readonly IExportProfileService _exportProfileService;
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
        private readonly IMailService _mailService;
        private readonly Lazy<IUrlHelper> _urlHelper;
        private readonly ProductUrlHelper _productUrlHelper;
        private readonly ITaxCalculator _taxCalculator;
        private readonly IProviderManager _providerManager;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly CatalogSettings _catalogSettings;
        private readonly PriceSettings _priceSettings;
        private readonly MediaSettings _mediaSettings;
        private readonly SeoSettings _seoSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly ContactDataSettings _contactDataSettings;

        public DataExporter(
            SmartDbContext db,
            ICommonServices services,
            IWorkContext workContext,
            IExportProfileService exportProfileService,
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
            IMailService mailService,
            Lazy<IUrlHelper> urlHelper,
            ProductUrlHelper productUrlHelper,
            ITaxCalculator taxCalculator,
            IProviderManager providerManager,
            IHttpClientFactory httpClientFactory,
            CatalogSettings catalogSettings,
            PriceSettings priceSettings,
            MediaSettings mediaSettings,
            SeoSettings seoSettings,
            CustomerSettings customerSettings,
            ContactDataSettings contactDataSettings)
        {
            _db = db;
            _services = services;
            _workContext = workContext;
            _exportProfileService = exportProfileService;
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
            _mailService = mailService;
            _urlHelper = urlHelper;
            _productUrlHelper = productUrlHelper;
            _taxCalculator = taxCalculator;
            _providerManager = providerManager;
            _httpClientFactory = httpClientFactory;

            _catalogSettings = catalogSettings;
            _priceSettings = priceSettings;
            _mediaSettings = mediaSettings;
            _seoSettings = seoSettings;
            _customerSettings = customerSettings;
            _contactDataSettings = contactDataSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        /// <summary>
        /// The name of the wwwroot subfolder where export files are to be exported to be publicly accessible.
        /// </summary>
        public static string PublicDirectoryName => "exchange";

        /// <summary>
        /// The page size for loading data from database during export.
        /// </summary>
        public static int PageSize => 100;

        public virtual async Task<DataExportResult> ExportAsync(DataExportRequest request, CancellationToken cancelToken = default)
        {
            Guard.NotNull(request);
            Guard.NotNull(request.Profile);
            Guard.NotNull(request.Provider?.Value);

            var ctx = CreateExporterContext(request, false, cancelToken);
            var profile = request.Profile;

            if (!profile.Enabled)
            {
                return ctx.Result;
            }

            var lockKey = $"dataexporter:profile:{profile.Id}";
            if (AsyncLock.IsLockHeld(lockKey))
            {
                ctx.Result.LastError = $"The execution of the profile \"{profile.Name.NaIfEmpty()}\" (ID {profile.Id}) is locked.";
                return ctx.Result;
            }

            // The export directory is the "Content" subfolder. ZIP and LOG file are in the parent folder.
            var dir = await _exportProfileService.GetExportDirectoryAsync(profile, "Content", true);
            var logFile = await dir.Parent.GetFileAsync("log.txt");
            var zipFile = await dir.Parent.GetFileAsync(PathUtility.SanitizeFileName(dir.Parent.Name) + ".zip");

            await dir.FileSystem.TryDeleteFileAsync(logFile);
            await dir.FileSystem.TryDeleteFileAsync(zipFile);
            dir.FileSystem.ClearDirectory(dir, false, TimeSpan.Zero);

            using (await AsyncLock.KeyedAsync(lockKey, null, cancelToken))
            using (var logger = new TraceLogger(logFile, false))
            {
                try
                {
                    ctx.ExportDirectory = ctx.ExecuteContext.ExportDirectory = dir;
                    ctx.Log = ctx.ExecuteContext.Log = logger;
                    ctx.ZipFile = zipFile;

                    if (request?.Provider?.Value?.FileExtension?.HasValue() ?? false)
                    {
                        ctx.Result.ExportDirectory = dir;
                    }

                    using (var scope = new DbContextScope(_db, autoDetectChanges: false, forceNoTracking: true))
                    {
                        var stores = await Init(ctx);
                        await CheckPermission(ctx);

                        ctx.ExecuteContext.Profile = CreateDynamic(profile);
                        ctx.ExecuteContext.Language = CreateDynamic(_workContext.WorkingLanguage);
                        ctx.ExecuteContext.Customer = ToDynamic(_workContext.CurrentCustomer);
                        ctx.ExecuteContext.Currency = ToDynamic(_workContext.WorkingCurrency, ctx);

                        foreach (var store in stores)
                        {
                            if (ctx.ExecuteContext.Abort != DataExchangeAbortion.None)
                                break;

                            ctx.Store = store;
                            ctx.LastId = 0;

                            await InternalExport(ctx);
                        }
                    }

                    if (!ctx.IsPreview && ctx.ExecuteContext.Abort != DataExchangeAbortion.Hard)
                    {
                        if (ctx.IsFileBasedExport)
                        {
                            if (profile.CreateZipArchive)
                            {
                                ZipFile.CreateFromDirectory(ctx.ExportDirectory.PhysicalPath, ctx.ZipFile.PhysicalPath, CompressionLevel.Fastest, false);
                            }

                            if (profile.Deployments.Any(x => x.Enabled))
                            {
                                await SetProgress(T("Common.Publishing"), ctx);

                                var allDeploymentsSucceeded = await Deploy(ctx);
                                if (allDeploymentsSucceeded && profile.Cleanup)
                                {
                                    ctx.Log.Info("Cleaning up export folder.");
                                    dir.FileSystem.ClearDirectory(dir, false, TimeSpan.Zero);
                                }
                            }
                        }

                        if (profile.EmailAccountId != 0 && !ctx.Supports(ExportFeatures.CanOmitCompletionMail))
                        {
                            await SendCompletionEmail(ctx);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ctx?.Log?.ErrorsAll(ex);
                    ctx.Result.LastError = ex.ToAllMessages(true);
                }
                finally
                {
                    await Finalize(ctx);
                }

                if (!ctx.IsPreview && ctx.ExecuteContext.Abort != DataExchangeAbortion.Hard)
                {
                    // Post process entities.
                    if (ctx.EntityIdsLoaded.Any() && ctx.Request.Provider.Value.EntityType == ExportEntityType.Order)
                    {
                        try
                        {
                            await UpdateOrderStatus(ctx, cancelToken);
                        }
                        catch (Exception ex)
                        {
                            ctx.Log.ErrorsAll(ex);
                            ctx.Result.LastError = ex.ToAllMessages(true);
                        }
                    }
                }
            }

            cancelToken.ThrowIfCancellationRequested();

            return ctx.Result;
        }

        public virtual async Task<DataExportPreviewResult> PreviewAsync(DataExportRequest request, int pageIndex, int pageSize)
        {
            Guard.NotNull(request);
            Guard.NotNull(request.Profile);
            Guard.NotNegative(pageIndex);
            Guard.NotNegative(pageSize);

            var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(5.0));
            var ctx = CreateExporterContext(request, true, cancellation.Token);

            var limit = Math.Max(request.Profile.Limit, 0);
            var take = limit > 0 && limit < pageSize ? limit : pageSize;
            var skip = Math.Max(ctx.Request.Profile.Offset, 0) + (pageIndex * take);

            var _ = await Init(ctx);
            await CheckPermission(ctx);

            var result = new DataExportPreviewResult
            {
                TotalRecords = ctx.ShopMetadata.First().Value.TotalRecords
            };

            var query = await GetEntitiesQuery(ctx);
            query = ApplyPaging(query, skip, take, ctx);
            var data = await query.ToListAsync(cancellation.Token);

            switch (ctx.Request.Provider.Value.EntityType)
            {
                case ExportEntityType.Product:
                    await data.Cast<Product>().EachAsync(async x => result.Data.Add(await ToDynamic(x, ctx)));
                    break;
                case ExportEntityType.Order:
                    data.Cast<Order>().Each(x => result.Data.Add(ToDynamic(x, ctx)));
                    break;
                case ExportEntityType.Manufacturer:
                    data.Cast<Manufacturer>().Each(x => result.Data.Add(ToDynamic(x, ctx)));
                    break;
                case ExportEntityType.Category:
                    data.Cast<Category>().Each(x => result.Data.Add(ToDynamic(x, ctx)));
                    break;
                case ExportEntityType.Customer:
                    data.Cast<Customer>().Each(x => result.Data.Add(ToDynamic(x)));
                    break;
                case ExportEntityType.NewsletterSubscription:
                    data.Cast<NewsletterSubscription>().Each(x => result.Data.Add(DataExporter.ToDynamic(x, ctx)));
                    break;
                case ExportEntityType.ShoppingCartItem:
                    await data.Cast<ShoppingCartItem>().EachAsync(async x => result.Data.Add(await ToDynamic(x, ctx)));
                    break;
            }

            return result;
        }

        private async Task<List<Store>> Init(DataExporterContext ctx)
        {
            List<Store> result = null;
            var ct = ctx.CancelToken;
            var provider = ctx.Request.Provider.Value;
            var currency = await _db.Currencies.FindByIdAsync(ctx.Projection.CurrencyId ?? 0, false, ct);
            var language = await _db.Languages.FindByIdAsync(ctx.Projection.LanguageId ?? 0, false, ct);
            var customer = await _db.Customers
                .IncludeCustomerRoles()
                .FindByIdAsync(ctx.Projection.CustomerId ?? 0, true, ct);

            if (currency != null)
            {
                _workContext.WorkingCurrency = currency;
            }
            if (language != null)
            {
                _workContext.WorkingLanguage = language;
            }
            if (customer != null)
            {
                // Do not check the permissions of projected customers. This complicates the configuration for the admin,
                // e.g. if prices are to be exported as they are seen by guests.
                ctx.Request.HasPermission = true;
                _workContext.CurrentCustomer = customer;
            }

            ctx.Stores = _services.StoreContext.GetAllStores().ToDictionarySafe(x => x.Id, x => x);
            ctx.Languages = await _db.Languages.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x, ct);

            if (!ctx.IsPreview)
            {
                ctx.DeliveryTimes = await _db.DeliveryTimes.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x, ct);
                ctx.QuantityUnits = await _db.QuantityUnits.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x, ct);
                ctx.PriceLabels = await _db.PriceLabels.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x, ct);
                ctx.ProductTemplates = await _db.ProductTemplates.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.ViewPath, ct);
                ctx.CategoryTemplates = await _db.CategoryTemplates.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.ViewPath, ct);

                if (provider.EntityType == ExportEntityType.Product ||
                    provider.EntityType == ExportEntityType.Order ||
                    provider.EntityType == ExportEntityType.Customer)
                {
                    ctx.Countries = await _db.Countries.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x, ct);
                }
                if (provider.EntityType == ExportEntityType.Order || provider.EntityType == ExportEntityType.Customer)
                {
                    ctx.StateProvinces = await _db.StateProvinces.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x, ct);
                }
                if (provider.EntityType == ExportEntityType.Customer)
                {
                    var subscriptionEmails = await _db.NewsletterSubscriptions
                        .AsNoTracking()
                        .Where(x => x.Active)
                        .Select(x => x.Email)
                        .Distinct()
                        .ToListAsync(ct);

                    ctx.NewsletterSubscriptions = new HashSet<string>(subscriptionEmails, StringComparer.OrdinalIgnoreCase);
                }

                // Get all translations and slugs for global entities in one go.
                ctx.Translations[nameof(Currency)] = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(nameof(Currency), null);
                ctx.Translations[nameof(Country)] = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(nameof(Country), null);
                ctx.Translations[nameof(StateProvince)] = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(nameof(StateProvince), null);
                ctx.Translations[nameof(DeliveryTime)] = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(nameof(DeliveryTime), null);
                ctx.Translations[nameof(QuantityUnit)] = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(nameof(QuantityUnit), null);
                ctx.Translations[nameof(Manufacturer)] = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(nameof(Manufacturer), null);
                ctx.Translations[nameof(Category)] = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(nameof(Category), null);

                ctx.UrlRecords[nameof(Category)] = await _urlService.GetUrlRecordCollectionAsync(nameof(Category), null, null);
                ctx.UrlRecords[nameof(Manufacturer)] = await _urlService.GetUrlRecordCollectionAsync(nameof(Manufacturer), null, null);
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

            // Get some metadata for each store.
            foreach (var store in result)
            {
                ctx.Store = store;

                var query = await GetEntitiesQuery(ctx);
                query = ApplyPaging(query, ctx.Request.Profile.Offset, int.MaxValue, ctx);

                ctx.ShopMetadata[store.Id] = new ShopMetadata
                {
                    TotalRecords = query.Count(),
                    MaxId = !ctx.IsPreview ? (query.Max(x => (int?)x.Id) ?? 0) : 0,
                    MasterLanguageId = await _languageService.GetMasterLanguageIdAsync(store.Id)
                };
            }

            return result;
        }

        private async Task InternalExport(DataExporterContext ctx)
        {
            var context = ctx.ExecuteContext;
            var dir = context.ExportDirectory;
            var profile = ctx.Request.Profile;
            var provider = ctx.Request.Provider.Value;
            var dataExchangeSettings = await _services.SettingFactory.LoadSettingsAsync<DataExchangeSettings>(ctx.Store.Id);
            var publicDeployment = profile.Deployments.FirstOrDefault(x => x.DeploymentType == ExportDeploymentType.PublicFolder);
            var fileExtension = provider.FileExtension.NullEmpty()?.ToLower()?.EnsureStartsWith(".") ?? string.Empty;

            context.FileIndex = 0;
            context.Store = DataExporter.ToDynamic(ctx.Store);
            context.MaxFileNameLength = dataExchangeSettings.MaxFileNameLength;
            context.HasPublicDeployment = publicDeployment != null;
            context.PublicDirectory = await _exportProfileService.GetDeploymentDirectoryAsync(publicDeployment, true);
            context.PublicDirectoryUrl = await _exportProfileService.GetDeploymentDirectoryUrlAsync(publicDeployment, ctx.Store);

            ctx.Log.Info(CreateLogHeader(ctx));

            using var segmenter = CreateSegmenter(ctx);

            if (segmenter == null)
            {
                throw new NotSupportedException($"Unsupported entity type '{provider.EntityType}'.");
            }

            if (segmenter.TotalRecords <= 0)
            {
                ctx.Log.Info("There are no records to export.");
            }

            while (context.Abort == DataExchangeAbortion.None && segmenter.HasData)
            {
                IFile file = null;

                segmenter.RecordPerSegmentCount = 0;
                context.RecordsSucceeded = 0;

                if (ctx.IsFileBasedExport)
                {
                    context.FileIndex += 1;
                    context.FileName = profile.ResolveFileNamePattern(ctx.Store, context.FileIndex, context.MaxFileNameLength) + fileExtension;
                    file = await dir.GetFileAsync(context.FileName);

                    if (profile.ExportRelatedData && ctx.Supports(ExportFeatures.UsesRelatedDataUnits))
                    {
                        context.ExtraDataUnits.AddRange(await GetDataUnitsForRelatedEntities(ctx));
                    }
                }

                if (await CallExportProvider("Execute", file, ctx))
                {
                    if (ctx.IsFileBasedExport && file.Exists)
                    {
                        ctx.Result.Files.Add(new DataExportResult.ExportFileInfo
                        {
                            StoreId = ctx.Store.Id,
                            FileName = context.FileName
                        });
                    }
                }

                ctx.EntityIdsPerSegment.Clear();

                if (context.IsMaxFailures)
                {
                    ctx.Log.Warn("Export aborted. The maximum number of failures has been reached.");
                }
                if (ctx.CancelToken.IsCancellationRequested)
                {
                    ctx.Log.Warn("Export aborted. A cancellation has been requested.");
                }
            }

            if (context.Abort != DataExchangeAbortion.Hard)
            {
                var calledExecuted = false;

                foreach (var dataUnit in context.ExtraDataUnits)
                {
                    context.DataStreamId = dataUnit.Id;

                    var success = true;
                    var file = dataUnit.FileName.HasValue()
                        ? await dir.GetFileAsync(dataUnit.FileName)
                        : null;

                    if (!dataUnit.RelatedType.HasValue)
                    {
                        // Data unit added by provider.
                        calledExecuted = true;
                        success = await CallExportProvider("OnExecuted", file, ctx);
                    }

                    if (success && ctx.IsFileBasedExport && dataUnit.DisplayInFileDialog && file.Exists)
                    {
                        // Save info about extra file.
                        ctx.Result.Files.Add(new DataExportResult.ExportFileInfo
                        {
                            StoreId = ctx.Store.Id,
                            FileName = dataUnit.FileName,
                            Label = dataUnit.Label,
                            RelatedType = dataUnit.RelatedType
                        });
                    }
                }

                if (!calledExecuted)
                {
                    // Always call OnExecuted.
                    await CallExportProvider("OnExecuted", null, ctx);
                }
            }

            context.ExtraDataUnits.Clear();
        }

        private async Task Finalize(DataExporterContext ctx)
        {
            var profile = ctx.Request.Profile;

            try
            {
                if (!ctx.IsPreview && profile.Id != 0)
                {
                    ctx.Result.Files = ctx.Result.Files.OrderBy(x => x.RelatedType).ToList();
                    profile.ResultInfo = XmlHelper.Serialize(ctx.Result);

                    await _db.SaveChangesAsync(ctx.CancelToken);
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
                ctx.StateProvinces.Clear();
                ctx.Languages.Clear();
                ctx.QuantityUnits.Clear();
                ctx.DeliveryTimes.Clear();
                ctx.PriceLabels.Clear();
                ctx.Stores.Clear();
                ctx.Translations.Clear();
                ctx.UrlRecords.Clear();

                ctx.TranslationsPerPage?.Clear();
                ctx.UrlRecordsPerPage?.Clear();

                ctx.Request.CustomData.Clear();
                ctx.ExecuteContext.CustomProperties.Clear();

                ctx.ExecuteContext.Log = null;
                ctx.Log = null;
            }
            catch (Exception ex)
            {
                ctx?.Log?.ErrorsAll(ex);
            }
        }

        #region Entities

        private async Task<IEnumerable<TEntity>> LoadEntities<TEntity>(DataExporterContext ctx)
        {
            DetachAllEntitiesAndClear(ctx);

            List<Product> productEntities = null;
            var entityType = ctx.Request.Provider.Value.EntityType;

            if (ctx.LastId >= ctx.ShopMetadata[ctx.Store.Id].MaxId)
            {
                // End of data reached.
                return null;
            }

            var query = await GetEntitiesQuery(ctx);
            query = ApplyPaging(query, null, PageSize, ctx);
            var entities = await query.ToListAsync(ctx.CancelToken);

            if (entities.Count == 0)
            {
                return null;
            }

            // Some entities need extra treatment.
            if (entityType == ExportEntityType.Product)
            {
                productEntities = new List<Product>();

                Multimap<int, Product> associatedProductsMap = null;
                var products = entities.Cast<Product>();

                if (ctx.Projection.NoGroupedProducts)
                {
                    var groupedProductIds = products
                        .Where(x => x.ProductType == ProductType.GroupedProduct)
                        .Select(x => x.Id)
                        .ToArray();

                    var associatedProducts = await _db.Products
                        .AsNoTracking()
                        .ApplyAssociatedProductsFilter(groupedProductIds, true)
                        .ToListAsync(ctx.CancelToken);

                    associatedProductsMap = associatedProducts.ToMultimap(x => x.ParentGroupedProductId, x => x);
                }

                foreach (var product in products)
                {
                    if (product.ProductType == ProductType.SimpleProduct || product.ProductType == ProductType.BundledProduct)
                    {
                        // We use ctx.EntityIdsPerSegment to avoid exporting products multiple times per segment\file (cause of associated products).
                        if (ctx.EntityIdsPerSegment.Add(product.Id))
                            productEntities.Add(product);
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
                                        continue;
                                    if (ctx.Filter.IsPublished.HasValue && ctx.Filter.IsPublished.Value != associatedProduct.Published)
                                        continue;

                                    if (ctx.EntityIdsPerSegment.Add(associatedProduct.Id))
                                        productEntities.Add(associatedProduct);
                                }
                            }
                        }
                        else if (ctx.EntityIdsPerSegment.Add(product.Id))
                        {
                            productEntities.Add(product);
                        }
                    }
                }
            }
            else if (entityType == ExportEntityType.Order && ctx.Projection.OrderStatusChange != ExportOrderStatusChange.None)
            {
                ctx.SetLoadedEntityIds(entities.Select(x => x.Id));
            }

            ctx.LastId = entities.Last().Id;
            await SetProgress(entities.Count, ctx);

            return productEntities?.Cast<TEntity>() ?? entities.Cast<TEntity>();
        }

        private async Task<IQueryable<BaseEntity>> GetEntitiesQuery(DataExporterContext ctx)
        {
            var f = ctx.Filter;
            var entityType = ctx.Request.Provider.Value.EntityType;
            var storeId = ctx.Request.Profile.PerStore ? ctx.Store.Id : f.StoreId;
            IQueryable<int> customerIdsByRolesQuery = null;
            IQueryable<BaseEntity> result = null;

            if (!f.CustomerRoleIds.IsNullOrEmpty())
            {
                customerIdsByRolesQuery = _db.CustomerRoleMappings
                    .AsNoTracking()
                    .Where(x => f.CustomerRoleIds.Contains(x.CustomerRoleId))
                    .Select(x => x.CustomerId);
            }

            if (entityType == ExportEntityType.Product)
            {
                if (ctx.Request.ProductQuery != null)
                    return ctx.Request.ProductQuery;

                var searchQuery = new CatalogSearchQuery()
                    .WithCurrency(_workContext.WorkingCurrency)
                    .WithLanguage(_workContext.WorkingLanguage)
                    .HasStoreId(storeId)
                    .PriceBetween(f.PriceMinimum, f.PriceMaximum)
                    .WithStockQuantity(f.AvailabilityMinimum, f.AvailabilityMaximum)
                    .CreatedBetween(f.CreatedFrom, f.CreatedTo);

                if (f.Visibility.HasValue)
                    searchQuery = searchQuery.WithVisibility(f.Visibility.Value);

                if (f.IsPublished.HasValue)
                    searchQuery = searchQuery.PublishedOnly(f.IsPublished.Value);

                if (f.ProductType.HasValue)
                    searchQuery = searchQuery.IsProductType(f.ProductType.Value);

                if (f.ProductTagId.HasValue)
                    searchQuery = searchQuery.WithProductTagIds(f.ProductTagId.Value);

                if (ctx.Request.EntitiesToExport.Count > 0)
                    searchQuery = searchQuery.WithProductIds(ctx.Request.EntitiesToExport.ToArray());
                else
                    searchQuery = searchQuery.WithProductId(f.IdMinimum, f.IdMaximum);

                if (f.WithoutManufacturers.HasValue)
                    searchQuery = searchQuery.HasAnyManufacturer(!f.WithoutManufacturers.Value);
                else if (f.ManufacturerId.HasValue)
                    searchQuery = searchQuery.WithManufacturerIds(f.FeaturedProducts, f.ManufacturerId.Value);

                if (f.WithoutCategories.HasValue)
                {
                    searchQuery = searchQuery.HasAnyCategory(!f.WithoutCategories.Value);
                }
                else if (!f.IncludeSubCategories && !f.CategoryIds.IsNullOrEmpty())
                {
                    searchQuery = searchQuery.WithCategoryIds(f.FeaturedProducts, f.CategoryIds);
                }
                else if (f.IncludeSubCategories && f.CategoryId.GetValueOrDefault() != 0)
                {
                    var categoryTree = await _categoryService.GetCategoryTreeAsync(0, true);
                    var treePath = categoryTree.SelectNodeById(f.CategoryId.Value)?.GetTreePath();

                    searchQuery = searchQuery.WithCategoryTreePath(treePath, f.FeaturedProducts);
                }

                return _catalogSearchService.PrepareQuery(searchQuery)
                    .AsNoTracking()
                    .AsNoCaching();
            }
            else if (entityType == ExportEntityType.Order)
            {
                var query = _db.Orders.AsNoTracking();

                if (storeId > 0)
                    query = query.Where(x => x.StoreId == storeId);

                // That's actually wrong because it is a projection and not a filter.
                if (ctx.Projection.CustomerId.HasValue)
                    query = query.Where(x => x.CustomerId == ctx.Projection.CustomerId.Value);

                result = query
                    .ApplyAuditDateFilter(f.CreatedFrom, f.CreatedTo)
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
                    .IncludeCustomerRoles()
                    .Include(x => x.BillingAddress)
                    .Include(x => x.ShippingAddress)
                    .Include(x => x.Addresses)
                    .AsSplitQuery()
                    .AsNoTrackingWithIdentityResolution()
                    .AsNoCaching();

                if (f.IsActiveCustomer.HasValue)
                    query = query.Where(x => x.Active == f.IsActiveCustomer.Value);

                if (f.IsTaxExempt.HasValue)
                    query = query.Where(x => x.IsTaxExempt == f.IsTaxExempt.Value);

                if (!f.BillingCountryIds.IsNullOrEmpty())
                    query = query.Where(x => x.BillingAddress != null && f.BillingCountryIds.Contains(x.BillingAddress.CountryId ?? 0));

                if (!f.ShippingCountryIds.IsNullOrEmpty())
                    query = query.Where(x => x.ShippingAddress != null && f.ShippingCountryIds.Contains(x.ShippingAddress.CountryId ?? 0));

                if (f.LastActivityFrom.HasValue)
                    query = query.Where(x => f.LastActivityFrom.Value <= x.LastActivityDateUtc);

                if (f.LastActivityTo.HasValue)
                    query = query.Where(x => f.LastActivityTo.Value >= x.LastActivityDateUtc);

                if (f.CreatedFrom.HasValue)
                    query = query.Where(x => f.CreatedFrom.Value <= x.CreatedOnUtc);

                if (f.CreatedTo.HasValue)
                    query = query.Where(x => f.CreatedTo.Value >= x.CreatedOnUtc);

                if (customerIdsByRolesQuery != null)
                    query = query.Where(x => customerIdsByRolesQuery.Contains(x.Id));

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
            else if (entityType == ExportEntityType.NewsletterSubscription)
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
                    var masterLanguageId = ctx.ShopMetadata.Get(storeId)?.MasterLanguageId ?? 0;

                    query = f.WorkingLanguageId == masterLanguageId
                        ? query.Where(x => x.Subscription.WorkingLanguageId == 0 || x.Subscription.WorkingLanguageId == f.WorkingLanguageId)
                        : query.Where(x => x.Subscription.WorkingLanguageId == f.WorkingLanguageId);
                }

                if (f.CreatedFrom.HasValue)
                    query = query.Where(x => f.CreatedFrom.Value <= x.Subscription.CreatedOnUtc);

                if (f.CreatedTo.HasValue)
                    query = query.Where(x => f.CreatedTo.Value >= x.Subscription.CreatedOnUtc);

                if (customerIdsByRolesQuery != null)
                    query = query.Where(x => customerIdsByRolesQuery.Contains(x.Customer.Id));

                result = query.Select(x => x.Subscription);
            }
            else if (entityType == ExportEntityType.ShoppingCartItem)
            {
                var query = _db.ShoppingCartItems
                    .Include(x => x.Customer)
                        .ThenInclude(x => x.CustomerRoleMappings)
                        .ThenInclude(x => x.CustomerRole)
                    .Include(x => x.Product)
                    .AsSplitQuery()
                    .AsNoTrackingWithIdentityResolution()
                    .AsNoCaching()
                    .Where(x => x.Customer != null && x.Product != null);

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

                if (f.LastActivityFrom.HasValue)
                    query = query.Where(x => f.LastActivityFrom.Value <= x.Customer.LastActivityDateUtc);

                if (f.LastActivityTo.HasValue)
                    query = query.Where(x => f.LastActivityTo.Value >= x.Customer.LastActivityDateUtc);

                if (f.CreatedFrom.HasValue)
                    query = query.Where(x => f.CreatedFrom.Value <= x.CreatedOnUtc);

                if (f.CreatedTo.HasValue)
                    query = query.Where(x => f.CreatedTo.Value >= x.CreatedOnUtc);

                if (ctx.Projection.NoBundleProducts)
                    query = query.Where(x => x.Product.ProductTypeId != (int)ProductType.BundledProduct);
                else
                    query = query.Where(x => x.BundleItemId == null);

                if (customerIdsByRolesQuery != null)
                    query = query.Where(x => customerIdsByRolesQuery.Contains(x.CustomerId));

                result = query;
            }
            else
            {
                throw new NotSupportedException($"Unsupported entity type '{entityType}'.");
            }

            if (ctx.Request.EntitiesToExport.Count > 0)
            {
                result = result.Where(x => ctx.Request.EntitiesToExport.Contains(x.Id));
            }

            return result;
        }

        private static IQueryable<BaseEntity> ApplyPaging(IQueryable<BaseEntity> query, int? skip, int take, DataExporterContext ctx)
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

        /// <summary>
        /// Related data is data without own export provider or importer.
        /// For a flat formatted export it has to be exported together with metadata to know what to be edited.
        /// Extra data units are only exported for file based exports, not for memory based on-the-fly exports.
        /// </summary>
        private static async Task<IEnumerable<ExportDataUnit>> GetDataUnitsForRelatedEntities(DataExporterContext ctx)
        {
            RelatedEntityType[] types;

            switch (ctx.Request.Provider.Value.EntityType)
            {
                case ExportEntityType.Product:
                    types = new RelatedEntityType[]
                    {
                        RelatedEntityType.TierPrice,
                        RelatedEntityType.ProductVariantAttributeValue,
                        RelatedEntityType.ProductVariantAttributeCombination
                    };
                    break;
                default:
                    return Enumerable.Empty<ExportDataUnit>();
            }

            var result = new List<ExportDataUnit>();
            var context = ctx.ExecuteContext;
            var dir = context.ExportDirectory;
            var fileExtension = Path.GetExtension(context.FileName);

            foreach (var type in types)
            {
                // Convention: must end with type name because that's how the import identifies the entity.
                // Be careful in case of accidents with file names. They must not be too long.
                var fileName = $"{ctx.Store.Id}-{context.FileIndex:D4}-{type}";
                var file = await dir.GetFileAsync(fileName + fileExtension);

                if (file.Exists)
                {
                    fileName = $"{CommonHelper.GenerateRandomDigitCode(4)}-{fileName}";
                }

                fileName += fileExtension;
                file = await dir.GetFileAsync(fileName);
                await file.CreateAsync(null, true, ctx.CancelToken);

                result.Add(new ExportDataUnit
                {
                    RelatedType = type,
                    DisplayInFileDialog = true,
                    FileName = fileName,
                    DataStream = new ExportFileStream(await file.OpenWriteAsync(null, ctx.CancelToken))
                });
            }

            return result;
        }

        private void DetachAllEntitiesAndClear(DataExporterContext ctx)
        {
            try
            {
                ctx.AssociatedProductBatchContext?.Clear();

                if (ctx.ProductBatchContext != null)
                {
                    Detach(x => x is Product || x is Discount || x is ProductVariantAttributeCombination || x is ProductVariantAttribute || x is ProductVariantAttributeValue ||
                        x is ProductAttribute || x is ProductBundleItem || x is ProductBundleItemAttributeFilter || x is ProductCategory || x is ProductManufacturer ||
                        x is Category || x is Manufacturer || x is ProductMediaFile || x is ProductTag || x is ProductSpecificationAttribute || x is SpecificationAttributeOption ||
                        x is SpecificationAttribute || x is TierPrice || x is ProductReview || x is ProductReviewHelpfulness || x is DeliveryTime || x is QuantityUnit || x is Download ||
                        x is MediaFile || x is MediaStorage || x is GenericAttribute || x is UrlRecord);

                    ctx.ProductBatchContext.Clear();
                }

                if (ctx.OrderBatchContext != null)
                {
                    Detach(x => x is Order || x is Address || x is GenericAttribute || x is Customer ||
                        x is OrderItem || x is RewardPointsHistory || x is Shipment || x is ProductVariantAttributeCombination);

                    ctx.OrderBatchContext.Clear();
                }

                if (ctx.CategoryBatchContext != null)
                {
                    Detach(x => x is Category || x is MediaFile || x is ProductCategory);

                    ctx.CategoryBatchContext.Clear();
                }

                if (ctx.ManufacturerBatchContext != null)
                {
                    Detach(x => x is Manufacturer || x is MediaFile || x is ProductManufacturer);

                    ctx.ManufacturerBatchContext.Clear();
                }

                if (ctx.CustomerBatchContext != null)
                {
                    Detach(x => x is Customer || x is GenericAttribute || x is CustomerContent);

                    ctx.CustomerBatchContext.Clear();
                }

                switch (ctx.Request.Provider.Value.EntityType)
                {
                    case ExportEntityType.ShoppingCartItem:
                        Detach(x => x is ShoppingCartItem || x is Customer || x is Product || x is ProductVariantAttributeCombination);
                        break;
                    case ExportEntityType.NewsletterSubscription:
                        Detach(x => x is NewsletterSubscription || x is Customer);
                        break;
                }
            }
            catch (Exception ex)
            {
                ctx.Log.Warn(ex, "Detaching entities failed.");
            }

            int Detach(Func<BaseEntity, bool> predicate)
            {
                return _db.DetachEntities(predicate);
            }
        }

        #endregion

        #region Utilities

        private IExportDataSegmenterProvider CreateSegmenter(DataExporterContext ctx)
        {
            var entityType = ctx.Request.Provider.Value.EntityType;
            var offset = Math.Max(ctx.Request.Profile.Offset, 0);
            var limit = Math.Max(ctx.Request.Profile.Limit, 0);
            var recordsPerSegment = ctx.IsPreview ? 0 : Math.Max(ctx.Request.Profile.BatchSize, 0);
            var totalRecords = offset + ctx.ShopMetadata[ctx.Store.Id].TotalRecords;
            var includeHidden = !ctx.Filter.IsPublished.HasValue || ctx.Filter.IsPublished.Value == false;

            if (entityType == ExportEntityType.Product)
            {
                async Task dataLoaded(ICollection<Product> entities)
                {
                    // Load data behind navigation properties for current entities batch in one go.
                    ctx.ProductBatchContext = _productService.CreateProductBatchContext(entities, ctx.Store, null, includeHidden);
                    ctx.PriceCalculationOptions = await CreatePriceCalculationOptions(ctx.ProductBatchContext, ctx);
                    ctx.AttributeCombinationPriceCalcOptions = await CreatePriceCalculationOptions(ctx.ProductBatchContext, ctx, true);
                    ctx.AssociatedProductBatchContext = null;

                    var context = ctx.ProductBatchContext;
                    if (!ctx.Projection.NoGroupedProducts && entities.Where(x => x.ProductType == ProductType.GroupedProduct).Any())
                    {
                        await context.AssociatedProducts.LoadAllAsync();
                        var associatedProducts = context.AssociatedProducts.SelectMany(x => x.Value);
                        ctx.AssociatedProductBatchContext = _productService.CreateProductBatchContext(associatedProducts, ctx.Store, null, includeHidden);

                        var allProductEntities = entities.Where(x => x.ProductType != ProductType.GroupedProduct).Concat(associatedProducts);
                        ctx.TranslationsPerPage[nameof(Product)] = await CreateTranslationCollection(nameof(Product), allProductEntities);
                        ctx.UrlRecordsPerPage[nameof(Product)] = await CreateUrlRecordCollection(nameof(Product), allProductEntities);
                    }
                    else
                    {
                        ctx.TranslationsPerPage[nameof(Product)] = await CreateTranslationCollection(nameof(Product), entities);
                        ctx.UrlRecordsPerPage[nameof(Product)] = await CreateUrlRecordCollection(nameof(Product), entities);
                    }

                    await context.ProductTags.LoadAllAsync();
                    await context.ProductBundleItems.LoadAllAsync();
                    await context.SpecificationAttributes.LoadAllAsync();
                    await context.Attributes.LoadAllAsync();

                    var psa = context.SpecificationAttributes.SelectMany(x => x.Value);
                    var sao = psa.Select(x => x.SpecificationAttributeOption);
                    var sa = psa.Select(x => x.SpecificationAttributeOption.SpecificationAttribute);

                    var pva = context.Attributes.SelectMany(x => x.Value);
                    var pvav = pva.SelectMany(x => x.ProductVariantAttributeValues);
                    var pa = pva.Select(x => x.ProductAttribute);

                    ctx.TranslationsPerPage[nameof(ProductTag)] = await CreateTranslationCollection(nameof(ProductTag), context.ProductTags.SelectMany(x => x.Value));
                    ctx.TranslationsPerPage[nameof(ProductBundleItem)] = await CreateTranslationCollection(nameof(ProductBundleItem), context.ProductBundleItems.SelectMany(x => x.Value));
                    ctx.TranslationsPerPage[nameof(SpecificationAttribute)] = await CreateTranslationCollection(nameof(SpecificationAttribute), sa);
                    ctx.TranslationsPerPage[nameof(SpecificationAttributeOption)] = await CreateTranslationCollection(nameof(SpecificationAttributeOption), sao);
                    ctx.TranslationsPerPage[nameof(ProductAttribute)] = await CreateTranslationCollection(nameof(ProductAttribute), pa);
                    ctx.TranslationsPerPage[nameof(ProductVariantAttributeValue)] = await CreateTranslationCollection(nameof(ProductVariantAttributeValue), pvav);
                }

                ctx.ExecuteContext.DataSegmenter = new ExportDataSegmenter<Product>
                (
                    () => LoadEntities<Product>(ctx),
                    dataLoaded,
                    entity => Convert(entity, ctx),
                    offset, PageSize, limit, recordsPerSegment, totalRecords
                );
            }
            else if (entityType == ExportEntityType.Order)
            {
                async Task dataLoaded(ICollection<Order> entities)
                {
                    ctx.OrderBatchContext = new OrderBatchContext(entities, _services);

                    await ctx.OrderBatchContext.OrderItems.LoadAllAsync();

                    var orderItems = ctx.OrderBatchContext.OrderItems.SelectMany(x => x.Value);
                    var products = orderItems.Select(x => x.Product);

                    ctx.ProductBatchContext = _productService.CreateProductBatchContext(products, ctx.Store, null, includeHidden);
                    ctx.PriceCalculationOptions = await CreatePriceCalculationOptions(ctx.ProductBatchContext, ctx);

                    ctx.TranslationsPerPage[nameof(Product)] = await CreateTranslationCollection(nameof(Product), products);
                    ctx.UrlRecordsPerPage[nameof(Product)] = await CreateUrlRecordCollection(nameof(Product), products);
                };

                ctx.ExecuteContext.DataSegmenter = new ExportDataSegmenter<Order>
                (
                    () => LoadEntities<Order>(ctx),
                    dataLoaded,
                    entity => Convert(entity, ctx),
                    offset, PageSize, limit, recordsPerSegment, totalRecords
                );
            }
            else if (entityType == ExportEntityType.Manufacturer)
            {
                Task dataLoaded(ICollection<Manufacturer> entities)
                {
                    ctx.ManufacturerBatchContext = new ManufacturerBatchContext(entities, _services);
                    return Task.CompletedTask;
                };

                ctx.ExecuteContext.DataSegmenter = new ExportDataSegmenter<Manufacturer>
                (
                    () => LoadEntities<Manufacturer>(ctx),
                    dataLoaded,
                    entity => Convert(entity, ctx),
                    offset, PageSize, limit, recordsPerSegment, totalRecords
                );
            }
            else if (entityType == ExportEntityType.Category)
            {
                Task dataLoaded(ICollection<Category> entities)
                {
                    ctx.CategoryBatchContext = new CategoryBatchContext(entities, _services);
                    return Task.CompletedTask;
                };

                ctx.ExecuteContext.DataSegmenter = new ExportDataSegmenter<Category>
                (
                    () => LoadEntities<Category>(ctx),
                    dataLoaded,
                    entity => Convert(entity, ctx),
                    offset, PageSize, limit, recordsPerSegment, totalRecords
                );
            }
            else if (entityType == ExportEntityType.Customer)
            {
                Task dataLoaded(ICollection<Customer> entities)
                {
                    ctx.CustomerBatchContext = new CustomerBatchContext(entities, _services);
                    return Task.CompletedTask;
                };

                ctx.ExecuteContext.DataSegmenter = new ExportDataSegmenter<Customer>
                (
                    () => LoadEntities<Customer>(ctx),
                    dataLoaded,
                    entity => Convert(entity, ctx),
                    offset, PageSize, limit, recordsPerSegment, totalRecords
                );
            }
            else if (entityType == ExportEntityType.NewsletterSubscription)
            {
                ctx.ExecuteContext.DataSegmenter = new ExportDataSegmenter<NewsletterSubscription>
                (
                    () => LoadEntities<NewsletterSubscription>(ctx),
                    null,
                    entity => Convert(entity, ctx),
                    offset, PageSize, limit, recordsPerSegment, totalRecords
                );
            }
            else if (entityType == ExportEntityType.ShoppingCartItem)
            {
                async Task dataLoaded(ICollection<ShoppingCartItem> entities)
                {
                    var products = entities.Select(x => x.Product);

                    ctx.ProductBatchContext = _productService.CreateProductBatchContext(products, ctx.Store, null, includeHidden);
                    ctx.PriceCalculationOptions = await CreatePriceCalculationOptions(ctx.ProductBatchContext, ctx);

                    ctx.TranslationsPerPage[nameof(Product)] = await CreateTranslationCollection(nameof(Product), products);
                    ctx.UrlRecordsPerPage[nameof(Product)] = await CreateUrlRecordCollection(nameof(Product), products);
                };

                ctx.ExecuteContext.DataSegmenter = new ExportDataSegmenter<ShoppingCartItem>
                (
                    () => LoadEntities<ShoppingCartItem>(ctx),
                    dataLoaded,
                    entity => Convert(entity, ctx),
                    offset, PageSize, limit, recordsPerSegment, totalRecords
                );
            }
            else
            {
                ctx.ExecuteContext.DataSegmenter = null;
            }

            return ctx.ExecuteContext.DataSegmenter as IExportDataSegmenterProvider;
        }

        private static async Task<bool> CallExportProvider(string method, IFile file, DataExporterContext ctx)
        {
            var context = ctx.ExecuteContext;
            var dir = context.ExportDirectory;
            var provider = ctx.Request.Provider.Value;

            if (method != "Execute" && method != "OnExecuted")
            {
                throw new NotSupportedException($"Unknown export method {method.NaIfEmpty()}.");
            }

            try
            {
                if (ctx.IsFileBasedExport && file != null)
                {
                    await file.CreateAsync(null, true, ctx.CancelToken);
                    context.DataStream = new ExportFileStream(await file.OpenWriteAsync(null, ctx.CancelToken));
                }
                else
                {
                    context.DataStream = new ExportFileStream(new MemoryStream());
                }

                if (method == "Execute")
                {
                    await provider.ExecuteAsync(context, ctx.CancelToken);
                }
                else if (method == "OnExecuted")
                {
                    await provider.OnExecutedAsync(context, ctx.CancelToken);
                }
            }
            catch (Exception ex)
            {
                context.Abort = DataExchangeAbortion.Hard;
                ctx.Log.Error(ex, $"The provider failed at the {method.NaIfEmpty()} method.");
                ctx.Result.LastError = ex.ToAllMessages(true);
            }
            finally
            {
                context.DataStream?.Dispose();
                context.DataStream = null;

                if (context.Abort == DataExchangeAbortion.Hard && ctx.IsFileBasedExport && file.Exists)
                {
                    await file.DeleteAsync(ctx.CancelToken);
                }

                if (method == "Execute")
                {
                    using var psb = StringBuilderPool.Instance.Get(out var unitFileInfos);

                    var relatedDataUnits = context.ExtraDataUnits
                        .Where(x => x.RelatedType.HasValue && x.FileName.HasValue() && x.DataStream != null)
                        .ToList();

                    foreach (var unit in relatedDataUnits)
                    {
                        // Set unit.DataStream to null so that providers know that this unit should no longer be written to.
                        // We need these units later for ExportProfile.ResultInfo.
                        unit.DataStream?.Dispose();
                        unit.DataStream = null;

                        var unitFile = unit.FileName.HasValue()
                            ? await dir.GetFileAsync(unit.FileName)
                            : null;

                        if (context.Abort == DataExchangeAbortion.Hard)
                        {
                            if (ctx.IsFileBasedExport && unitFile.Exists)
                            {
                                await unitFile.DeleteAsync(ctx.CancelToken);
                            }
                        }
                        else
                        {
                            unitFileInfos.AppendLine();
                            unitFileInfos.Append($"Provider reports {unit.RecordsSucceeded:N0} successfully exported record(s) of type {unit.RelatedType.Value} to {unitFile?.PhysicalPath?.NaIfEmpty()}.");
                        }
                    }

                    if (context.Abort != DataExchangeAbortion.Hard)
                    {
                        ctx.Log.Info($"Provider reports {context.RecordsSucceeded:N0} successfully exported record(s) of type {provider.EntityType} to {file?.PhysicalPath?.NaIfEmpty()}.");
                    }

                    ctx.Log.Info(unitFileInfos.ToString());
                }
            }

            return context.Abort != DataExchangeAbortion.Hard;
        }

        private async Task<bool> Deploy(DataExporterContext ctx)
        {
            var allSucceeded = true;
            var deployments = ctx.Request.Profile.Deployments
                .Where(x => x.Enabled)
                .OrderBy(x => x.Id)
                .ToArray();

            if (deployments.Length == 0)
            {
                return false;
            }

            var context = new ExportDeploymentContext
            {
                T = T,
                Log = ctx.Log,
                ExportProfileService = _exportProfileService,
                ExportDirectory = ctx.ExportDirectory,
                ZipFile = ctx.ZipFile,
                CreateZipArchive = ctx.Request.Profile.CreateZipArchive
            };

            foreach (var deployment in deployments)
            {
                IFilePublisher publisher = null;

                context.Result = new DataDeploymentResult
                {
                    LastExecutionUtc = DateTime.UtcNow
                };

                try
                {
                    switch (deployment.DeploymentType)
                    {
                        case ExportDeploymentType.Email:
                            publisher = new EmailFilePublisher(_db,
                                (DatabaseMediaStorageProvider)_providerManager.GetProvider<IMediaStorageProvider>(DatabaseMediaStorageProvider.SystemName).Value);
                            break;
                        case ExportDeploymentType.FileSystem:
                            publisher = new FileSystemFilePublisher(_services.ApplicationContext);
                            break;
                        case ExportDeploymentType.Ftp:
                            publisher = new FtpFilePublisher();
                            break;
                        case ExportDeploymentType.Http:
                            publisher = new HttpFilePublisher(_httpClientFactory);
                            break;
                        case ExportDeploymentType.PublicFolder:
                            publisher = new PublicFolderPublisher(_services.ApplicationContext);
                            break;
                    }

                    if (publisher != null)
                    {
                        await publisher.PublishAsync(deployment, context, ctx.CancelToken);

                        if (!context.Result.Succeeded)
                        {
                            allSucceeded = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    allSucceeded = false;

                    if (context.Result != null)
                    {
                        context.Result.LastError = ex.ToAllMessages(true);
                    }

                    ctx.Log.Error(ex, $"Deployment \"{deployment.Name}\" of type {deployment.DeploymentType} failed.");
                }

                deployment.ResultInfo = XmlHelper.Serialize(context.Result);
            }

            await _db.SaveChangesAsync(ctx.CancelToken);

            return allSucceeded;
        }

        private async Task SendCompletionEmail(DataExporterContext ctx)
        {
            var profile = ctx.Request.Profile;
            var emailAccount = await _db.EmailAccounts.FindByIdAsync(profile.EmailAccountId, false, ctx.CancelToken);
            if (emailAccount == null || emailAccount.Host.IsEmpty())
            {
                return;
            }

            var languageId = ctx.Projection.LanguageId ?? 0;
            var uri = ctx.Store.GetBaseUri();
            var storeInfo = $"{ctx.Store.Name} ({uri})";
            var intro = _services.Localization.GetResource("Admin.DataExchange.Export.CompletedEmail.Body", languageId).FormatInvariant(storeInfo);

            using var psb = StringBuilderPool.Instance.Get(out var body);
            body.Append(intro);

            if (ctx.Result.LastError.HasValue())
            {
                body.AppendFormat("<p style=\"color: #B94A48;\">{0}</p>", ctx.Result.LastError);
            }

            if (ctx.IsFileBasedExport && ctx.ZipFile.Exists)
            {
                var downloadUrl = _urlHelper.Value.Action("DownloadExportFile", "Export", new { area = "Admin", id = profile.Id, ctx.ZipFile.Name }, uri.Scheme, uri.Authority);
                body.AppendFormat("<p><a href='{0}' download>{1}</a></p>", downloadUrl, ctx.ZipFile.Name);
            }

            if (ctx.IsFileBasedExport && ctx.Result.Files.Count > 0)
            {
                body.Append("<p>");
                foreach (var file in ctx.Result.Files)
                {
                    var downloadUrl = _urlHelper.Value.Action("DownloadExportFile", "Export", new { area = "Admin", id = profile.Id, name = file.FileName }, uri.Scheme, uri.Authority);
                    body.AppendFormat("<div><a href='{0}' download>{1}</a></div>", downloadUrl, file.FileName);
                }
                body.Append("</p>");
            }

            using var message = new MailMessage
            {
                From = new(emailAccount.Email, emailAccount.DisplayName),
                Subject = _services.Localization.GetResource("Admin.DataExchange.Export.CompletedEmail.Subject", languageId).FormatInvariant(ctx.Request.Profile.Name),
                Body = body.ToString()
            };

            if (profile.CompletedEmailAddresses.HasValue())
            {
                var addresses = profile.CompletedEmailAddresses
                    .SplitSafe(',')
                    .Where(x => x.IsEmail())
                    .Select(x => new MailAddress(x));

                message.To.AddRange(addresses);
            }

            if (message.To.Count == 0 && _contactDataSettings.CompanyEmailAddress.HasValue())
            {
                message.To.Add(new(_contactDataSettings.CompanyEmailAddress));
            }

            if (message.To.Count == 0)
            {
                message.To.Add(new(emailAccount.Email, emailAccount.DisplayName));
            }

            await using var client = await _mailService.ConnectAsync(emailAccount);
            await client.SendAsync(message, ctx.CancelToken);

            //_db.QueuedEmails.Add(new QueuedEmail
            //{
            //    From = emailAccount.Email,
            //    To = message.To.First().Address,
            //    Subject = message.Subject,
            //    Body = message.Body,
            //    CreatedOnUtc = DateTime.UtcNow,
            //    EmailAccountId = emailAccount.Id,
            //    SendManually = true
            //});
            //await _db.SaveChangesAsync();
        }

        private async Task UpdateOrderStatus(DataExporterContext ctx, CancellationToken cancelToken)
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
                foreach (var chunk in ctx.EntityIdsLoaded.Chunk(200))
                {
                    num += await _db.Orders
                        .Where(x => chunk.Contains(x.Id))
                        .ExecuteUpdateAsync(setter => setter.SetProperty(o => o.OrderStatusId, o => newOrderStatusId.Value), cancelToken);
                }

                ctx.Log.Info($"Updated order status for {num} order(s).");
            }
        }

        private async Task<PriceCalculationOptions> CreatePriceCalculationOptions(ProductBatchContext batchContext, DataExporterContext ctx, bool forAttributeCombinations = false)
        {
            Guard.NotNull(batchContext);

            var priceDisplay = ctx.Projection.PriceType ?? _priceSettings.PriceDisplayType;
            var options = _priceCalculationService.CreateDefaultOptions(false, null, null, batchContext);

            options.DiscountValidationFlags = DiscountValidationFlags.All;
            options.ApplyPreselectedAttributes = priceDisplay == PriceDisplayType.PreSelectedPrice;

            if (forAttributeCombinations)
            {
                options.DetermineLowestPrice = false;
                options.DeterminePreselectedPrice = false;
            }
            else
            {
                options.DetermineLowestPrice = priceDisplay == PriceDisplayType.LowestPrice;
                options.DeterminePreselectedPrice = priceDisplay == PriceDisplayType.PreSelectedPrice;
            }

            if (ctx.Projection.ConvertNetToGrossPrices)
            {
                options.TaxInclusive = true;
            }

            if (ctx.Projection.StoreId.HasValue)
            {
                var taxSettings = await _services.SettingFactory.LoadSettingsAsync<TaxSettings>(ctx.Projection.StoreId.Value);
                options.IsGrossPrice = taxSettings.PricesIncludeTax;
            }

            return options;
        }

        private DataExporterContext CreateExporterContext(DataExportRequest request, bool isPreview, CancellationToken cancelToken)
        {
            var profile = request.Profile;
            var provider = request.Provider.Value;

            var context = new DataExporterContext
            {
                IsPreview = isPreview,
                Request = request,
                CancelToken = cancelToken,
                Filter = XmlHelper.Deserialize<ExportFilter>(profile.Filtering),
                Projection = XmlHelper.Deserialize<ExportProjection>(profile.Projection),
                ProgressInfo = T("Admin.DataExchange.Export.ProgressInfo")
            };

            if (profile.Projection.IsEmpty())
            {
                context.Projection.DescriptionMergingId = (int)ExportDescriptionMerging.Description;
            }

            context.ExecuteContext = new ExportExecuteContext(context.Result, cancelToken)
            {
                Filter = context.Filter,
                Projection = context.Projection,
                ProfileId = profile.Id,
                ProgressCallback = request.ProgressCallback
            };

            if (!isPreview && profile.ProviderConfigData.HasValue() && provider.ConfigurationInfo != null)
            {
                context.ExecuteContext.ConfigurationData = XmlHelper.Deserialize(profile.ProviderConfigData, provider.ConfigurationInfo.ModelType);
            }

            request.CustomData.Each(x => context.ExecuteContext.CustomProperties.Add(x.Key, x.Value));

            return context;
        }

        private async Task<LocalizedPropertyCollection> CreateTranslationCollection(string keyGroup, IEnumerable<BaseEntity> entities)
        {
            if (entities == null || !entities.Any())
            {
                return new LocalizedPropertyCollection(keyGroup, null, Enumerable.Empty<LocalizedProperty>());
            }

            return await _localizedEntityService.GetLocalizedPropertyCollectionAsync(keyGroup, entities.ToDistinctArray(x => x.Id));
        }

        private async Task<UrlRecordCollection> CreateUrlRecordCollection(string entityName, IEnumerable<BaseEntity> entities)
        {
            if (entities == null || !entities.Any())
            {
                return new UrlRecordCollection(entityName, null, Enumerable.Empty<UrlRecord>());
            }

            return await _urlService.GetUrlRecordCollectionAsync(entityName, null, entities.ToDistinctArray(x => x.Id));
        }

        private string CreateLogHeader(DataExporterContext ctx)
        {
            var customer = _workContext.CurrentCustomer;
            var profile = ctx.Request.Profile;
            var provider = ctx.Request.Provider;
            var module = provider.Metadata.ModuleDescriptor;
            var providerName = provider.Metadata.FriendlyName.HasValue()
                ? $"{provider.Metadata.FriendlyName} ({profile.ProviderSystemName})"
                : profile.ProviderSystemName;

            using var psb = StringBuilderPool.Instance.Get(out var sb);

            sb.AppendLine();
            sb.AppendLine(new string('-', 40));
            sb.AppendLine("Smartstore: v." + SmartstoreVersion.CurrentFullVersion);
            sb.Append("Export profile: " + profile.Name);
            sb.AppendLine(profile.Id == 0 ? " (transient)" : $" (ID {profile.Id})");
            sb.AppendLine("Export provider: " + providerName);

            sb.Append("Module: ");
            if (module != null)
            {
                var incompatible = module.Incompatible ? " INCOMPATIBLE" : string.Empty;
                sb.AppendLine($"{module.FriendlyName} ({module.SystemName}){incompatible}");
            }
            else
            {
                sb.AppendLine(string.Empty.NaIfEmpty());
            }

            sb.AppendLine("Entity: " + provider.Value.EntityType.ToString());

            try
            {
                sb.AppendLine($"Store: {ctx.Store.GetBaseUri().DnsSafeHost.NaIfEmpty()} (ID {ctx.Store.Id})");
            }
            catch
            {
            }

            sb.Append("Executed by: " + (customer.Email.HasValue() ? customer.Email : customer.SystemName));

            return sb.ToString();
        }

        private async Task CheckPermission(DataExporterContext ctx)
        {
            if (ctx.Request.HasPermission)
            {
                return;
            }

            var customer = _workContext.CurrentCustomer;

            if (customer.IsBackgroundTaskAccount())
            {
                return;
            }

            if (!await _services.Permissions.AuthorizeAsync(Permissions.Configuration.Export.Execute, customer))
            {
                throw new SecurityException(await _services.Permissions.GetUnauthorizedMessageAsync(Permissions.Configuration.Export.Execute));
            }
        }

        private static async Task SetProgress(string message, DataExporterContext ctx)
        {
            if (!ctx.IsPreview && message.HasValue())
            {
                try
                {
                    await ctx.Request.ProgressCallback.Invoke(0, 0, message);
                }
                catch
                {
                }
            }
        }

        private static async Task SetProgress(int loadedRecords, DataExporterContext ctx)
        {
            if (!ctx.IsPreview && loadedRecords > 0)
            {
                try
                {
                    var totalRecords = ctx.ShopMetadata.Sum(x => x.Value.TotalRecords);

                    if (ctx.Request.Profile.Limit > 0 && totalRecords > ctx.Request.Profile.Limit)
                    {
                        totalRecords = ctx.Request.Profile.Limit;
                    }

                    ctx.RecordCount = Math.Min(ctx.RecordCount + loadedRecords, totalRecords);
                    var msg = ctx.ProgressInfo.FormatInvariant(ctx.RecordCount.ToString("N0"), totalRecords.ToString("N0"));
                    await ctx.Request.ProgressCallback.Invoke(ctx.RecordCount, totalRecords, msg);
                }
                catch
                {
                }
            }
        }

        #endregion
    }
}
