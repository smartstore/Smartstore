using System.Collections.Frozen;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Products.Utilities;
using Smartstore.Core.Content.Media;
using Smartstore.Core.DataExchange.Import.Events;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Net.Http;

namespace Smartstore.Core.DataExchange.Import
{
    public class ProductImporter : EntityImporterBase
    {
        const string CargoDataKey = "ProductImporter.CargoData";

        /// <summary>
        /// Old Product.Id -> new Product.Id
        /// </summary>
        const string TargetProductIdsKey = "ProductImporter.TargetProductIds";

        /// <summary>
        /// Old Product.Id -> old Product.ParentGroupedProductId
        /// </summary>
        const string ParentProductIdsKey = "ProductImporter.ParentProductIds";

        private static readonly FrozenDictionary<string, Expression<Func<Product, string>>> _localizableProperties = new Dictionary<string, Expression<Func<Product, string>>>()
        {
            { nameof(Product.Name), x => x.Name },
            { nameof(Product.ShortDescription), x => x.ShortDescription },
            { nameof(Product.FullDescription), x => x.FullDescription },
            { nameof(Product.MetaKeywords), x => x.MetaKeywords },
            { nameof(Product.MetaDescription), x => x.MetaDescription },
            { nameof(Product.MetaTitle), x => x.MetaTitle },
            { nameof(Product.BundleTitleText), x => x.BundleTitleText }
        }.ToFrozenDictionary();

        private readonly IMediaImporter _mediaImporter;

        public ProductImporter(
            ICommonServices services,
            IStoreMappingService storeMappingService,
            IUrlService urlService,
            IMediaImporter mediaImporter,
            SeoSettings seoSettings)
            : base(services, storeMappingService, urlService, seoSettings)
        {
            _mediaImporter = mediaImporter;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public static string[] SupportedKeyFields => new[]
        {
            nameof(Product.Id),
            nameof(Product.Sku),
            nameof(Product.Gtin),
            nameof(Product.ManufacturerPartNumber),
            nameof(Product.Name)
        };

        public static string[] DefaultKeyFields => new[]
        {
            nameof(Product.Sku),
            nameof(Product.Gtin),
            nameof(Product.ManufacturerPartNumber)
        };

        protected override async Task ProcessBatchAsync(ImportExecuteContext context, CancellationToken cancelToken = default)
        {
            if (context.File.RelatedType.HasValue)
            {
                switch (context.File.RelatedType.Value)
                {
                    case RelatedEntityType.TierPrice:
                        await ProcessTierPricesAsync(context);
                        break;
                    case RelatedEntityType.ProductVariantAttributeValue:
                        await ProcessAttributeValuesAsync(context);
                        break;
                    case RelatedEntityType.ProductVariantAttributeCombination:
                        await ProcessAttributeCombinationsAsync(context);
                        break;
                }
            }
            else
            {
                await ProcessProductsAsync(context);
            }
        }

        protected virtual async Task ProcessProductsAsync(ImportExecuteContext context)
        {
            var entityName = nameof(Product);
            var cargo = await GetCargoData(context);
            var segmenter = context.DataSegmenter;
            var batch = segmenter.GetCurrentBatch<Product>();

            using (var scope = new DbContextScope(_db, autoDetectChanges: false, minHookImportance: HookImportance.Important, deferCommit: true))
            {
                await context.SetProgressAsync(segmenter.CurrentSegmentFirstRowIndex - 1, segmenter.TotalRows);

                // ===========================================================================
                // 1.) Import products.
                // ===========================================================================
                var savedProducts = 0;
                try
                {
                    savedProducts = await InternalProcessProductsAsync(context, cargo, scope, batch);
                }
                catch (Exception ex)
                {
                    context.Result.AddError(ex, segmenter.CurrentSegment, nameof(InternalProcessProductsAsync));
                }

                // Reduce batch to saved (valid) products.
                // No need to perform import operations on errored products.
                batch = batch.Where(x => x.Entity != null && !x.IsTransient).ToArray();

                // Update result object.
                context.Result.NewRecords += batch.Count(x => x.IsNew);
                context.Result.ModifiedRecords += Math.Max(0, savedProducts - context.Result.NewRecords);

                // ===========================================================================
                // 2.) Import SEO slugs.
                // ===========================================================================
                if (segmenter.HasColumn("SeName", true) || batch.Any(x => x.IsNew || x.NameChanged))
                {
                    try
                    {
                        scope.DbContext.SuppressCommit = false;
                        await ProcessSlugsAsync(context, batch, entityName);
                    }
                    catch (Exception ex)
                    {
                        context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessSlugsAsync));
                    }
                    finally
                    {
                        scope.DbContext.SuppressCommit = true;
                    }
                }

                // ===========================================================================
                // 3.) Import store mappings.
                // ===========================================================================
                if (segmenter.HasColumn("StoreIds"))
                {
                    try
                    {
                        await ProcessStoreMappingsAsync(context, scope, batch, entityName);
                    }
                    catch (Exception ex)
                    {
                        context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessStoreMappingsAsync));
                    }
                }

