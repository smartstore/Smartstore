using System.Runtime.CompilerServices;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Localization;
using Smartstore.Core.OutputCache;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Data;
using Smartstore.Diagnostics;
using Smartstore.Web.Infrastructure.Hooks;
using Smartstore.Web.Models.Catalog;
using Smartstore.Web.Models.Media;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.Controllers
{
    public partial class CatalogHelper
    {
        public void MapListActions(ProductSummaryModel model, IPagingOptions entity, string defaultPageSizeOptions)
        {
            var searchQuery = _catalogSearchQueryFactory.Current;

            // View mode
            model.AllowViewModeChanging = _catalogSettings.AllowProductViewModeChanging;

            // Sorting
            model.AllowSorting = _catalogSettings.AllowProductSorting;
            if (model.AllowSorting)
            {
                model.CurrentSortOrder = searchQuery?.CustomData.Get("CurrentSortOrder").Convert<int?>();

                model.AvailableSortOptions = _cache.Get("pres:productlistsortoptions-{0}".FormatInvariant(_workContext.WorkingLanguage.Id), () =>
                {
                    var dict = new Dictionary<int, string>();
                    foreach (ProductSortingEnum enumValue in Enum.GetValues(typeof(ProductSortingEnum)))
                    {
                        if (enumValue == ProductSortingEnum.CreatedOnAsc || enumValue == ProductSortingEnum.Initial)
                            continue;

                        dict[(int)enumValue] = _localizationService.GetLocalizedEnum(enumValue, _workContext.WorkingLanguage.Id);

                    }

                    return dict;
                });

                if (!searchQuery.Origin.EqualsNoCase("Search/Search"))
                {
                    model.RelevanceSortOrderName = T("Products.Sorting.Featured");
                    if ((int)ProductSortingEnum.Relevance == (model.CurrentSortOrder ?? 1))
                    {
                        model.CurrentSortOrderName = model.RelevanceSortOrderName;
                    }
                }

                if (model.CurrentSortOrderName.IsEmpty())
                {
                    model.CurrentSortOrderName = model.AvailableSortOptions.Get(model.CurrentSortOrder ?? 1) ?? model.AvailableSortOptions.First().Value;
                }
            }

            // Pagination
            if (entity?.AllowCustomersToSelectPageSize ?? _catalogSettings.AllowCustomersToSelectPageSize)
            {
                try
                {
                    model.AvailablePageSizes = (entity?.PageSizeOptions.NullEmpty() ?? defaultPageSizeOptions).Convert<List<int>>();
                }
                catch
                {
                    model.AvailablePageSizes = new int[] { 12, 24, 36, 48, 72, 120 };
                }
            }

            model.AllowFiltering = true;
        }

        public ProductSummaryMappingSettings GetBestFitProductSummaryMappingSettings(ProductSummaryViewMode viewMode)
        {
            return GetBestFitProductSummaryMappingSettings(viewMode, null);
        }

        public ProductSummaryMappingSettings GetBestFitProductSummaryMappingSettings(ProductSummaryViewMode viewMode, Action<ProductSummaryMappingSettings> fn)
        {
            var settings = new ProductSummaryMappingSettings
            {
                ViewMode = viewMode,
                MapPrices = true,
                MapPictures = true,
                ThumbnailSize = _mediaSettings.ProductThumbPictureSize
            };

            if (viewMode == ProductSummaryViewMode.Grid)
            {
                settings.MapShortDescription = _catalogSettings.ShowShortDescriptionInGridStyleLists;
                settings.MapManufacturers = _catalogSettings.ShowManufacturerInGridStyleLists;
                settings.MapColorAttributes = _catalogSettings.ShowColorSquaresInLists;
                settings.MapAttributes = _catalogSettings.ShowProductOptionsInLists;
                settings.MapReviews = _catalogSettings.ShowProductReviewsInProductLists;
                settings.DeliveryTimesPresentation = _catalogSettings.DeliveryTimesInLists;
            }
            else if (viewMode == ProductSummaryViewMode.List)
            {
                settings.MapShortDescription = true;
                settings.MapLegalInfo = _taxSettings.ShowLegalHintsInProductList;
                settings.MapManufacturers = true;
                settings.MapColorAttributes = _catalogSettings.ShowColorSquaresInLists;
                settings.MapAttributes = _catalogSettings.ShowProductOptionsInLists;
                //settings.MapSpecificationAttributes = true; // TODO: (mc) What about SpecAttrs in List-Mode (?) Option?
                settings.MapReviews = _catalogSettings.ShowProductReviewsInProductLists;
                settings.DeliveryTimesPresentation = _catalogSettings.DeliveryTimesInLists;
                settings.MapDimensions = _catalogSettings.ShowDimensions;
            }
            else if (viewMode == ProductSummaryViewMode.Compare)
            {
                settings.MapShortDescription = _catalogSettings.IncludeShortDescriptionInCompareProducts;
                settings.MapFullDescription = _catalogSettings.IncludeFullDescriptionInCompareProducts;
                settings.MapLegalInfo = _taxSettings.ShowLegalHintsInProductList;
                settings.MapManufacturers = true;
                settings.MapAttributes = true;
                settings.MapSpecificationAttributes = true;
                settings.MapReviews = _catalogSettings.ShowProductReviewsInProductLists;
                settings.DeliveryTimesPresentation = _catalogSettings.DeliveryTimesInLists;
                settings.MapDimensions = _catalogSettings.ShowDimensions;
            }

            fn?.Invoke(settings);

            return settings;
        }

        public virtual Task<ProductSummaryModel> MapProductSummaryModelAsync(IList<Product> products, ProductSummaryMappingSettings settings)
        {
            Guard.NotNull(products, nameof(products));

            return MapProductSummaryModelAsync(products.ToPagedList(0, int.MaxValue), settings);
        }

        public virtual async Task<ProductSummaryModel> MapProductSummaryModelAsync(CatalogSearchResult sourceResult, ProductSummaryMappingSettings settings)
        {
            return await MapProductSummaryModelAsync(await sourceResult.GetHitsAsync(), sourceResult, settings);
        }

        public virtual async Task<ProductSummaryModel> MapProductSummaryModelAsync(IPagedList<Product> products, ProductSummaryMappingSettings settings)
        {
            return await MapProductSummaryModelAsync(products, null, settings);
        }

        public virtual async Task<ProductSummaryModel> MapProductSummaryModelAsync(IPagedList<Product> products, CatalogSearchResult sourceResult, ProductSummaryMappingSettings settings)
        {
            Guard.NotNull(products, nameof(products));

            if (settings == null)
            {
                settings = new ProductSummaryMappingSettings();
            }

            using (_services.Chronometer.Step("MapProductSummaryModel"))
            {
                var model = new ProductSummaryModel(products, sourceResult)
                {
                    ViewMode = settings.ViewMode,
                    GridColumnSpan = _catalogSettings.GridStyleListColumnSpan,
                    ShowSku = _catalogSettings.ShowProductSku,
                    ShowWeight = _catalogSettings.ShowWeight,
                    ShowDimensions = settings.MapDimensions,
                    ShowLegalInfo = settings.MapLegalInfo,
                    ShowDescription = settings.MapShortDescription,
                    ShowFullDescription = settings.MapFullDescription,
                    ShowRatings = settings.MapReviews,
                    ShowPrice = settings.MapPrices,
                    ShowBasePrice = settings.MapPrices && _catalogSettings.ShowBasePriceInProductLists && settings.ViewMode != ProductSummaryViewMode.Mini,
                    ShowShippingSurcharge = settings.MapPrices && settings.ViewMode != ProductSummaryViewMode.Mini,
                    ShowButtons = settings.ViewMode != ProductSummaryViewMode.Mini,
                    ShowBrand = settings.MapManufacturers,
                    ForceRedirectionAfterAddingToCart = settings.ForceRedirectionAfterAddingToCart,
                    CompareEnabled = _catalogSettings.CompareProductsEnabled,
                    WishlistEnabled = await _services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist),
                    BuyEnabled = !_catalogSettings.HideBuyButtonInLists,
                    ThumbSize = settings.ThumbnailSize,
                    ShowDiscountBadge = _catalogSettings.ShowDiscountSign,
                    ShowNewBadge = _catalogSettings.LabelAsNewForMaxDays.HasValue,
                    DeliveryTimesPresentation = settings.DeliveryTimesPresentation,
                };

                if (products.Count == 0)
                {
                    // No products, stop here.
                    return model;
                }

                await using var scope = new DbContextScope(_db, retainConnection: true, deferCommit: true);

                // PERF!!
                var calculationOptions = _priceCalculationService.CreateDefaultOptions(true);
                var language = calculationOptions.Language;
                var customer = calculationOptions.Customer;
                var allowPrices = await _services.Permissions.AuthorizeAsync(Permissions.Catalog.DisplayPrice);
                var allowShoppingCart = await _services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart);
                var allowWishlist = await _services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist);
                var cachedBrandModels = new Dictionary<int, BrandOverviewModel>();
                var prefetchTranslations = settings.PrefetchTranslations == true || (settings.PrefetchTranslations == null && _performanceSettings.AlwaysPrefetchTranslations);
                var prefetchSlugs = settings.PrefetchUrlSlugs == true || (settings.PrefetchUrlSlugs == null && _performanceSettings.AlwaysPrefetchUrlSlugs);
                var allProductIds = prefetchSlugs || prefetchTranslations ? products.Select(x => x.Id).ToArray() : Array.Empty<int>();

                //var productIds = products.Select(x => x.Id).ToArray();

                string taxInfo = T(calculationOptions.TaxInclusive ? "Tax.InclVAT" : "Tax.ExclVAT");
                var legalInfo = string.Empty;

                var res = new Dictionary<string, LocalizedString>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Products.CallForPrice", T("Products.CallForPrice") },
                    { "Products.PriceRangeFrom", T("Products.PriceRangeFrom") },
                    { "Media.Product.ImageLinkTitleFormat", T("Media.Product.ImageLinkTitleFormat") },
                    { "Media.Product.ImageAlternateTextFormat", T("Media.Product.ImageAlternateTextFormat") },
                    { "Products.DimensionsValue", T("Products.DimensionsValue") },
                    { "Common.AdditionalShippingSurcharge", T("Common.AdditionalShippingSurcharge") }
                };

                if (settings.MapLegalInfo)
                {
                    var shippingInfoUrl = (await _urlHelper.TopicAsync("shippinginfo"));
                    legalInfo = shippingInfoUrl.HasValue()
                        ? T("Tax.LegalInfoShort", taxInfo, shippingInfoUrl)
                        : T("Tax.LegalInfoShort2", taxInfo);
                }

                if (prefetchSlugs)
                {
                    await _urlService.PrefetchUrlRecordsAsync(nameof(Product), new[] { language.Id, 0 }, allProductIds);
                }

                if (prefetchTranslations)
                {
                    // Prefetch all delivery time translations
                    await _localizedEntityService.PrefetchLocalizedPropertiesAsync(nameof(DeliveryTime), language.Id, null);
                }

                // Run in uncommitting scope, because pictures could be updated (IsNew property) 
                var batchContext = _productService.CreateProductBatchContext(products, calculationOptions.Store, customer, false, true);

                if (settings.MapPrices)
                {
                    await batchContext.AppliedDiscounts.LoadAllAsync();
                    await batchContext.TierPrices.LoadAllAsync();
                }

                if (settings.MapAttributes || settings.MapColorAttributes)
                {
                    await batchContext.Attributes.LoadAllAsync();

                    if (prefetchTranslations)
                    {
                        // Prefetch all product attribute translations
                        await PrefetchTranslations(
                            nameof(ProductAttribute),
                            language.Id,
                            batchContext.Attributes.SelectMany(x => x.Value).Select(x => x.ProductAttribute));

                        // Prefetch all variant attribute value translations
                        await PrefetchTranslations(
                            nameof(ProductVariantAttributeValue),
                            language.Id,
                            batchContext.Attributes.SelectMany(x => x.Value).SelectMany(x => x.ProductVariantAttributeValues));
                    }
                }

                if (settings.MapManufacturers)
                {
                    await batchContext.ProductManufacturers.LoadAllAsync();
                }

                if (settings.MapSpecificationAttributes)
                {
                    await batchContext.SpecificationAttributes.LoadAllAsync();

                    if (prefetchTranslations)
                    {
                        // Prefetch all spec attribute option translations
                        await PrefetchTranslations(
                            nameof(SpecificationAttributeOption),
                            language.Id,
                            batchContext.SpecificationAttributes.SelectMany(x => x.Value).Select(x => x.SpecificationAttributeOption));

                        // Prefetch all spec attribute translations
                        await PrefetchTranslations(
                            nameof(SpecificationAttribute),
                            language.Id,
                            batchContext.SpecificationAttributes.SelectMany(x => x.Value).Select(x => x.SpecificationAttributeOption.SpecificationAttribute));
                    }
                }

                // If a size has been set in the view, we use it in priority
                int thumbSize = model.ThumbSize ?? _mediaSettings.ProductThumbPictureSize;

                calculationOptions.BatchContext = batchContext;

                // Don't perform discount limitation and coupon code check in list rendering as it can have heavy impact on performance.
                calculationOptions.CheckDiscountValidity = false;

                var mapItemContext = new MapProductSummaryItemContext
                {
                    BatchContext = batchContext,
                    CalculationOptions = calculationOptions,
                    CachedBrandModels = cachedBrandModels,
                    PrimaryCurrency = _currencyService.PrimaryCurrency,
                    LegalInfo = legalInfo,
                    Model = model,
                    Resources = res,
                    MappingSettings = settings,
                    AllowPrices = allowPrices,
                    AllowShoppingCart = allowShoppingCart,
                    AllowWishlist = allowWishlist,
                    ShippingChargeTaxFormat = _taxService.GetTaxFormat(priceIncludesTax: calculationOptions.TaxInclusive, target: PricingTarget.ShippingCharge, language: language),
                };

                if (settings.MapPictures)
                {
                    var fileIds = products
                        .Select(x => x.MainPictureId ?? 0)
                        .Where(x => x != 0)
                        .Distinct()
                        .ToArray();

                    mapItemContext.MediaFiles = (await _mediaService.GetFilesByIdsAsync(fileIds)).ToDictionarySafe(x => x.Id);
                }

                foreach (var product in products)
                {
                    await MapProductSummaryItem(product, mapItemContext);
                }

                _services.DisplayControl.AnnounceRange(products);

                await scope.CommitAsync();

                batchContext.Clear();

                // don't show stuff without data at all
                model.ShowDescription = model.ShowDescription && model.Items.Any(x => x.ShortDescription?.Value?.HasValue() == true);
                model.ShowBrand = model.ShowBrand && model.Items.Any(x => x.Brand != null);

                return model;
            }
        }

        private Task PrefetchTranslations(string keyGroup, int languageId, IEnumerable<BaseEntity> entities)
        {
            if (entities.Any())
            {
                return _localizedEntityService.PrefetchLocalizedPropertiesAsync(keyGroup, languageId, entities.Select(x => x.Id).Distinct().ToArray());
            }

            return Task.CompletedTask;
        }

        private async Task MapProductSummaryItem(Product product, MapProductSummaryItemContext ctx)
        {
            var contextProduct = product;
            var finalPrice = default(Money);
            var model = ctx.Model;
            var settings = ctx.MappingSettings;
            var options = ctx.CalculationOptions;
            var slug = await product.GetActiveSlugAsync();

            var item = new ProductSummaryModel.SummaryItem(ctx.Model)
            {
                Id = product.Id,
                Name = product.GetLocalized(x => x.Name),
                SeName = slug,
                DetailUrl = _urlHelper.RouteUrl("Product", new { SeName = slug })
            };

            if (model.ShowDescription)
            {
                item.ShortDescription = product.GetLocalized(x => x.ShortDescription);
            }

            if (settings.MapFullDescription)
            {
                item.FullDescription = product.GetLocalized(x => x.FullDescription, detectEmptyHtml: true);
            }

            // Price
            if (settings.MapPrices)
            {
                (finalPrice, contextProduct) = await MapSummaryItemPrice(product, item, ctx);
            }

            // (Color) Attributes
            if (settings.MapColorAttributes || settings.MapAttributes)
            {
                var attributes = await ctx.BatchContext.Attributes.GetOrLoadAsync(contextProduct.Id);
                var cachedAttributeNames = new Dictionary<int, LocalizedValue<string>>();

                // Color squares
                if (attributes.Any() && settings.MapColorAttributes)
                {
                    var colorAttributes = attributes
                        .Where(x => x.IsListTypeAttribute())
                        .SelectMany(x => x.ProductVariantAttributeValues)
                        .Where(x => x.Color.HasValue() && !x.Color.EqualsNoCase("transparent"))
                        .Distinct()
                        .Take(20) // limit results
                        .Select(x =>
                        {
                            var attr = x.ProductVariantAttribute.ProductAttribute;
                            var attrName = cachedAttributeNames.Get(attr.Id) ?? (cachedAttributeNames[attr.Id] = attr.GetLocalized(l => l.Name));

                            return new ProductSummaryModel.ColorAttributeValue
                            {
                                Id = x.Id,
                                Color = x.Color,
                                Alias = x.Alias,
                                FriendlyName = x.GetLocalized(l => l.Name),
                                AttributeId = x.ProductVariantAttributeId,
                                AttributeName = attrName,
                                ProductAttributeId = attr.Id,
                                ProductUrl = _productUrlHelper.GetProductUrl(product.Id, item.SeName, 0, x)
                            };
                        })
                        .ToList();

                    item.ColorAttributes = colorAttributes;

                    // TODO: (mc) Resolve attribute value images also
                }

                // Variant Attributes
                if (attributes.Any() && settings.MapAttributes)
                {
                    if (item.ColorAttributes != null && item.ColorAttributes.Any())
                    {
                        var processedIds = item.ColorAttributes.Select(x => x.AttributeId).Distinct().ToArray();
                        attributes = attributes.Where(x => !processedIds.Contains(x.Id)).ToList();
                    }

                    foreach (var attr in attributes)
                    {
                        var pa = attr.ProductAttribute;
                        item.Attributes.Add(new ProductSummaryModel.Attribute
                        {
                            Id = attr.Id,
                            Alias = pa.Alias,
                            Name = cachedAttributeNames.Get(pa.Id) ?? (cachedAttributeNames[pa.Id] = pa.GetLocalized(l => l.Name))
                        });
                    }
                }
            }

            // Picture
            if (settings.MapPictures)
            {
                var thumbSize = model.ThumbSize ?? _mediaSettings.ProductThumbPictureSize;

                ctx.MediaFiles.TryGetValue(product.MainPictureId ?? 0, out var file);

                item.Image = new ImageModel
                {
                    File = file,
                    ThumbSize = thumbSize,
                    Title = file?.File?.GetLocalized(x => x.Title)?.Value.NullEmpty() ?? string.Format(ctx.Resources["Media.Product.ImageLinkTitleFormat"], item.Name),
                    Alt = file?.File?.GetLocalized(x => x.Alt)?.Value.NullEmpty() ?? string.Format(ctx.Resources["Media.Product.ImageAlternateTextFormat"], item.Name),
                    NoFallback = _catalogSettings.HideProductDefaultPictures
                };

                _services.DisplayControl.Announce(file?.File);
            }

            // Brands
            if (settings.MapManufacturers)
            {
                item.Brand = (await PrepareBrandOverviewModelAsync(
                    await ctx.BatchContext.ProductManufacturers.GetOrLoadAsync(product.Id),
                    ctx.CachedBrandModels,
                    _catalogSettings.ShowManufacturerLogoInLists && settings.ViewMode == ProductSummaryViewMode.List)).FirstOrDefault();
            }

            // Spec Attributes
            if (settings.MapSpecificationAttributes)
            {
                item.SpecificationAttributes.AddRange(MapProductSpecificationModels(await ctx.BatchContext.SpecificationAttributes.GetOrLoadAsync(product.Id)));
            }

            item.MinPriceProductId = contextProduct.Id;
            item.Sku = contextProduct.Sku;

            // Measure Dimensions
            if (model.ShowDimensions && (contextProduct.Width != 0 || contextProduct.Height != 0 || contextProduct.Length != 0))
            {
                item.Dimensions = ctx.Resources["Products.DimensionsValue"].Value.FormatCurrent(
                    contextProduct.Width.ToString("N2"),
                    contextProduct.Height.ToString("N2"),
                    contextProduct.Length.ToString("N2")
                );
                item.DimensionMeasureUnit = (await _db.MeasureDimensions.FindByIdAsync(_measureSettings.BaseDimensionId, false))?.SystemKeyword;
            }

            // Delivery Times.
            item.HideDeliveryTime = product.ProductType == ProductType.GroupedProduct;
            if (!item.HideDeliveryTime && model.DeliveryTimesPresentation != DeliveryTimesPresentation.None)
            {
                // We cannot include ManageInventoryMethod.ManageStockByAttributes because it's only functional with MergeWithCombination.
                // INFO: (core) Don't uncomment this part
                //item.StockAvailablity = contextProduct.FormatStockMessage(_localizationService);
                //item.DisplayDeliveryTimeAccordingToStock = contextProduct.DisplayDeliveryTimeAccordingToStock(_catalogSettings);

                //var deliveryTime = _deliveryTimeService.GetDeliveryTime(contextProduct);
                //if (deliveryTime != null)
                //{
                //	item.DeliveryTimeName = deliveryTime.GetLocalized(x => x.Name);
                //	item.DeliveryTimeHexValue = deliveryTime.ColorHexValue;
                //}

                var deliveryTimeId = product.DeliveryTimeId ?? 0;
                if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock && product.StockQuantity <= 0 && _catalogSettings.DeliveryTimeIdForEmptyStock.HasValue)
                {
                    deliveryTimeId = _catalogSettings.DeliveryTimeIdForEmptyStock.Value;
                }

                var deliveryTime = await _db.DeliveryTimes.FindByIdAsync(deliveryTimeId, false);
                if (deliveryTime != null)
                {
                    item.DeliveryTimeName = deliveryTime.GetLocalized(x => x.Name);
                    item.DeliveryTimeHexValue = deliveryTime.ColorHexValue;

                    // Due to lack of space, the grid view does not show a date for the delivery time.
                    if (settings.ViewMode >= ProductSummaryViewMode.List &&
                        (model.DeliveryTimesPresentation == DeliveryTimesPresentation.DateOnly || model.DeliveryTimesPresentation == DeliveryTimesPresentation.LabelAndDate))
                    {
                        item.DeliveryTimeDate = _deliveryTimeService.GetFormattedDeliveryDate(deliveryTime);
                    }
                }

                item.DisplayDeliveryTimeAccordingToStock = product.ManageInventoryMethod == ManageInventoryMethod.ManageStock
                    ? product.StockQuantity > 0 || (product.StockQuantity <= 0 && _catalogSettings.DeliveryTimeIdForEmptyStock.HasValue)
                    : true;

                if (product.DisplayStockAvailability && product.ManageInventoryMethod == ManageInventoryMethod.ManageStock)
                {
                    if (product.StockQuantity > 0)
                    {
                        item.StockAvailablity = product.DisplayStockQuantity
                            ? T("Products.Availability.InStockWithQuantity", product.StockQuantity)
                            : T("Products.Availability.InStock");
                    }
                    else
                    {
                        item.StockAvailablity = product.BackorderMode == BackorderMode.NoBackorders || product.BackorderMode == BackorderMode.AllowQtyBelow0
                            ? T("Products.Availability.OutOfStock")
                            : T("Products.Availability.Backordering");
                    }
                }
            }

            item.LegalInfo = ctx.LegalInfo;
            item.RatingSum = product.ApprovedRatingSum;
            item.TotalReviews = product.ApprovedTotalReviews;
            item.IsShippingEnabled = contextProduct.IsShippingEnabled;

            if (finalPrice != decimal.Zero && model.ShowBasePrice)
            {
                item.BasePriceInfo = _priceCalculationService.GetBasePriceInfo(contextProduct, finalPrice, options.TargetCurrency);
            }

            if (settings.MapPrices)
            {
                var addShippingPrice = ToWorkingCurrency(contextProduct.AdditionalShippingCharge, ctx);
                if (addShippingPrice > 0)
                {
                    item.TransportSurcharge = addShippingPrice.WithPostFormat(ctx.Resources["Common.AdditionalShippingSurcharge"]);
                }

                item.PriceDisplayStyle = _catalogSettings.PriceDisplayStyle;
                item.DisplayTextForZeroPrices = _catalogSettings.DisplayTextForZeroPrices;
            }

            if (model.ShowWeight && contextProduct.Weight > 0)
            {
                var measureWeightName = (await _db.MeasureWeights.FindByIdAsync(_measureSettings.BaseWeightId, false))?.GetLocalized(x => x.Name) ?? string.Empty;
                item.Weight = $"{contextProduct.Weight.ToString("N2")} {measureWeightName}";
            }

            // New Badge
            if (product.IsNew(_catalogSettings))
            {
                item.Badges.Add(new ProductSummaryModel.Badge
                {
                    Label = T("Common.New"),
                    Style = BadgeStyle.Success
                });
            }

            model.Items.Add(item);
        }

        /// <param name="contextProduct">The product or the first associated product of a group.</param>
        /// <returns>The final price</returns>
        private async Task<(Money FinalPrice, Product ContextProduct)> MapSummaryItemPrice(Product product, ProductSummaryModel.SummaryItem item, MapProductSummaryItemContext ctx)
        {
            var options = ctx.CalculationOptions;
            var batchContext = ctx.BatchContext;
            var contextProduct = product;
            ICollection<Product> associatedProducts = null;

            var priceModel = new ProductSummaryModel.PriceModel();
            item.Price = priceModel;

            if (product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing && !batchContext.ProductBundleItems.FullyLoaded)
            {
                await batchContext.ProductBundleItems.LoadAllAsync();
            }

            if (product.ProductType == ProductType.GroupedProduct)
            {
                priceModel.DisableBuyButton = true;
                priceModel.DisableWishlistButton = true;
                priceModel.AvailableForPreOrder = false;

                if (ctx.GroupedProducts == null)
                {
                    // One-time batched retrieval of all associated products.
                    var searchQuery = new CatalogSearchQuery()
                        .PublishedOnly(true)
                        .HasStoreId(options.Store.Id)
                        .HasParentGroupedProduct(batchContext.ProductIds.ToArray());

                    // Get all associated products for this batch grouped by ParentGroupedProductId.
                    var searchResult = await _catalogSearchService.SearchAsync(searchQuery);
                    var allAssociatedProducts = (await searchResult.GetHitsAsync())
                        .OrderBy(x => x.ParentGroupedProductId)
                        .ThenBy(x => x.DisplayOrder);

                    ctx.GroupedProducts = allAssociatedProducts.ToMultimap(x => x.ParentGroupedProductId, x => x);
                    ctx.AssociatedProductBatchContext = _productService.CreateProductBatchContext(allAssociatedProducts, options.Store, options.Customer, false);

                    options.ChildProductsBatchContext = ctx.AssociatedProductBatchContext;
                }

                associatedProducts = ctx.GroupedProducts[product.Id];
                if (associatedProducts.Any())
                {
                    contextProduct = associatedProducts.OrderBy(x => x.DisplayOrder).First();
                    _services.DisplayControl.Announce(contextProduct);
                }
            }
            else
            {
                priceModel.DisableBuyButton = product.DisableBuyButton || !ctx.AllowShoppingCart || !ctx.AllowPrices;
                priceModel.DisableWishlistButton = product.DisableWishlistButton || !ctx.AllowWishlist || !ctx.AllowPrices;
                priceModel.AvailableForPreOrder = product.AvailableForPreOrder;
            }

            // Return if there's no pricing at all.
            if (contextProduct == null || contextProduct.CustomerEntersPrice || !ctx.AllowPrices || _catalogSettings.PriceDisplayType == PriceDisplayType.Hide)
            {
                return (new Money(options.TargetCurrency), contextProduct);
            }

            // Return if group has no associated products.
            if (product.ProductType == ProductType.GroupedProduct && !associatedProducts.Any())
            {
                return (new Money(options.TargetCurrency), contextProduct);
            }

            // Call for price.
            priceModel.CallForPrice = contextProduct.CallForPrice;
            if (contextProduct.CallForPrice)
            {
                var money = new Money(options.TargetCurrency).WithPostFormat(ctx.Resources["Products.CallForPrice"]);
                priceModel.Price = money;
                return (money, contextProduct);
            }

            var calculationContext = new PriceCalculationContext(product, options)
            {
                AssociatedProducts = associatedProducts
            };

            // -----> Perform calculation <-------
            var calculatedPrice = await _priceCalculationService.CalculatePriceAsync(calculationContext);
            var savings = calculatedPrice.PriceSaving;

            priceModel.Price = calculatedPrice.FinalPrice;
            priceModel.HasDiscount = savings.HasSaving;

            if (savings.HasSaving)
            {
                priceModel.RegularPrice = savings.SavingPrice;
                priceModel.SavingAmount = savings.SavingAmount;
                priceModel.SavingPercent = savings.SavingPercent;

                if (ctx.Model.ShowDiscountBadge)
                {
                    item.Badges.Add(new ProductSummaryModel.Badge
                    {
                        Label = T("Products.SavingBadgeLabel", priceModel.SavingPercent.ToString("N0")),
                        Style = BadgeStyle.Danger
                    });
                }
            }

            return (calculatedPrice.FinalPrice, contextProduct);
        }

        private IEnumerable<ProductSpecificationModel> MapProductSpecificationModels(IEnumerable<ProductSpecificationAttribute> attributes)
        {
            Guard.NotNull(attributes, nameof(attributes));

            if (!attributes.Any())
                return Enumerable.Empty<ProductSpecificationModel>();

            var productId = attributes.First().ProductId;

            string cacheKey = string.Format(ModelCacheInvalidator.PRODUCT_SPECS_MODEL_KEY, productId, _workContext.WorkingLanguage.Id);
            return _cache.Get(cacheKey, () =>
            {
                var model = attributes.Select(psa =>
                {
                    return new ProductSpecificationModel
                    {
                        SpecificationAttributeId = psa.SpecificationAttributeOption.SpecificationAttributeId,
                        SpecificationAttributeName = psa.SpecificationAttributeOption.SpecificationAttribute.GetLocalized(x => x.Name),
                        SpecificationAttributeOption = psa.SpecificationAttributeOption.GetLocalized(x => x.Name)
                    };
                }).ToList();

                return model;
            });
        }

        #region Utils

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Money ToWorkingCurrency(decimal amount, MapProductSummaryItemContext ctx, bool showCurrency = true)
        {
            return _currencyService.ConvertToCurrency(new Money(amount, ctx.PrimaryCurrency, !showCurrency), ctx.CalculationOptions.TargetCurrency);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Money ToWorkingCurrency(Money amount, MapProductSummaryItemContext ctx)
        {
            return _currencyService.ConvertToCurrency(amount, ctx.CalculationOptions.TargetCurrency);
        }

        #endregion

        private class MapProductSummaryItemContext
        {
            public ProductSummaryModel Model { get; set; }
            public ProductSummaryMappingSettings MappingSettings { get; set; }
            public ProductBatchContext BatchContext { get; set; }
            public PriceCalculationOptions CalculationOptions { get; set; }
            public ProductBatchContext AssociatedProductBatchContext { get; set; }
            public Multimap<int, Product> GroupedProducts { get; set; }
            public Dictionary<int, BrandOverviewModel> CachedBrandModels { get; set; }
            public Dictionary<int, MediaFileInfo> MediaFiles { get; set; } = new Dictionary<int, MediaFileInfo>();
            public Dictionary<string, LocalizedString> Resources { get; set; }
            public string LegalInfo { get; set; }
            public Currency PrimaryCurrency { get; set; }

            public bool AllowPrices { get; set; }
            public bool AllowShoppingCart { get; set; }
            public bool AllowWishlist { get; set; }
            public string ShippingChargeTaxFormat { get; set; }
        }
    }

    public class ProductSummaryMappingSettings
    {
        public ProductSummaryMappingSettings()
        {
            MapPrices = true;
            MapPictures = true;
            ViewMode = ProductSummaryViewMode.Grid;
        }

        public ProductSummaryViewMode ViewMode { get; set; }

        public bool MapPrices { get; set; }
        public bool MapPictures { get; set; }
        public bool MapDimensions { get; set; }
        public bool MapSpecificationAttributes { get; set; }
        public bool MapColorAttributes { get; set; }
        public bool MapAttributes { get; set; }
        public bool MapManufacturers { get; set; }
        public bool MapShortDescription { get; set; }
        public bool MapFullDescription { get; set; }
        public bool MapLegalInfo { get; set; }
        public bool MapReviews { get; set; }
        public DeliveryTimesPresentation DeliveryTimesPresentation { get; set; }

        public bool ForceRedirectionAfterAddingToCart { get; set; }
        public int? ThumbnailSize { get; set; }

        public bool? PrefetchTranslations { get; set; }
        public bool? PrefetchUrlSlugs { get; set; }
    }
}
