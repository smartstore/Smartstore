using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.DataExchange.Import
{
    public class ProductImporter : EntityImporterBase
    {
        private static readonly Dictionary<string, Expression<Func<Product, string>>> _localizableProperties = new()
        {
            { "Name", x => x.Name },
            { "ShortDescription", x => x.ShortDescription },
            { "FullDescription", x => x.FullDescription },
            { "MetaKeywords", x => x.MetaKeywords },
            { "MetaDescription", x => x.MetaDescription },
            { "MetaTitle", x => x.MetaTitle },
            { "BundleTitleText", x => x.BundleTitleText }
        };

        public ProductImporter(
            ICommonServices services,
            ILocalizedEntityService localizedEntityService,
            IStoreMappingService storeMappingService,
            IUrlService urlService)
            : base(services, localizedEntityService, storeMappingService, urlService)
        {
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public static string[] SupportedKeyFields => new[] { "Id", "Sku", "Gtin", "ManufacturerPartNumber", "Name" };
        public static string[] DefaultKeyFields => new[] { "Sku", "Gtin", "ManufacturerPartNumber" };

        protected override async Task ProcessBatchAsync(ImportExecuteContext context, CancellationToken cancelToken = default)
        {
            // TODO: (mg) (core) Is forceNoTracking = true really a good idea here?
            using var scope = new DbContextScope(_db, autoDetectChanges: false, minHookImportance: HookImportance.Important, deferCommit: true, forceNoTracking: true);

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
            var srcToDestId = new Dictionary<int, ImportProductMapping>();
            var segmenter = context.DataSegmenter;
            var batch = segmenter.GetCurrentBatch<Product>();

            await context.SetProgressAsync(segmenter.CurrentSegmentFirstRowIndex - 1, segmenter.TotalRows);

            // ===========================================================================
            // 1.) Import products
            // ===========================================================================
            var savedProducts = 0;
            try
            {
                savedProducts = await InternalProcessProductsAsync(context, batch, srcToDestId);
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
            // 2.) Import SEO Slugs
            // ===========================================================================
            if (segmenter.HasColumn("SeName", true) || batch.Any(x => x.IsNew || x.NameChanged))
            {
                try
                {
                    await ProcessSlugsAsync(context, batch, typeof(Product).Name);
                }
                catch (Exception ex)
                {
                    context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessSlugsAsync));
                }
            }

            // ===========================================================================
            // 3.) Import StoreMappings
            // ===========================================================================
            if (segmenter.HasColumn("StoreIds"))
            {
                try
                {
                    await ProcessStoreMappingsAsync(context, batch);
                }
                catch (Exception ex)
                {
                    context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessStoreMappingsAsync));
                }
            }

            // ===========================================================================
            // 4.) Import Localizations
            // ===========================================================================
            try
            {
                await ProcessLocalizationsAsync(context, batch, _localizableProperties);
            }
            catch (Exception ex)
            {
                context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessLocalizationsAsync));
            }

            // ===========================================================================
            // 5.) Import product category mappings
            // ===========================================================================
            if (segmenter.HasColumn("CategoryIds"))
            {
                try
                {
                    await ProcessProductCategoriesAsync(context, batch);
                }
                catch (Exception ex)
                {
                    context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessProductCategoriesAsync));
                }
            }

            // ===========================================================================
            // 6.) Import product manufacturer mappings
            // ===========================================================================
            if (segmenter.HasColumn("ManufacturerIds"))
            {
                try
                {
                    await ProcessProductManufacturersAsync(context, batch);
                }
                catch (Exception ex)
                {
                    context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessProductManufacturersAsync));
                }
            }

            //...
        }

        protected virtual async Task<int> InternalProcessProductsAsync(
            ImportExecuteContext context,
            IEnumerable<ImportRow<Product>> batch,
            Dictionary<int, ImportProductMapping> srcToDestId)
        {
            var cargo = await GetCargoData(context);
            var defaultTemplateId = cargo.TemplateViewPaths["Product"];
            var hasNameColumn = context.DataSegmenter.HasColumn("Name");

            foreach (var row in batch)
            {
                Product product = null;
                var id = row.GetDataValue<int>("Id");

                foreach (var keyName in context.KeyFieldNames)
                {
                    var keyValue = row.GetDataValue<string>(keyName);
                    if (keyValue.HasValue() || id > 0)
                    {
                        switch (keyName)
                        {
                            case "Id":
                                product = await _db.Products.FindByIdAsync(id, true, context.CancelToken);
                                break;
                            case "Sku":
                                product = await _db.Products
                                    .ApplySkuFilter(keyValue)
                                    .FirstOrDefaultAsync(context.CancelToken);
                                break;
                            case "Gtin":
                                product = await _db.Products
                                    .ApplyGtinFilter(keyValue)
                                    .FirstOrDefaultAsync(context.CancelToken);
                                break;
                            case "ManufacturerPartNumber":
                                product = await _db.Products
                                    .ApplyMpnFilter(keyValue)
                                    .FirstOrDefaultAsync(context.CancelToken);
                                break;
                            case "Name":
                                keyValue = keyValue.Trim();
                                product = await _db.Products
                                    .AsQueryable()
                                    .Where(x => x.Name == keyValue)
                                    .OrderBy(x => x.Id)
                                    .FirstOrDefaultAsync(context.CancelToken);
                                break;
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
                    if (!row.HasDataValue("Name"))
                    {
                        ++context.Result.SkippedRecords;
                        context.Result.AddError("The 'Name' field is required for new products. Skipping row.", row.RowInfo, "Name");
                        continue;
                    }

                    product = new Product();
                }

                var name = row.GetDataValue<string>("Name");

                row.Initialize(product, name ?? product.Name);

                if (!row.IsNew && hasNameColumn)
                {
                    if (!product.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        // Perf: use this later for SeName updates.
                        row.NameChanged = true;
                    }
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
                row.SetProperty(context.Result, (x) => x.SampleDownloadId, null, ZeroToNull);    // TODO: global scope
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
                row.SetProperty(context.Result, (x) => x.OrderMaximumQuantity, 100);
                row.SetProperty(context.Result, (x) => x.QuantityStep, 1);
                row.SetProperty(context.Result, (x) => x.HideQuantityControl);
                row.SetProperty(context.Result, (x) => x.AllowedQuantities);
                row.SetProperty(context.Result, (x) => x.DisableBuyButton);
                row.SetProperty(context.Result, (x) => x.DisableWishlistButton);
                row.SetProperty(context.Result, (x) => x.AvailableForPreOrder);
                row.SetProperty(context.Result, (x) => x.CallForPrice);
                row.SetProperty(context.Result, (x) => x.Price);
                row.SetProperty(context.Result, (x) => x.OldPrice);
                row.SetProperty(context.Result, (x) => x.ProductCost);
                row.SetProperty(context.Result, (x) => x.SpecialPrice);
                row.SetProperty(context.Result, (x) => x.SpecialPriceStartDateTimeUtc);
                row.SetProperty(context.Result, (x) => x.SpecialPriceEndDateTimeUtc);
                row.SetProperty(context.Result, (x) => x.CustomerEntersPrice);
                row.SetProperty(context.Result, (x) => x.MinimumCustomerEnteredPrice);
                row.SetProperty(context.Result, (x) => x.MaximumCustomerEnteredPrice, 1000);
                // HasTierPrices... ignore as long as no tier prices are imported
                // LowestAttributeCombinationPrice... ignore as long as no combinations are imported
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

                if (row.TryGetDataValue("QuantiyControlType", out int qct))
                {
                    product.QuantiyControlType = (QuantityControlType)qct;
                }
                if (row.TryGetDataValue("AttributeChoiceBehaviour", out int attributeChoiceBehaviour))
                {
                    product.AttributeChoiceBehaviour = (AttributeChoiceBehaviour)attributeChoiceBehaviour;
                }
                if (row.TryGetDataValue("Visibility", out int visibilityValue))
                {
                    product.Visibility = (ProductVisibility)visibilityValue;
                }
                if (row.TryGetDataValue("Condition", out int conditionValue))
                {
                    product.Condition = (ProductCondition)conditionValue;
                }

                if (row.TryGetDataValue("ProductTemplateViewPath", out string tvp, row.IsTransient))
                {
                    product.ProductTemplateId = tvp.HasValue() && cargo.TemplateViewPaths.ContainsKey(tvp) 
                        ? cargo.TemplateViewPaths[tvp] 
                        : defaultTemplateId;
                }

                if (id != 0 && !srcToDestId.ContainsKey(id))
                {
                    srcToDestId.Add(id, new ImportProductMapping { Inserted = row.IsTransient });
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
            var num = await _db.SaveChangesAsync(context.CancelToken);

            // Get new product ids.
            foreach (var row in batch)
            {
                var id = row.GetDataValue<int>("Id");
                if (id != 0 && srcToDestId.ContainsKey(id))
                {
                    srcToDestId[id].DestinationId = row.Entity.Id;
                }
            }

            return num;
        }

        protected virtual async Task<int> ProcessProductCategoriesAsync(ImportExecuteContext context, IEnumerable<ImportRow<Product>> batch)
        {
            var cargo = await GetCargoData(context);

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

            var num = await _db.SaveChangesAsync(context.CancelToken);
            return num;
        }

        protected virtual async Task<int> ProcessProductManufacturersAsync(ImportExecuteContext context, IEnumerable<ImportRow<Product>> batch)
        {
            var cargo = await GetCargoData(context);

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

            var num = await _db.SaveChangesAsync(context.CancelToken);
            return num;
        }

        protected virtual async Task ProcessTierPricesAsync(ImportExecuteContext context)
        {
            var entityName = await _services.Localization.GetLocalizedEnumAsync(RelatedEntityType.TierPrice, _services.WorkContext.WorkingLanguage.Id);
            var segmenter = context.DataSegmenter;
            var batch = segmenter.GetCurrentBatch<TierPrice>();
            var savedEntities = 0;

            await context.SetProgressAsync(T("Admin.Common.ProcessingInfo", entityName, segmenter.CurrentSegmentFirstRowIndex - 1, segmenter.TotalRows));

            try
            {
                foreach (var row in batch)
                {
                    var id = row.GetDataValue<int>("Id");
                    var tierPrice = await _db.TierPrices.FindByIdAsync(id, true, context.CancelToken);

                    if (tierPrice == null)
                    {
                        if (context.UpdateOnly)
                        {
                            ++context.Result.SkippedRecords;
                            continue;
                        }

                        // ProductId is required for new tier prices.
                        var productId = row.GetDataValue<int>("ProductId");
                        if (productId == 0)
                        {
                            ++context.Result.SkippedRecords;
                            context.Result.AddError("The 'ProductId' field is required for new tier prices. Skipping row.", row.RowInfo, "ProductId");
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

                    if (row.TryGetDataValue("CalculationMethod", out int calcMethod))
                    {
                        tierPrice.CalculationMethod = (TierPriceCalculationMethod)calcMethod;
                    }

                    if (row.IsTransient)
                    {
                        _db.TierPrices.Add(tierPrice);
                    }
                }

                savedEntities = await _db.SaveChangesAsync(context.CancelToken);
            }
            catch (Exception ex)
            {
                context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessTierPricesAsync));
            }

            batch = batch.Where(x => x.Entity != null && !x.IsTransient).ToArray();

            context.Result.NewRecords += batch.Count(x => x.IsNew);
            context.Result.ModifiedRecords += Math.Max(0, savedEntities - context.Result.NewRecords);

            // Updating HasTierPrices property not necessary anymore.
            // This is done by the TierPriceHook (minHookImportance is set to HookImportance.Important).
        }

        protected virtual async Task ProcessAttributeValuesAsync(ImportExecuteContext context)
        {
            var entityName = await _services.Localization.GetLocalizedEnumAsync(RelatedEntityType.ProductVariantAttributeValue, _services.WorkContext.WorkingLanguage.Id);
            var segmenter = context.DataSegmenter;
            var batch = segmenter.GetCurrentBatch<ProductVariantAttributeValue>();
            var savedEntities = 0;

            await context.SetProgressAsync(T("Admin.Common.ProcessingInfo", entityName, segmenter.CurrentSegmentFirstRowIndex - 1, segmenter.TotalRows));

            try
            {
                foreach (var row in batch)
                {
                    var id = row.GetDataValue<int>("Id");
                    var attributeValue = await _db.ProductVariantAttributeValues.FindByIdAsync(id, true, context.CancelToken);

                    if (attributeValue == null)
                    {
                        if (context.UpdateOnly)
                        {
                            ++context.Result.SkippedRecords;
                            continue;
                        }

                        // ProductVariantAttributeId is required for new attribute values.
                        var pvaId = row.GetDataValue<int>("ProductVariantAttributeId");
                        if (pvaId == 0)
                        {
                            ++context.Result.SkippedRecords;
                            context.Result.AddError("The 'ProductVariantAttributeId' field is required for new attribute values. Skipping row.", row.RowInfo, "ProductVariantAttributeId");
                            continue;
                        }

                        if (!row.HasDataValue("Name"))
                        {
                            ++context.Result.SkippedRecords;
                            context.Result.AddError("The 'Name' field is required for new attribute values. Skipping row.", row.RowInfo, "Name");
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

                savedEntities = await _db.SaveChangesAsync(context.CancelToken);
            }
            catch (Exception ex)
            {
                context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessAttributeValuesAsync));
            }

            batch = batch.Where(x => x.Entity != null && !x.IsTransient).ToArray();

            context.Result.NewRecords += batch.Count(x => x.IsNew);
            context.Result.ModifiedRecords += Math.Max(0, savedEntities - context.Result.NewRecords);
        }

        protected virtual async Task ProcessAttributeCombinationsAsync(ImportExecuteContext context)
        {
            var entityName = await _services.Localization.GetLocalizedEnumAsync(RelatedEntityType.ProductVariantAttributeCombination, _services.WorkContext.WorkingLanguage.Id);
            var segmenter = context.DataSegmenter;
            var batch = segmenter.GetCurrentBatch<ProductVariantAttributeCombination>();
            var savedEntities = 0;

            await context.SetProgressAsync(T("Admin.Common.ProcessingInfo", entityName, segmenter.CurrentSegmentFirstRowIndex - 1, segmenter.TotalRows));

            try
            {
                foreach (var row in batch)
                {
                    var id = row.GetDataValue<int>("Id");
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
                                    case "Sku":
                                        combination = await _db.ProductVariantAttributeCombinations
                                            .ApplySkuFilter(keyValue)
                                            .FirstOrDefaultAsync(context.CancelToken);
                                        break;
                                    case "Gtin":
                                        combination = await _db.ProductVariantAttributeCombinations
                                            .ApplyGtinFilter(keyValue)
                                            .FirstOrDefaultAsync(context.CancelToken);
                                        break;
                                    case "ManufacturerPartNumber":
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
                    row.SetProperty(context.Result, (x) => x.RawAttributes);
                }

                savedEntities = await _db.SaveChangesAsync(context.CancelToken);
            }
            catch (Exception ex)
            {
                context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessAttributeCombinationsAsync));
            }

            batch = batch.Where(x => x.Entity != null && !x.IsTransient).ToArray();

            context.Result.NewRecords += batch.Count(x => x.IsNew);
            context.Result.ModifiedRecords += Math.Max(0, savedEntities - context.Result.NewRecords);

            // Updating LowestAttributeCombinationPrice property not necessary anymore.
            // This is done by the ProductVariantAttributeCombinationHook (minHookImportance is set to HookImportance.Important).
        }

        private async Task<ImporterCargoData> GetCargoData(ImportExecuteContext context)
        {
            if (context.CustomProperties.TryGetValue("ProductImporterCargoData", out object value))
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
                result.CategoryIds = await _db.Categories
                    .AsNoTracking()
                    .Select(x => x.Id)
                    .ToListAsync(context.CancelToken);
            }

            if (segmenter.HasColumn("ManufacturerIds"))
            {
                result.ManufacturerIds = await _db.Manufacturers
                    .AsNoTracking()
                    .Select(x => x.Id)
                    .ToListAsync(context.CancelToken);
            }

            context.CustomProperties["ProductImporterCargoData"] = result;
            return result;
        }

        protected class ImportProductMapping
        {
            public int DestinationId { get; set; }
            public bool Inserted { get; set; }
        }

        protected class ImporterCargoData
        {
            public Dictionary<string, int> TemplateViewPaths { get; init; }
            public List<int> CategoryIds { get; set; }
            public List<int> ManufacturerIds { get; set; }
        }
    }
}