                // ===========================================================================
                // 4.) Import localizations.
                // ===========================================================================
                try
                {
                    await ProcessLocalizationsAsync(context, scope, batch, entityName, _localizableProperties);
                }
                catch (Exception ex)
                {
                    context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessLocalizationsAsync));
                }

                // ===========================================================================
                // 5.) Import product category mappings.
                // ===========================================================================
                if (segmenter.HasColumn("CategoryIds"))
                {
                    try
                    {
                        await ProcessProductCategoriesAsync(context, cargo, scope, batch);
                    }
                    catch (Exception ex)
                    {
                        context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessProductCategoriesAsync));
                    }
                }

                // ===========================================================================
                // 6.) Import product manufacturer mappings.
                // ===========================================================================
                if (segmenter.HasColumn("ManufacturerIds"))
                {
                    try
                    {
                        await ProcessProductManufacturersAsync(context, cargo, scope, batch);
                    }
                    catch (Exception ex)
                    {
                        context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessProductManufacturersAsync));
                    }
                }

                // ===========================================================================
                // 7.) Import product picture mappings.
                // ===========================================================================
                if (segmenter.HasColumn("ImageUrls"))
                {
                    try
                    {
                        cargo.NumberOfNewImages += await ProcessProductImagesAsync(context, scope, batch);
                    }
                    catch (Exception ex)
                    {
                        context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessProductImagesAsync));
                    }
                }

                // ===========================================================================
                // 8.) Import product tag names.
                // ===========================================================================
                if (segmenter.HasColumn("TagNames"))
                {
                    try
                    {
                        await ProcessProductTagsAsync(context, scope, batch);
                    }
                    catch (Exception ex)
                    {
                        context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessProductTagsAsync));
                    }
                }

                // ===========================================================================
                // 9.) Import related products.
                // ===========================================================================
                if (segmenter.HasColumn("RelatedProductIds"))
                {
                    try
                    {
                        await ProcessRelatedProductsAsync(context, scope, batch);
                    }
                    catch (Exception ex)
                    {
                        context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessRelatedProductsAsync));
                    }
                }

                // ===========================================================================
                // 10.) Import cross selling products.
                // ===========================================================================
                if (segmenter.HasColumn("CrossSellProductIds"))
                {
                    try
                    {
                        await ProcessCrossSellingProductsAsync(context, scope, batch);
                    }
                    catch (Exception ex)
                    {
                        context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessCrossSellingProductsAsync));
                    }
                }

                if (segmenter.IsLastSegment || context.Abort == DataExchangeAbortion.Hard)
                {
                    // ===========================================================================
                    // 11.) Map parent ID of inserted products.
                    // ===========================================================================
                    if (segmenter.HasColumn(nameof(Product.Id)) &&
                        segmenter.HasColumn(nameof(Product.ParentGroupedProductId)) &&
                        !segmenter.IsIgnored(nameof(Product.ParentGroupedProductId)))
                    {
                        try
                        {
                            // We can make the parent grouped product assignment only after all the data has been processed and imported.
                            await ProcessGroupedProductsAsync(context, scope);
                        }
                        catch (Exception ex)
                        {
                            context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessGroupedProductsAsync));
                        }
                    }

                    // ===========================================================================
                    // 12.) PostProcess: normalization.
                    // ===========================================================================          
                    await ProductPictureHelper.FixProductMainPictureIds(_db, context.UtcNow);

                    if (cargo.NumberOfNewImages > 0)
                    {
                        context.Result.AddInfo("Importing new images may result in image duplicates if TinyImage is installed or the images are larger than \"Maximum image size\" setting.");
                    }
                }
            }

            await _services.EventPublisher.PublishAsync(new ImportBatchExecutedEvent<Product>(context, batch), context.CancelToken);
        }

        protected virtual async Task<int> InternalProcessProductsAsync(
            ImportExecuteContext context,
            ImporterCargoData cargo,
            DbContextScope scope,
            IEnumerable<ImportRow<Product>> batch)
        {
            var defaultTemplateId = cargo.TemplateViewPaths["Product"];
            var hasNameColumn = context.DataSegmenter.HasColumn(nameof(Product.Name));
            var parentProductIds = context.GetCustomProperty<Dictionary<int, int>>(ParentProductIdsKey);

            foreach (var row in batch)
            {
                Product product = null;
                var id = row.GetDataValue<int>(nameof(Product.Id));

                foreach (var keyName in context.KeyFieldNames)
                {
                    if (keyName == nameof(Product.Id))
                    {
                        product = await _db.Products.FindByIdAsync(id, true, context.CancelToken);
                    }
                    else
                    {
                        var keyValue = row.GetDataValue<string>(keyName).TrimSafe();
                        if (keyValue.HasValue())
                        {
                            switch (keyName)
                            {
                                case nameof(Product.Sku):
                                    product = await _db.Products
                                        .ApplySkuFilter(keyValue)
                                        .FirstOrDefaultAsync(context.CancelToken);
                                    break;
                                case nameof(Product.Gtin):
                                    product = await _db.Products
                                        .ApplyGtinFilter(keyValue)
                                        .FirstOrDefaultAsync(context.CancelToken);
                                    break;
                                case nameof(Product.ManufacturerPartNumber):
                                    product = await _db.Products
                                        .ApplyMpnFilter(keyValue)
                                        .FirstOrDefaultAsync(context.CancelToken);
                                    break;
                                case nameof(Product.Name):
                                    product = await _db.Products
                                        .AsQueryable()
                                        .Where(x => x.Name == keyValue)
                                        .OrderBy(x => x.Id)
                                        .FirstOrDefaultAsync(context.CancelToken);
                                    break;
                            }
                        }
                    }

                    if (product != null)
                        break;
                }

                if (product == null)
                {
                    if (context.UpdateOnly)
                    {
                        ++context.Result.SkippedRecords;
                        continue;
                    }

                    // A name is required for new products.
                    if (!row.HasDataValue(nameof(Product.Name)))
                    {
                        ++context.Result.SkippedRecords;
                        context.Result.AddMissingFieldError(row.RowInfo, nameof(Product), nameof(Product.Name));
                        continue;
                    }

                    product = new Product();
                }

                var name = row.GetDataValue<string>(nameof(Product.Name));

                row.Initialize(product, name ?? product.Name);

                if (!row.IsNew && hasNameColumn && !product.Name.EqualsNoCase(name))
                {
                    // Perf: use this later for SeName updates.
                    row.NameChanged = true;
                }

                row.SetProperty(context.Result, (x) => x.ProductTypeId, (int)ProductType.SimpleProduct);
                row.SetProperty(context.Result, (x) => x.Name);
                row.SetProperty(context.Result, (x) => x.ShortDescription);
                row.SetProperty(context.Result, (x) => x.FullDescription);
                row.SetProperty(context.Result, (x) => x.AdminComment);
                row.SetProperty(context.Result, (x) => x.ShowOnHomePage);
                row.SetProperty(context.Result, (x) => x.HomePageDisplayOrder);
                row.SetProperty(context.Result, (x) => x.MetaKeywords);
                row.SetProperty(context.Result, (x) => x.MetaDescription);
                row.SetProperty(context.Result, (x) => x.MetaTitle);
                row.SetProperty(context.Result, (x) => x.AllowCustomerReviews, true);
                row.SetProperty(context.Result, (x) => x.ApprovedRatingSum);
                row.SetProperty(context.Result, (x) => x.NotApprovedRatingSum);
                row.SetProperty(context.Result, (x) => x.ApprovedTotalReviews);
                row.SetProperty(context.Result, (x) => x.NotApprovedTotalReviews);
                row.SetProperty(context.Result, (x) => x.Published, true);
                row.SetProperty(context.Result, (x) => x.Sku);
                row.SetProperty(context.Result, (x) => x.ManufacturerPartNumber);
                row.SetProperty(context.Result, (x) => x.Gtin);
                row.SetProperty(context.Result, (x) => x.IsGiftCard);
                row.SetProperty(context.Result, (x) => x.GiftCardTypeId);
                row.SetProperty(context.Result, (x) => x.RequireOtherProducts);
                row.SetProperty(context.Result, (x) => x.RequiredProductIds);   // TODO: global scope
                row.SetProperty(context.Result, (x) => x.AutomaticallyAddRequiredProducts);
                row.SetProperty(context.Result, (x) => x.IsDownload);
                //row.SetProperty(context.Result, (x) => x.DownloadId);
                //row.SetProperty(context.Result, (x) => x.UnlimitedDownloads, true);
                row.SetProperty(context.Result, (x) => x.MaxNumberOfDownloads, 10);
                row.SetProperty(context.Result, (x) => x.DownloadExpirationDays);
                row.SetProperty(context.Result, (x) => x.DownloadActivationTypeId, 1);
                row.SetProperty(context.Result, (x) => x.HasSampleDownload);
                row.SetProperty(context.Result, (x) => x.SampleDownloadId, null, ImportUtility.ZeroToNull);    // TODO: global scope
                row.SetProperty(context.Result, (x) => x.HasUserAgreement);
                row.SetProperty(context.Result, (x) => x.UserAgreementText);
                row.SetProperty(context.Result, (x) => x.IsRecurring);
                row.SetProperty(context.Result, (x) => x.RecurringCycleLength, 100);
                row.SetProperty(context.Result, (x) => x.RecurringCyclePeriodId);
                row.SetProperty(context.Result, (x) => x.RecurringTotalCycles, 10);
                row.SetProperty(context.Result, (x) => x.IsShippingEnabled, true);
                row.SetProperty(context.Result, (x) => x.IsFreeShipping);
                row.SetProperty(context.Result, (x) => x.AdditionalShippingCharge);
                row.SetProperty(context.Result, (x) => x.IsEsd);
                row.SetProperty(context.Result, (x) => x.IsTaxExempt);
                row.SetProperty(context.Result, (x) => x.TaxCategoryId, 1);    // TODO: global scope
                row.SetProperty(context.Result, (x) => x.ManageInventoryMethodId);
                row.SetProperty(context.Result, (x) => x.StockQuantity, 10000);
                row.SetProperty(context.Result, (x) => x.DisplayStockAvailability);
                row.SetProperty(context.Result, (x) => x.DisplayStockQuantity);
                row.SetProperty(context.Result, (x) => x.MinStockQuantity);
                row.SetProperty(context.Result, (x) => x.LowStockActivityId);
                row.SetProperty(context.Result, (x) => x.NotifyAdminForQuantityBelow, 1);
                row.SetProperty(context.Result, (x) => x.BackorderModeId);
                row.SetProperty(context.Result, (x) => x.AllowBackInStockSubscriptions);
                row.SetProperty(context.Result, (x) => x.OrderMinimumQuantity, 1);
                row.SetProperty(context.Result, (x) => x.OrderMaximumQuantity, 50);
                row.SetProperty(context.Result, (x) => x.QuantityStep, 1);
                row.SetProperty(context.Result, (x) => x.HideQuantityControl);
                row.SetProperty(context.Result, (x) => x.AllowedQuantities);
                row.SetProperty(context.Result, (x) => x.DisableBuyButton);
                row.SetProperty(context.Result, (x) => x.DisableWishlistButton);
                row.SetProperty(context.Result, (x) => x.AvailableForPreOrder);
                row.SetProperty(context.Result, (x) => x.CallForPrice);
                row.SetProperty(context.Result, (x) => x.Price);
                row.SetProperty(context.Result, (x) => x.ComparePrice);
                row.SetProperty(context.Result, (x) => x.ComparePriceLabelId);
                row.SetProperty(context.Result, (x) => x.ProductCost);
                row.SetProperty(context.Result, (x) => x.SpecialPrice);
                row.SetProperty(context.Result, (x) => x.SpecialPriceStartDateTimeUtc);
                row.SetProperty(context.Result, (x) => x.SpecialPriceEndDateTimeUtc);
                row.SetProperty(context.Result, (x) => x.CustomerEntersPrice);
                row.SetProperty(context.Result, (x) => x.MinimumCustomerEnteredPrice);
                row.SetProperty(context.Result, (x) => x.MaximumCustomerEnteredPrice, 1000);
                // HasTierPrices: see ProcessTierPricesAsync.
                // LowestAttributeCombinationPrice: see ProcessAttributeCombinationsAsync.
                row.SetProperty(context.Result, (x) => x.Weight);
                row.SetProperty(context.Result, (x) => x.Length);
                row.SetProperty(context.Result, (x) => x.Width);
                row.SetProperty(context.Result, (x) => x.Height);
                row.SetProperty(context.Result, (x) => x.DisplayOrder);
                row.SetProperty(context.Result, (x) => x.DeliveryTimeId);      // TODO: global scope
                row.SetProperty(context.Result, (x) => x.QuantityUnitId);      // TODO: global scope
                row.SetProperty(context.Result, (x) => x.BasePriceEnabled);
                row.SetProperty(context.Result, (x) => x.BasePriceMeasureUnit);
                row.SetProperty(context.Result, (x) => x.BasePriceAmount);
                row.SetProperty(context.Result, (x) => x.BasePriceBaseAmount);
                row.SetProperty(context.Result, (x) => x.BundleTitleText);
                row.SetProperty(context.Result, (x) => x.BundlePerItemShipping);
                row.SetProperty(context.Result, (x) => x.BundlePerItemPricing);
                row.SetProperty(context.Result, (x) => x.BundlePerItemShoppingCart);
                row.SetProperty(context.Result, (x) => x.AvailableStartDateTimeUtc);
                row.SetProperty(context.Result, (x) => x.AvailableEndDateTimeUtc);
                // With new entities, "LimitedToStores" is an implicit field, meaning
                // it has to be set to true by code if it's absent but "StoreIds" exists.
                row.SetProperty(context.Result, (x) => x.LimitedToStores, !row.GetDataValue<List<int>>("StoreIds").IsNullOrEmpty());
                row.SetProperty(context.Result, (x) => x.CustomsTariffNumber);
                row.SetProperty(context.Result, (x) => x.CountryOfOriginId);
                row.SetProperty(context.Result, (x) => x.AttributeCombinationRequired);

                if (row.TryGetDataValue(nameof(Product.QuantityControlType), out int qct))
                {
                    product.QuantityControlType = (QuantityControlType)qct;
                }
                if (row.TryGetDataValue(nameof(Product.AttributeChoiceBehaviour), out int attributeChoiceBehaviour))
                {
                    product.AttributeChoiceBehaviour = (AttributeChoiceBehaviour)attributeChoiceBehaviour;
                }
                if (row.TryGetDataValue(nameof(Product.Visibility), out int visibilityValue))
                {
                    product.Visibility = (ProductVisibility)visibilityValue;
                }
                if (row.TryGetDataValue(nameof(Product.Condition), out int conditionValue))
                {
                    product.Condition = (ProductCondition)conditionValue;
                }

                if (row.TryGetDataValue("ProductTemplateViewPath", out string tvp, row.IsTransient))
                {
                    product.ProductTemplateId = tvp.HasValue() && cargo.TemplateViewPaths.ContainsKey(tvp)
                        ? cargo.TemplateViewPaths[tvp]
                        : defaultTemplateId;
                }

                if (row.TryGetDataValue(nameof(Product.ParentGroupedProductId), out int parentId) && parentId != 0 && id != 0)
                {
                    parentProductIds[id] = parentId;
                }

                if (row.IsTransient)
                {
                    _db.Products.Add(product);
                }
                else
                {
                    product.UpdatedOnUtc = DateTime.UtcNow;
                }
            }

            // Commit whole batch at once.
            var num = await scope.CommitAsync(context.CancelToken);

            // Get new product ids.
            // Required to assign associated products to their parent products.
            var targetProductIds = context.GetCustomProperty<Dictionary<int, int>>(TargetProductIdsKey);

            foreach (var row in batch.Where(x => x.Entity != null))
            {
                var id = row.GetDataValue<int>(nameof(Product.Id));
                if (id != 0)
                {
                    targetProductIds[id] = row.Entity.Id;
                }
            }

            return num;
        }

        protected virtual async Task<int> ProcessProductCategoriesAsync(
            ImportExecuteContext context,
            ImporterCargoData cargo,
            DbContextScope scope,
            IEnumerable<ImportRow<Product>> batch)
        {
            foreach (var row in batch)
            {
                var categoryIds = row.GetDataValue<List<int>>("CategoryIds");
                if (categoryIds.IsNullOrEmpty())
                    continue;

                try
                {
                    var existingMappingsCategoryIds = await _db.ProductCategories
                        .AsNoTracking()
                        .Where(x => x.ProductId == row.Entity.Id)
                        .Select(x => x.CategoryId)
                        .ToListAsync(context.CancelToken);

                    foreach (var categoryId in categoryIds)
                    {
                        if (categoryId != 0 &&
                            cargo.CategoryIds.Contains(categoryId) &&
                            !existingMappingsCategoryIds.Contains(categoryId))
                        {
                            _db.ProductCategories.Add(new ProductCategory
                            {
                                ProductId = row.Entity.Id,
                                CategoryId = categoryId,
                                IsFeaturedProduct = false,
                                DisplayOrder = 1
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    context.Result.AddWarning(ex.Message, row.RowInfo, "CategoryIds");
                }
            }

            var num = await scope.CommitAsync(context.CancelToken);
            return num;
        }

        protected virtual async Task<int> ProcessProductManufacturersAsync(
            ImportExecuteContext context,
            ImporterCargoData cargo,
            DbContextScope scope,
            IEnumerable<ImportRow<Product>> batch)
        {
            foreach (var row in batch)
            {
                var manufacturerIds = row.GetDataValue<List<int>>("ManufacturerIds");
                if (manufacturerIds.IsNullOrEmpty())
                    continue;

                try
                {
                    var existingMappingsManufacturerIds = await _db.ProductManufacturers
                        .AsNoTracking()
                        .Where(x => x.ProductId == row.Entity.Id)
                        .Select(x => x.ManufacturerId)
                        .ToListAsync(context.CancelToken);

                    foreach (var manufacturerId in manufacturerIds)
                    {
                        if (manufacturerId != 0 &&
                            cargo.ManufacturerIds.Contains(manufacturerId) &&
                            !existingMappingsManufacturerIds.Contains(manufacturerId))
                        {
                            _db.ProductManufacturers.Add(new ProductManufacturer
                            {
                                ProductId = row.Entity.Id,
                                ManufacturerId = manufacturerId,
                                IsFeaturedProduct = false,
                                DisplayOrder = 1
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    context.Result.AddWarning(ex.Message, row.RowInfo, "ManufacturerIds");
                }
            }

            var num = await scope.CommitAsync(context.CancelToken);
            return num;
        }

        protected virtual async Task<int> ProcessProductImagesAsync(ImportExecuteContext context, DbContextScope scope, IEnumerable<ImportRow<Product>> batch)
        {
            _mediaImporter.MessageHandler ??= (msg, item) =>
            {
                AddMessage<Product>(msg, item, context);
            };

            var items = new List<DownloadManagerItem>();
            var rows = batch
                .Select(row => new
                {
                    Row = row,
                    Urls = row.GetDataValue<string>("ImageUrls").SplitSafe('|').ToArray()
                })
                .Where(x => x.Urls.Length > 0);

            foreach (var row in rows)
            {
                var fileNameLookup = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

                var urls = context.ExtraData.NumberOfPictures.HasValue
                    ? row.Urls.Take(context.ExtraData.NumberOfPictures.Value)
                    : row.Urls;

                var itemsPerProduct = urls.Select(url =>
                    _mediaImporter.CreateDownloadItem(context.ImageDirectory, context.ImageDownloadDirectory, row.Row.Entity, url, row.Row, 0, fileNameLookup));

                items.AddRange(itemsPerProduct);
            }

            return await _mediaImporter.ImportProductImagesAsync(scope, items, DuplicateFileHandling.Rename, context.CancelToken);
        }

        protected virtual async Task ProcessProductTagsAsync(ImportExecuteContext context, DbContextScope scope, IEnumerable<ImportRow<Product>> batch)
        {
            var productIds = batch.Select(x => x.Entity.Id).ToArray();
            var tagsPerProduct = await _db.Products
                .AsNoTracking()
                .Include(x => x.ProductTags)
                .Where(x => productIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.ProductTags, context.CancelToken);

            foreach (var row in batch)
            {
                try
                {
                    // Key: tag name. Value: tag is published (for particular product).
                    var sourceTags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                    var product = row.Entity;

                    foreach (var value in row.GetDataValue<string>("TagNames").SplitSafe('|'))
                    {
                        var arr = value.SplitSafe('~').ToArray();
                        if (arr.Length > 0)
                        {
                            sourceTags[arr[0]] = arr.Length <= 1 || arr[1].ToBool(true);
                        }
                    }

                    if (!tagsPerProduct.TryGetValue(product.Id, out var existingTags))
                    {
                        existingTags = new List<ProductTag>();
                    }

                    if (!sourceTags.Any())
                    {
                        // Remove all tags.
                        if (existingTags.Any())
                        {
                            await _db.LoadCollectionAsync(product, x => x.ProductTags, cancelToken: context.CancelToken);
                            product.ProductTags.Clear();
                        }
                    }
                    else
                    {
                        // Remove tags.
                        var tagsToRemove = existingTags
                            .Where(x => !sourceTags.Keys.Any(y => y.EqualsNoCase(x.Name)))
                            .ToList();

                        if (tagsToRemove.Any())
                        {
                            await _db.LoadCollectionAsync(product, x => x.ProductTags, cancelToken: context.CancelToken);
                            tagsToRemove.Each(x => product.ProductTags.Remove(x));
                        }

                        var addedMissingTags = false;
                        var sourceTagNames = sourceTags.Select(x => x.Key).ToArray();
                        var tagsPerName = await _db.ProductTags
                            .AsQueryable()
                            .Where(x => sourceTagNames.Contains(x.Name))
                            .ToDictionaryAsync(x => x.Name, StringComparer.OrdinalIgnoreCase, context.CancelToken);

                        // Add missing tags.
                        foreach (var sourceTag in sourceTags)
                        {
                            if (!tagsPerName.TryGetValue(sourceTag.Key, out var tag))
                            {
                                tag = new ProductTag { Name = sourceTag.Key, Published = sourceTag.Value };

                                _db.ProductTags.Add(tag);
                                tagsPerName[sourceTag.Key] = tag;
                                addedMissingTags = true;
                            }
                        }

                        if (addedMissingTags)
                        {
                            // Tags must be saved and assigned an ID prior adding a mapping.
                            await scope.CommitAsync(context.CancelToken);

                            // Clear cached product per tag counts.
                            context.ClearCache = true;
                        }

                        // Add tag mappings.
                        foreach (var sourceTag in sourceTags)
                        {
                            if (tagsPerName.TryGetValue(sourceTag.Key, out var tag) && !existingTags.Any(x => x.Id == tag.Id))
                            {
                                product.ProductTags.Add(tag);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    context.Result.AddWarning(ex.Message, row.RowInfo, "TagNames");
                }
            }

            var num = await scope.CommitAsync(context.CancelToken);

            if (num > 0)
            {
                context.ClearCache = true;
            }
        }

        protected virtual async Task ProcessRelatedProductsAsync(ImportExecuteContext context, DbContextScope scope, IEnumerable<ImportRow<Product>> batch)
        {
            var productIds = batch.Select(x => x.Entity.Id).ToArray();
            var idsToRemove = new HashSet<int>();

            var existingRelatedProducts = await (
                from rp in _db.RelatedProducts.AsNoTracking()
                join p in _db.Products.AsNoTracking() on rp.ProductId2 equals p.Id
                where productIds.Contains(rp.ProductId1)
                select rp)
                .ToListAsync(context.CancelToken);

            var existingRelatedProductsMap = existingRelatedProducts.ToMultimap(x => x.ProductId1, x => x);

            foreach (var row in batch)
            {
                try
                {
                    var relatedProductIds = row.GetDataValue<List<int>>("RelatedProductIds")
                        ?.Where(x => x != 0)
                        ?.Distinct()
                        ?.ToArray();

                    existingRelatedProductsMap.TryGetValues(row.Entity.Id, out var existingMappingsTmp);
                    var existingMappings = existingMappingsTmp?.AsEnumerable() ?? Enumerable.Empty<RelatedProduct>();

                    if (relatedProductIds.IsNullOrEmpty())
                    {
                        idsToRemove.AddRange(existingMappings.Select(x => x.Id));
                    }
                    else
                    {
                        idsToRemove.AddRange(existingMappings
                            .Where(x => !relatedProductIds.Contains(x.ProductId2))
                            .Select(x => x.Id));

                        var displayOrder = existingMappingsTmp?.Count > 0 ? existingMappingsTmp.Max(x => x.DisplayOrder) : 0;

                        var existingProductIds = await _db.Products
                            .Where(x => relatedProductIds.Contains(x.Id))
                            .Select(x => x.Id)
                            .ToArrayAsync(context.CancelToken);

                        foreach (var relatedProductId in relatedProductIds)
                        {
                            if (relatedProductId != row.Entity.Id &&
                                existingProductIds.Contains(relatedProductId) &&
                                !existingMappings.Any(x => x.ProductId2 == relatedProductId))
                            {
                                _db.RelatedProducts.Add(new()
                                {
                                    ProductId1 = row.Entity.Id,
                                    ProductId2 = relatedProductId,
                                    DisplayOrder = ++displayOrder
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    context.Result.AddWarning(ex.Message, row.RowInfo, "RelatedProductIds");
                }
            }

            await scope.CommitAsync(context.CancelToken);

            foreach (var idsChunk in idsToRemove.Chunk(100))
            {
                await _db.RelatedProducts
                    .Where(x => idsChunk.Contains(x.Id))
                    .ExecuteDeleteAsync(context.CancelToken);
            }
        }

        protected virtual async Task ProcessCrossSellingProductsAsync(ImportExecuteContext context, DbContextScope scope, IEnumerable<ImportRow<Product>> batch)
        {
            var productIds = batch.Select(x => x.Entity.Id).ToArray();
            var idsToRemove = new HashSet<int>();

            var existingCrossSellProducts = await (
                from csp in _db.CrossSellProducts
                join p in _db.Products.AsNoTracking() on csp.ProductId2 equals p.Id
                where productIds.Contains(csp.ProductId1)
                select csp)
                .ToListAsync(context.CancelToken);

            var existingCrossSellProductsMap = existingCrossSellProducts.ToMultimap(x => x.ProductId1, x => x);

            foreach (var row in batch)
            {
                try
                {
                    var crossSellProductIds = row.GetDataValue<List<int>>("CrossSellProductIds")
                        ?.Where(x => x != 0)
                        ?.Distinct()
                        ?.ToArray();

                    if (row.RowInfo.Position >= 524 && row.RowInfo.Position <= 526)
                    {
                        row.RowInfo.Position.ToStringInvariant().Dump();
                    }

                    existingCrossSellProductsMap.TryGetValues(row.Entity.Id, out var existingMappingsTmp);
                    var existingMappings = existingMappingsTmp?.AsEnumerable() ?? Enumerable.Empty<CrossSellProduct>();

                    if (crossSellProductIds.IsNullOrEmpty())
                    {
                        idsToRemove.AddRange(existingMappings.Select(x => x.Id));
                    }
                    else
                    {
                        idsToRemove.AddRange(existingMappings
                            .Where(x => !crossSellProductIds.Contains(x.ProductId2))
                            .Select(x => x.Id));

                        var existingProductIds = await _db.Products
                            .Where(x => crossSellProductIds.Contains(x.Id))
                            .Select(x => x.Id)
                            .ToArrayAsync(context.CancelToken);

                        foreach (var crossSellProductId in crossSellProductIds)
                        {
                            if (crossSellProductId != row.Entity.Id &&
                                existingProductIds.Contains(crossSellProductId) &&
                                !existingMappings.Any(x => x.ProductId2 == crossSellProductId))
                            {
                                _db.CrossSellProducts.Add(new()
                                {
                                    ProductId1 = row.Entity.Id,
                                    ProductId2 = crossSellProductId
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    context.Result.AddWarning(ex.Message, row.RowInfo, "CrossSellProductIds");
                }
            }

            await scope.CommitAsync(context.CancelToken);

            foreach (var idsChunk in idsToRemove.Chunk(100))
            {
                await _db.CrossSellProducts
                    .Where(x => idsChunk.Contains(x.Id))
                    .ExecuteDeleteAsync(context.CancelToken);
            }
        }

        protected virtual async Task<int> ProcessGroupedProductsAsync(ImportExecuteContext context, DbContextScope scope)
        {
            var parentProductIds = context.GetCustomProperty<Dictionary<int, int>>(ParentProductIdsKey);
            if (!parentProductIds.Any())
            {
                return 0;
            }

            var productIds = context.GetCustomProperty<Dictionary<int, int>>(TargetProductIdsKey);
            var newIds = new Dictionary<int, int>();
            var num = 0;

            // Get new IDs.
            foreach (var item in parentProductIds)
            {
                // Find new parent product ID.
                if (productIds.TryGetValue(item.Value/* old parent ID */, out var newParentId) && newParentId != 0)
                {
                    // Find new associated product ID.
                    if (productIds.TryGetValue(item.Key/* old associated ID */, out var newAssociatedId) && newAssociatedId != 0)
                    {
                        newIds[newAssociatedId] = newParentId;
                    }
                }
            }

            // Update ParentGroupedProductId using batches.
            parentProductIds.Clear();
            var associatedIds = newIds.Keys.ToArray();

            foreach (var associatedIdsChunk in associatedIds.Chunk(100))
            {
                var associatedProducts = await _db.Products
                    .AsQueryable()
                    .Where(x => associatedIdsChunk.Contains(x.Id))
                    .ToListAsync(context.CancelToken);

                foreach (var associatedProduct in associatedProducts)
                {
                    if (newIds.TryGetValue(associatedProduct.Id, out var parentId))
                    {
                        associatedProduct.ParentGroupedProductId = parentId;
                    }
                }

                num += await scope.CommitAsync(context.CancelToken);
            }

            return num;
        }

        protected virtual async Task ProcessTierPricesAsync(ImportExecuteContext context)
        {
            var segmenter = context.DataSegmenter;
            var batch = segmenter.GetCurrentBatch<TierPrice>();
            var entityName = _services.Localization.GetLocalizedEnum(RelatedEntityType.TierPrice, _services.WorkContext.WorkingLanguage.Id);
            var savedEntities = 0;

            using (var scope = new DbContextScope(_db, autoDetectChanges: false, minHookImportance: HookImportance.Important, deferCommit: true))
            {
                await context.SetProgressAsync(T("Admin.Common.ProcessingInfo", entityName, segmenter.CurrentSegmentFirstRowIndex - 1, segmenter.TotalRows));

                try
                {
                    foreach (var row in batch)
                    {
                        var id = row.GetDataValue<int>(nameof(TierPrice.Id));
                        var tierPrice = await _db.TierPrices.FindByIdAsync(id, true, context.CancelToken);

                        if (tierPrice == null)
                        {
                            if (context.UpdateOnly)
                            {
                                ++context.Result.SkippedRecords;
                                continue;
                            }

                            // Product-ID is required for new tier prices.
                            var productId = row.GetDataValue<int>(nameof(TierPrice.ProductId));

                            if (productId == 0 &&
                                context.KeyFieldNames.Contains("Sku") &&
                                row.TryGetDataValue<string>("ProductSku", out var sku) &&
                                sku.HasValue())
                            {
                                productId = await _db.Products
                                    .ApplySkuFilter(sku)
                                    .Select(x => x.Id)
                                    .FirstOrDefaultAsync(context.CancelToken);
                            }

                            if (productId == 0)
                            {
                                ++context.Result.SkippedRecords;
                                context.Result.AddMissingFieldError(row.RowInfo, nameof(TierPrice), nameof(TierPrice.ProductId));
                                continue;
                            }

                            tierPrice = new TierPrice
                            {
                                ProductId = productId
                            };
                        }

                        row.Initialize(tierPrice, null);

                        // Ignore ProductId field to avoid accidents.
                        row.SetProperty(context.Result, (x) => x.StoreId);
                        row.SetProperty(context.Result, (x) => x.CustomerRoleId);
                        row.SetProperty(context.Result, (x) => x.Quantity);
                        row.SetProperty(context.Result, (x) => x.Price);

                        if (row.TryGetDataValue(nameof(TierPrice.CalculationMethod), out int calcMethod))
                        {
                            tierPrice.CalculationMethod = (TierPriceCalculationMethod)calcMethod;
                        }

                        if (row.IsTransient)
                        {
                            _db.TierPrices.Add(tierPrice);
                        }
                    }

                    savedEntities = await scope.CommitAsync(context.CancelToken);
                }
                catch (Exception ex)
                {
                    context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessTierPricesAsync));
                }

                batch = batch.Where(x => x.Entity != null && !x.IsTransient).ToArray();

                context.Result.NewRecords += batch.Count(x => x.IsNew);
                context.Result.ModifiedRecords += Math.Max(0, savedEntities - context.Result.NewRecords);

                // INFO: Product.HasTierPrices is updated by TierPriceHook.
            }

            await _services.EventPublisher.PublishAsync(new ImportBatchExecutedEvent<TierPrice>(context, batch));
        }

        protected virtual async Task ProcessAttributeValuesAsync(ImportExecuteContext context)
        {
            var segmenter = context.DataSegmenter;
            var batch = segmenter.GetCurrentBatch<ProductVariantAttributeValue>();
            var entityName = _services.Localization.GetLocalizedEnum(RelatedEntityType.ProductVariantAttributeValue, _services.WorkContext.WorkingLanguage.Id);
            var savedEntities = 0;

            using (var scope = new DbContextScope(_db, autoDetectChanges: false, minHookImportance: HookImportance.Important, deferCommit: true))
            {
                await context.SetProgressAsync(T("Admin.Common.ProcessingInfo", entityName, segmenter.CurrentSegmentFirstRowIndex - 1, segmenter.TotalRows));

                try
                {
                    foreach (var row in batch)
                    {
                        var id = row.GetDataValue<int>(nameof(ProductVariantAttributeValue.Id));
                        var attributeValue = await _db.ProductVariantAttributeValues.FindByIdAsync(id, true, context.CancelToken);

                        if (attributeValue == null)
                        {
                            if (context.UpdateOnly)
                            {
                                ++context.Result.SkippedRecords;
                                continue;
                            }

                            // ProductVariantAttributeId is required for new attribute values.
                            var pvaId = row.GetDataValue<int>(nameof(ProductVariantAttributeValue.ProductVariantAttributeId));
                            if (pvaId == 0)
                            {
                                ++context.Result.SkippedRecords;
                                context.Result.AddMissingFieldError(row.RowInfo, nameof(ProductVariantAttributeValue), nameof(ProductVariantAttributeValue.ProductVariantAttributeId));
                                continue;
                            }

                            if (!row.HasDataValue(nameof(ProductVariantAttributeValue.Name)))
                            {
                                ++context.Result.SkippedRecords;
                                context.Result.AddMissingFieldError(row.RowInfo, nameof(ProductVariantAttributeValue), nameof(ProductVariantAttributeValue.Name));
                                continue;
                            }

                            attributeValue = new ProductVariantAttributeValue
                            {
                                ProductVariantAttributeId = pvaId
                            };
                        }

                        row.Initialize(attributeValue, null);

                        // Ignore ProductVariantAttributeId field to avoid accidents.
                        row.SetProperty(context.Result, (x) => x.Alias);
                        row.SetProperty(context.Result, (x) => x.Name);
                        row.SetProperty(context.Result, (x) => x.Color);
                        row.SetProperty(context.Result, (x) => x.PriceAdjustment);
                        row.SetProperty(context.Result, (x) => x.WeightAdjustment);
                        row.SetProperty(context.Result, (x) => x.Quantity, 10000);
                        row.SetProperty(context.Result, (x) => x.IsPreSelected);
                        row.SetProperty(context.Result, (x) => x.DisplayOrder);
                        row.SetProperty(context.Result, (x) => x.ValueTypeId);
                        row.SetProperty(context.Result, (x) => x.LinkedProductId);

                        if (row.IsTransient)
                        {
                            _db.ProductVariantAttributeValues.Add(attributeValue);
                        }
                    }

                    savedEntities = await scope.CommitAsync(context.CancelToken);
                }
                catch (Exception ex)
                {
                    context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessAttributeValuesAsync));
                }

                batch = batch.Where(x => x.Entity != null && !x.IsTransient).ToArray();

                context.Result.NewRecords += batch.Count(x => x.IsNew);
                context.Result.ModifiedRecords += Math.Max(0, savedEntities - context.Result.NewRecords);
            }

            await _services.EventPublisher.PublishAsync(new ImportBatchExecutedEvent<ProductVariantAttributeValue>(context, batch));
        }

        protected virtual async Task ProcessAttributeCombinationsAsync(ImportExecuteContext context)
        {
            var segmenter = context.DataSegmenter;
            var batch = segmenter.GetCurrentBatch<ProductVariantAttributeCombination>();
            var entityName = _services.Localization.GetLocalizedEnum(RelatedEntityType.ProductVariantAttributeCombination, _services.WorkContext.WorkingLanguage.Id);
            var attributesColumnName = nameof(ProductVariantAttributeCombination.RawAttributes);
            var savedEntities = 0;

            using (var scope = new DbContextScope(_db, autoDetectChanges: false, minHookImportance: HookImportance.Important, deferCommit: true))
            {
                await context.SetProgressAsync(T("Admin.Common.ProcessingInfo", entityName, segmenter.CurrentSegmentFirstRowIndex - 1, segmenter.TotalRows));

                try
                {
                    foreach (var row in batch)
                    {
                        var id = row.GetDataValue<int>(nameof(Product.Id));
                        var combination = await _db.ProductVariantAttributeCombinations.FindByIdAsync(id, true, context.CancelToken);

                        if (combination == null)
                        {
                            foreach (var keyName in context.KeyFieldNames)
                            {
                                var keyValue = row.GetDataValue<string>(keyName);
                                if (keyValue.HasValue())
                                {
                                    switch (keyName)
                                    {
                                        case nameof(Product.Sku):
                                            combination = await _db.ProductVariantAttributeCombinations
                                                .ApplySkuFilter(keyValue)
                                                .FirstOrDefaultAsync(context.CancelToken);
                                            break;
                                        case nameof(Product.Gtin):
                                            combination = await _db.ProductVariantAttributeCombinations
                                                .ApplyGtinFilter(keyValue)
                                                .FirstOrDefaultAsync(context.CancelToken);
                                            break;
                                        case nameof(Product.ManufacturerPartNumber):
                                            combination = await _db.ProductVariantAttributeCombinations
                                                .ApplyMpnFilter(keyValue)
                                                .FirstOrDefaultAsync(context.CancelToken);
                                            break;
                                    }
                                }

                                if (combination != null)
                                    break;
                            }
                        }

                        if (combination == null)
                        {
                            // We do not insert records here to avoid inconsistent attribute combination data.
                            ++context.Result.SkippedRecords;
                            context.Result.AddError("The 'Id' or another key field is required. Inserting attribute combinations not supported. Skipping row.", row.RowInfo, "Id");
                            continue;
                        }

                        row.Initialize(combination, null);

                        // Ignore ProductId field to avoid accidents.
                        row.SetProperty(context.Result, (x) => x.Sku);
                        row.SetProperty(context.Result, (x) => x.Gtin);
                        row.SetProperty(context.Result, (x) => x.ManufacturerPartNumber);
                        row.SetProperty(context.Result, (x) => x.StockQuantity, 10000);
                        row.SetProperty(context.Result, (x) => x.Price);
                        row.SetProperty(context.Result, (x) => x.Length);
                        row.SetProperty(context.Result, (x) => x.Width);
                        row.SetProperty(context.Result, (x) => x.Height);
                        row.SetProperty(context.Result, (x) => x.BasePriceAmount);
                        row.SetProperty(context.Result, (x) => x.BasePriceBaseAmount);
                        row.SetProperty(context.Result, (x) => x.AssignedMediaFileIds);
                        row.SetProperty(context.Result, (x) => x.IsActive, true);
                        row.SetProperty(context.Result, (x) => x.AllowOutOfStockOrders);
                        row.SetProperty(context.Result, (x) => x.DeliveryTimeId);
                        row.SetProperty(context.Result, (x) => x.QuantityUnitId);

                        if (row.TryGetDataValue<string>(attributesColumnName, out var rawAttributes) && rawAttributes.HasValue())
                        {
                            if (ImportUtility.ValidateXmlOrJson(ref rawAttributes))
                            {
                                combination.RawAttributes = rawAttributes;
                            }
                            else
                            {
                                context.Result.AddWarning($"Ignored {attributesColumnName} because it contains invalid XML or JSON.", row.RowInfo, attributesColumnName);
                            }
                        }
                    }

                    savedEntities = await scope.CommitAsync(context.CancelToken);
                }
                catch (Exception ex)
                {
                    context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessAttributeCombinationsAsync));
                }

                batch = batch.Where(x => x.Entity != null && !x.IsTransient).ToArray();

                context.Result.NewRecords += batch.Count(x => x.IsNew);
                context.Result.ModifiedRecords += Math.Max(0, savedEntities - context.Result.NewRecords);

                // INFO: Product.LowestAttributeCombinationPrice is updated by ProductVariantAttributeCombinationHook.
            }

            await _services.EventPublisher.PublishAsync(new ImportBatchExecutedEvent<ProductVariantAttributeCombination>(context, batch));
        }

        /// <summary>
        /// Perf: load certain data only once per import to reduce database roundtrips.
        /// </summary>
        private async Task<ImporterCargoData> GetCargoData(ImportExecuteContext context)
        {
            if (context.CustomProperties.TryGetValue(CargoDataKey, out object value))
            {
                return (ImporterCargoData)value;
            }

            var segmenter = context.DataSegmenter;

            var productTemplates = await _db.ProductTemplates
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync(context.CancelToken);

            // Do not pass entities here because of batch scope!
            var result = new ImporterCargoData
            {
                TemplateViewPaths = productTemplates.ToDictionarySafe(x => x.ViewPath, x => x.Id)
            };

            if (segmenter.HasColumn("CategoryIds"))
            {
                result.CategoryIds = await _db.Categories.Select(x => x.Id).ToListAsync(context.CancelToken);
            }

            if (segmenter.HasColumn("ManufacturerIds"))
            {
                result.ManufacturerIds = await _db.Manufacturers.Select(x => x.Id).ToListAsync(context.CancelToken);
            }

            context.CustomProperties[CargoDataKey] = result;
            return result;
        }

        /// <summary>
        /// Perf: contains data that is loaded once per import.
        /// </summary>
        protected class ImporterCargoData
        {
            public Dictionary<string, int> TemplateViewPaths { get; init; }
            public List<int> CategoryIds { get; set; }
            public List<int> ManufacturerIds { get; set; }
            public int NumberOfNewImages { get; set; }
        }
    }
}
