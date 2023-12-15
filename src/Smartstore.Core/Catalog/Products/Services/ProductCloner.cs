using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Diagnostics;

namespace Smartstore.Core.Catalog.Products
{
    public partial class ProductCloner : IProductCloner
    {
        private readonly SmartDbContext _db;
        private readonly IChronometer _chronometer;
        private readonly ILanguageService _languageService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly IUrlService _urlService;

        public ProductCloner(
            SmartDbContext db,
            IChronometer chronometer,
            ILanguageService languageService,
            IStoreMappingService storeMappingService,
            ILocalizedEntityService localizedEntityService,
            IProductAttributeMaterializer productAttributeMaterializer,
            IUrlService urlService)
        {
            _db = db;
            _chronometer = chronometer;
            _languageService = languageService;
            _storeMappingService = storeMappingService;
            _localizedEntityService = localizedEntityService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _urlService = urlService;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public virtual async Task<Product> CloneProductAsync(
            Product product,
            string cloneName,
            bool isPublished,
            bool copyAssociatedProducts = true)
        {
            Guard.NotNull(product);
            Guard.NotEmpty(cloneName);

            var localizedKeySelectors = new List<Expression<Func<Product, string>>>
            {
                x => x.Name,
                x => x.ShortDescription,
                x => x.FullDescription,
                x => x.MetaKeywords,
                x => x.MetaDescription,
                x => x.MetaTitle,
                x => x.BundleTitleText
            };

            var entry = _db.Entry(product);
            if (entry.State == EfState.Detached)
            {
                entry.State = EfState.Unchanged;
            }

            var clone = new Product();
            var utcNow = DateTime.UtcNow;
            var languages = await _languageService.GetAllLanguagesAsync(true);
            int? sampleDownloadId = null;

            // Enable hooks for slugs cache invalidation.
            using (_chronometer.Step("Clone product " + product.Id))
            using (var scope = new DbContextScope(_db, autoDetectChanges: false, minHookImportance: HookImportance.Normal, deferCommit: true, forceNoTracking: true))
            {
                if (product.HasSampleDownload && product.SampleDownload != null)
                {
                    var sampleDownloadClone = product.SampleDownload.Clone();
                    _db.Downloads.Add(sampleDownloadClone);

                    await scope.CommitAsync();
                    sampleDownloadId = sampleDownloadClone.Id;
                }

                var props = FastProperty.GetProperties(typeof(Product));

                foreach (var prop in props.Values)
                {
                    if (prop.IsComplexType)
                        continue;

                    if (!prop.IsPublicSettable)
                        continue;

                    prop.SetValue(clone, prop.GetValue(product));
                }

                clone.Id = 0;
                clone.Name = cloneName;
                clone.SampleDownloadId = sampleDownloadId;
                clone.Published = isPublished;
                clone.CreatedOnUtc = utcNow;
                clone.UpdatedOnUtc = utcNow;

                // Reset properties of ratings and reviews.
                clone.ApprovedRatingSum = 0;
                clone.NotApprovedRatingSum = 0;
                clone.ApprovedTotalReviews = 0;
                clone.NotApprovedTotalReviews = 0;

                // Category mappings.
                clone.ProductCategories.AddRange(product.ProductCategories.Select(x => new ProductCategory
                {
                    CategoryId = x.CategoryId,
                    IsFeaturedProduct = x.IsFeaturedProduct,
                    DisplayOrder = x.DisplayOrder
                }));

                // Manufacturer mappings.
                clone.ProductManufacturers.AddRange(product.ProductManufacturers.Select(x => new ProductManufacturer
                {
                    ManufacturerId = x.ManufacturerId,
                    IsFeaturedProduct = x.IsFeaturedProduct,
                    DisplayOrder = x.DisplayOrder
                }));

                // Media file mappings.
                clone.ProductMediaFiles.AddRange(product.ProductMediaFiles.Select(x => new ProductMediaFile
                {
                    MediaFileId = x.MediaFileId,
                    DisplayOrder = x.DisplayOrder
                }));

                clone.MainPictureId ??= product.ProductMediaFiles.FirstOrDefault()?.MediaFileId;

                // Product specification attributes.
                clone.ProductSpecificationAttributes.AddRange(product.ProductSpecificationAttributes.Select(x => new ProductSpecificationAttribute
                {
                    SpecificationAttributeOptionId = x.SpecificationAttributeOptionId,
                    AllowFiltering = x.AllowFiltering,
                    ShowOnProductPage = x.ShowOnProductPage,
                    DisplayOrder = x.DisplayOrder
                }));

                // Tier prices.
                clone.TierPrices.AddRange(product.TierPrices.Select(x => new TierPrice
                {
                    StoreId = x.StoreId,
                    CustomerRoleId = x.CustomerRoleId,
                    Quantity = x.Quantity,
                    Price = x.Price,
                    CalculationMethod = x.CalculationMethod
                }));
                clone.HasTierPrices = clone.TierPrices.Any();

                // Discount mappings.
                foreach (var discount in product.AppliedDiscounts)
                {
                    clone.AppliedDiscounts.Add(discount);
                    clone.HasDiscountsApplied = true;
                }

                // Tags.
                foreach (var tag in product.ProductTags)
                {
                    clone.ProductTags.Add(tag);
                }

                // >>>>>>> Put clone to db (from here on we need the product clone's ID).
                _db.Products.Add(clone);
                await scope.CommitAsync();

                // Store mappings.
                var selectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(product);
                selectedStoreIds.Each(id => _storeMappingService.AddStoreMapping(clone, id));

                await ProcessPromotions(product, clone);

                await ProcessSlugs(product, clone, languages);

                await ProcessLocalizations(product, clone, localizedKeySelectors, languages);

                await ProcessDownloads(product, clone);

                // >>>>>>> Put to db.
                await scope.CommitAsync();

                await ProcessBundleItems(scope, product, clone, languages);

                // Attributes and attribute combinations.
                await ProcessAttributes(scope, product, clone, languages);

                // Update computed properties.
                clone.LowestAttributeCombinationPrice = await _db.ProductVariantAttributeCombinations
                    .ApplyLowestPriceFilter(clone.Id)
                    .Select(x => x.Price)
                    .FirstOrDefaultAsync();

                // Associated products.
                if (copyAssociatedProducts && product.ProductType != ProductType.BundledProduct)
                {
                    await ProcessAssociatedProducts(product, clone, isPublished);
                }

                // >>>>>>> Our final commit.
                await scope.CommitAsync();
            }

            return clone;
        }

        private async Task ProcessSlugs(Product product, Product clone, IEnumerable<Language> languages)
        {
            await _urlService.SaveSlugAsync(clone, string.Empty, clone.GetDisplayName(), true);

            foreach (var lang in languages)
            {
                string name = product.GetLocalized(x => x.Name, lang, false, false);

                await _urlService.SaveSlugAsync(clone, string.Empty, name, false, lang.Id, true);
            }
        }

        private async Task ProcessPromotions(Product product, Product clone)
        {
            // Related products mappings.
            var relatedProducts = await _db.RelatedProducts
                .AsNoTracking()
                .ApplyProductId1Filter(product.Id, true)
                .ToListAsync();

            _db.RelatedProducts.AddRange(relatedProducts.Select(x => new RelatedProduct
            {
                ProductId1 = clone.Id,
                ProductId2 = x.ProductId2,
                DisplayOrder = x.DisplayOrder
            }));

            // Cross-sell products mappings.
            var crossSellProducts = await _db.CrossSellProducts
                .AsNoTracking()
                .ApplyProductId1Filter(product.Id, true)
                .ToListAsync();

            _db.CrossSellProducts.AddRange(crossSellProducts.Select(x => new CrossSellProduct
            {
                ProductId1 = clone.Id,
                ProductId2 = x.ProductId2
            }));
        }

        private async Task ProcessAttributes(DbContextScope scope, Product product, Product clone, IEnumerable<Language> languages)
        {
            var localizedKeySelectors = new List<Expression<Func<ProductVariantAttributeValue, string>>>
            {
                x => x.Name,
                x => x.Alias
            };

            await _db.LoadCollectionAsync(product, x => x.ProductVariantAttributes);
            await _db.LoadCollectionAsync(product, x => x.ProductVariantAttributeCombinations);

            // Former attribute id > clone.
            var attributeMap = new Dictionary<int, ProductVariantAttribute>();
            // Former attribute value id > clone.
            var valueMap = new Dictionary<int, ProductVariantAttributeValue>();
            var newCombinations = new List<ProductVariantAttributeCombination>();

            // Product attributes.
            foreach (var attribute in product.ProductVariantAttributes)
            {
                // Save associated value (used for combinations copying).
                attributeMap[attribute.Id] = new ProductVariantAttribute
                {
                    ProductId = clone.Id,
                    ProductAttributeId = attribute.ProductAttributeId,
                    TextPrompt = attribute.TextPrompt,
                    IsRequired = attribute.IsRequired,
                    AttributeControlTypeId = attribute.AttributeControlTypeId,
                    DisplayOrder = attribute.DisplayOrder
                };
            }

            _db.ProductVariantAttributes.AddRange(attributeMap.Select(x => x.Value));

            // >>>>>> Commit attributes.
            await scope.CommitAsync();

            // Product variant attribute values.
            foreach (var attribute in product.ProductVariantAttributes)
            {
                var attributeClone = attributeMap[attribute.Id];

                foreach (var value in attribute.ProductVariantAttributeValues)
                {
                    // Save associated value (used for combinations copying).
                    valueMap.Add(value.Id, new()
                    {
                        ProductVariantAttributeId = attributeClone.Id,
                        Name = value.Name,
                        Color = value.Color,
                        PriceAdjustment = value.PriceAdjustment,
                        WeightAdjustment = value.WeightAdjustment,
                        IsPreSelected = value.IsPreSelected,
                        DisplayOrder = value.DisplayOrder,
                        ValueTypeId = value.ValueTypeId,
                        LinkedProductId = value.LinkedProductId,
                        Quantity = value.Quantity,
                        MediaFileId = value.MediaFileId
                    });
                }
            }

            _db.ProductVariantAttributeValues.AddRange(valueMap.Select(x => x.Value));

            // >>>>>> Commit attribute values.
            await scope.CommitAsync();

            // Attribute value localization.
            var allValues = product.ProductVariantAttributes
                .SelectMany(x => x.ProductVariantAttributeValues)
                .ToArray();

            foreach (var value in allValues)
            {
                if (valueMap.TryGetValue(value.Id, out var newValue))
                {
                    await ProcessLocalizations(value, newValue, localizedKeySelectors, languages);
                }
            }

            // >>>>>> Commit localized values.
            await scope.CommitAsync();

            // Attribute combinations.
            foreach (var combination in product.ProductVariantAttributeCombinations)
            {
                var oldAttributesMap = combination.AttributeSelection.AttributesMap;
                var oldAttributes = await _productAttributeMaterializer.MaterializeProductVariantAttributesAsync(combination.AttributeSelection);
                var newSelection = new ProductVariantAttributeSelection(null);

                foreach (var oldAttribute in oldAttributes)
                {
                    // Only include and store list type attributes!
                    if (attributeMap.TryGetValue(oldAttribute.Id, out var newAttribute) && newAttribute.IsListTypeAttribute())
                    {
                        var item = oldAttributesMap.FirstOrDefault(x => x.Key == oldAttribute.Id);
                        if (item.Key != 0)
                        {
                            foreach (var value in item.Value)
                            {
                                var oldValueId = value.ToString().EmptyNull().ToInt();
                                if (valueMap.TryGetValue(oldValueId, out var newValue))
                                {
                                    newSelection.AddAttributeValue(newAttribute.Id, newValue.Id);
                                }
                            }
                        }
                    }
                }

                newCombinations.Add(new()
                {
                    ProductId = clone.Id,
                    RawAttributes = newSelection.AsJson(),
                    StockQuantity = combination.StockQuantity,
                    AllowOutOfStockOrders = combination.AllowOutOfStockOrders,
                    Sku = combination.Sku,
                    Gtin = combination.Gtin,
                    ManufacturerPartNumber = combination.ManufacturerPartNumber,
                    Price = combination.Price,
                    AssignedMediaFileIds = combination.AssignedMediaFileIds,
                    Length = combination.Length,
                    Width = combination.Width,
                    Height = combination.Height,
                    BasePriceAmount = combination.BasePriceAmount,
                    BasePriceBaseAmount = combination.BasePriceBaseAmount,
                    DeliveryTimeId = combination.DeliveryTimeId,
                    QuantityUnitId = combination.QuantityUnitId,
                    IsActive = combination.IsActive
                });
            }

            _db.ProductVariantAttributeCombinations.AddRange(newCombinations);

            // >>>>>> Commit combinations.
            await scope.CommitAsync();
        }

        private async Task ProcessBundleItems(DbContextScope scope, Product product, Product clone, IEnumerable<Language> languages)
        {
            var localizedKeySelectors = new List<Expression<Func<ProductBundleItem, string>>>
            {
                x => x.Name,
                x => x.ShortDescription
            };

            var bundledItems = await _db.ProductBundleItem
                .AsNoTracking()
                .Include(x => x.AttributeFilters)
                .ApplyBundledProductsFilter(new[] { product.Id }, true)
                .ToListAsync();

            if (!bundledItems.Any())
            {
                return;
            }

            var itemMap = new Dictionary<int, ProductBundleItem>();

            foreach (var bundledItem in bundledItems)
            {
                var newBundleItem = bundledItem.Clone();
                newBundleItem.BundleProductId = clone.Id;
                itemMap[bundledItem.Id] = newBundleItem;
            }

            _db.ProductBundleItem.AddRange(itemMap.Select(x => x.Value));
            await scope.CommitAsync();

            foreach (var bundledItem in bundledItems)
            {
                if (!itemMap.TryGetValue(bundledItem.Id, out var newBundleItem))
                {
                    continue;
                }

                foreach (var itemFilter in bundledItem.AttributeFilters)
                {
                    var newItemFilter = itemFilter.Clone();
                    newItemFilter.BundleItemId = newBundleItem.Id;

                    _db.ProductBundleItemAttributeFilter.Add(newItemFilter);
                }

                await ProcessLocalizations(bundledItem, newBundleItem, localizedKeySelectors, languages);
            }

            await scope.CommitAsync();
        }

        private async Task ProcessDownloads(Product product, Product clone)
        {
            var downloads = await _db.Downloads
                .AsNoTracking()
                .Include(x => x.MediaFile)
                .ApplyEntityFilter(nameof(Product), product.Id)
                .ToListAsync();

            foreach (var download in downloads)
            {
                var downloadClone = download.Clone();
                downloadClone.EntityId = clone.Id;

                _db.Downloads.Add(downloadClone);
            }
        }

        private async Task ProcessAssociatedProducts(Product product, Product clone, bool isPublished)
        {
            var associatedProducts = await _db.Products
                .ApplyAssociatedProductsFilter(new[] { product.Id }, true)
                .ToListAsync();

            foreach (var associatedProduct in associatedProducts)
            {
                var cloneName = T("Admin.Common.CopyOf", associatedProduct.Name).ToString();
                var associatedProductClone = await CloneProductAsync(associatedProduct, cloneName, isPublished, false);
                associatedProductClone.ParentGroupedProductId = clone.Id;
            }
        }

        private async Task ProcessLocalizations<T>(T source, T target, List<Expression<Func<T, string>>> keySelectors, IEnumerable<Language> languages)
            where T : BaseEntity, ILocalizedEntity
        {
            foreach (var lang in languages)
            {
                foreach (var keySelector in keySelectors)
                {
                    string value = source.GetLocalized(keySelector, lang, false, false);
                    if (value.HasValue())
                    {
                        await _localizedEntityService.ApplyLocalizedValueAsync(target, keySelector, value, lang.Id);
                    }
                }
            }
        }
    }
}