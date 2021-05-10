using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.DataExchange.Export.Internal;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Utilities;
using Smartstore.Utilities.Html;

namespace Smartstore.Core.DataExchange.Export
{
    public partial class DataExporter
    {
        // TODO: (mg) (core) add code comments when ready.

        private async Task<dynamic> ToDynamic(Product product, DataExporterContext ctx, string seName = null, Money? price = null)
        {
            if (product == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(product);
            var translations = ctx.TranslationsPerPage[nameof(Product)];
            var urlRecords = ctx.UrlRecordsPerPage[nameof(Product)];
            var localizedName = translations.GetValue(ctx.LanguageId, product.Id, nameof(product.Name)) ?? product.Name;

            result.AppliedDiscounts = null;
            result.Downloads = null;
            result.TierPrices = null;
            result.ProductAttributes = null;
            result.ProductAttributeCombinations = null;
            result.ProductPictures = null;
            result.ProductCategories = null;
            result.ProductManufacturers = null;
            result.ProductTags = null;
            result.ProductSpecificationAttributes = null;
            result.ProductBundleItems = null;
            result.Name = localizedName;

            if (!ctx.IsPreview)
            {
                result.SeName = seName ?? ctx.UrlRecordsPerPage[nameof(Product)].GetSlug(ctx.LanguageId, product.Id);
                result.ShortDescription = translations.GetValue(ctx.LanguageId, product.Id, nameof(product.ShortDescription)) ?? product.ShortDescription;
                result.FullDescription = translations.GetValue(ctx.LanguageId, product.Id, nameof(product.FullDescription)) ?? product.FullDescription;
                result.MetaKeywords = translations.GetValue(ctx.LanguageId, product.Id, nameof(product.MetaKeywords)) ?? product.MetaKeywords;
                result.MetaDescription = translations.GetValue(ctx.LanguageId, product.Id, nameof(product.MetaDescription)) ?? product.MetaDescription;
                result.MetaTitle = translations.GetValue(ctx.LanguageId, product.Id, nameof(product.MetaTitle)) ?? product.MetaTitle;
                result.BundleTitleText = translations.GetValue(ctx.LanguageId, product.Id, nameof(product.BundleTitleText)) ?? product.BundleTitleText;

                result._ProductTemplateViewPath = ctx.ProductTemplates.ContainsKey(product.ProductTemplateId)
                    ? ctx.ProductTemplates[product.ProductTemplateId]
                    : string.Empty;

                if (product.BasePriceHasValue && product.BasePriceAmount != 0)
                {
                    if (price == null)
                    {
                        var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, ctx.ContextCustomer, ctx.ContextCurrency, ctx.ProductBatchContext);
                        var productPrice = await _priceCalculationService.CalculatePriceAsync(new PriceCalculationContext(product, calculationOptions));

                        price = productPrice.FinalPrice;
                    }

                    result._BasePriceInfo = _priceCalculationService.GetBasePriceInfo(product, price.Value, ctx.ContextCurrency, ctx.ContextLanguage, false);
                }
                else
                {
                    result._BasePriceInfo = string.Empty;
                }

                result.DeliveryTime = ctx.DeliveryTimes.TryGetValue(product.DeliveryTimeId ?? 0, out var deliveryTime)
                    ? ToDynamic(deliveryTime, ctx)
                    : null;

                result.QuantityUnit = ctx.QuantityUnits.TryGetValue(product.QuantityUnitId ?? 0, out var quantityUnit)
                    ? ToDynamic(quantityUnit, ctx)
                    : null;

                result.CountryOfOrigin = product.CountryOfOriginId.HasValue
                    ? ToDynamic(product.CountryOfOrigin, ctx)
                    : null;

                result._Localized = GetLocalized(ctx, translations, urlRecords, product,
                    x => x.Name,
                    x => x.ShortDescription,
                    x => x.FullDescription,
                    x => x.MetaKeywords,
                    x => x.MetaDescription,
                    x => x.MetaTitle,
                    x => x.BundleTitleText);
            }

            return result;
        }

        private async Task<dynamic> ToDynamic(Product product, bool isParent, DataExporterContext ctx, DynamicProductContext productContext)
        {
            var combination = productContext.Combination;

            product.MergeWithCombination(combination);

            var productManufacturers = await ctx.ProductBatchContext.ProductManufacturers.GetOrLoadAsync(product.Id);
            var productCategories = await ctx.ProductBatchContext.ProductCategories.GetOrLoadAsync(product.Id);
            var productAttributes = await ctx.ProductBatchContext.Attributes.GetOrLoadAsync(product.Id);
            var productTags = await ctx.ProductBatchContext.ProductTags.GetOrLoadAsync(product.Id);
            var specificationAttributes = await ctx.ProductBatchContext.SpecificationAttributes.GetOrLoadAsync(product.Id);
            var selectedAttributes = combination?.AttributeSelection;
            var variantAttributeValues = combination?.AttributeSelection?.MaterializeProductVariantAttributeValues(productAttributes);

            // Price calculation.
            var calculationContext = new PriceCalculationContext(product, ctx.PriceCalculationOptions);
            calculationContext.AddSelectedAttributes(combination?.AttributeSelection, product.Id);
            var price = (await _priceCalculationService.CalculatePriceAsync(calculationContext)).FinalPrice;

            dynamic dynObject = ToDynamic(product, ctx, productContext.SeName, price);
            dynObject._IsParent = isParent;
            dynObject._CategoryName = null;
            dynObject._CategoryPath = null;
            dynObject._AttributeCombinationValues = null;
            dynObject._AttributeCombinationId = 0;

            dynObject.Price = price.Amount;

            if (combination != null)
            {
                dynObject._AttributeCombinationId = combination.Id;
                dynObject._UniqueId = product.Id + "-" + combination.Id;

                if (ctx.Supports(ExportFeatures.UsesAttributeCombination))
                {
                    dynObject._AttributeCombinationValues = variantAttributeValues;
                }

                if (ctx.Projection.AttributeCombinationValueMerging == ExportAttributeValueMerging.AppendAllValuesToName)
                {
                    var translations = ctx.TranslationsPerPage[nameof(ProductVariantAttributeValue)];
                    var valueNames = variantAttributeValues
                        .Select(x => translations.GetValue(ctx.LanguageId, x.Id, nameof(x.Name)) ?? x.Name)
                        .ToList();

                    dynObject.Name = ((string)dynObject.Name).Grow(string.Join(", ", valueNames), " ");
                }
            }
            else
            {
                dynObject._UniqueId = product.Id.ToString();
            }

            if (selectedAttributes?.AttributesMap?.Any() ?? false)
            {
                var query = new ProductVariantQuery();
                await _productUrlHelper.AddAttributesToQueryAsync(query, selectedAttributes, product.Id, 0, productAttributes);

                dynObject._DetailUrl = productContext.AbsoluteProductUrl + _productUrlHelper.ToQueryString(query);
            }
            else
            {
                dynObject._DetailUrl = productContext.AbsoluteProductUrl;
            }

            // Category path.
            {
                var categoryPath = string.Empty;
                var pc = productCategories.OrderBy(x => x.DisplayOrder).FirstOrDefault();
                if (pc != null)
                {
                    var node = await _categoryService.GetCategoryTreeAsync(pc.CategoryId, true, ctx.Store.Id);
                    if (node != null)
                    {
                        categoryPath = _categoryService.GetCategoryPath(node, ctx.Projection.LanguageId, null, " > ");
                    }
                }

                dynObject._CategoryPath = categoryPath;
            }

            dynObject.CountryOfOrigin = ctx.Countries.TryGetValue(product.CountryOfOriginId ?? 0, out var countryOfOrigin)
                ? ToDynamic(countryOfOrigin, ctx)
                : null;

            dynObject.ProductManufacturers = productManufacturers
                .OrderBy(x => x.DisplayOrder)
                .Select(x =>
                {
                    dynamic dyn = new DynamicEntity(x);
                    dyn.Manufacturer = ToDynamic(x.Manufacturer, ctx);
                    dyn.Manufacturer.Picture = x.Manufacturer != null && x.Manufacturer.MediaFileId.HasValue
                        ? ToDynamic(x.Manufacturer.MediaFile, _mediaSettings.ManufacturerThumbPictureSize, _mediaSettings.ManufacturerThumbPictureSize, ctx)
                        : null;

                    return dyn;
                })
                .ToList();

            dynObject.ProductCategories = productCategories
                .OrderBy(x => x.DisplayOrder)
                .Select(x =>
                {
                    dynamic dyn = new DynamicEntity(x);
                    dyn.Category = ToDynamic(x.Category, ctx);
                    dyn.Category.Picture = x.Category != null && x.Category.MediaFileId.HasValue
                        ? ToDynamic(x.Category.MediaFile, _mediaSettings.CategoryThumbPictureSize, _mediaSettings.CategoryThumbPictureSize, ctx)
                        : null;

                    if (dynObject._CategoryName == null)
                    {
                        dynObject._CategoryName = (string)dyn.Category.Name;
                    }

                    return dyn;
                })
                .ToList();

            dynObject.ProductAttributes = productAttributes
                .OrderBy(x => x.DisplayOrder)
                .Select(x => ToDynamic(x, ctx))
                .ToList();

            // Do not export combinations if a combination is exported as a product.
            if (productContext.Combinations != null && productContext.Combination == null)
            {
                var pictureSize = ctx.Projection.PictureSize > 0 ? ctx.Projection.PictureSize : _mediaSettings.ProductDetailsPictureSize;
                var productMediaFiles = (await ctx.ProductBatchContext.ProductMediaFiles.GetOrLoadAsync(product.Id))
                    .ToDictionarySafe(x => x.MediaFileId, x => x);

                dynObject.ProductAttributeCombinations = productContext.Combinations
                    .Select(x =>
                    {
                        dynamic dyn = DataExporter.ToDynamic(x, ctx);
                        var assignedFiles = new List<dynamic>();

                        foreach (var fileId in x.GetAssignedMediaIds().Take(ctx.Projection.NumberOfMediaFiles ?? int.MaxValue))
                        {
                            if (productMediaFiles.TryGetValue(fileId, out var assignedFile) && assignedFile.MediaFile != null)
                            {
                                assignedFiles.Add(ToDynamic(assignedFile.MediaFile, _mediaSettings.ProductThumbPictureSize, pictureSize, ctx));
                            }
                        }

                        dyn.Pictures = assignedFiles;
                        return dyn;
                    })
                    .ToList();
            }
            else
            {
                dynObject.ProductAttributeCombinations = Enumerable.Empty<ProductVariantAttributeCombination>();
            }

            if (product.HasTierPrices)
            {
                var tierPrices = await ctx.ProductBatchContext.TierPrices.GetOrLoadAsync(product.Id);

                dynObject.TierPrices = tierPrices
                    .RemoveDuplicatedQuantities()
                    .Select(x =>
                    {
                        dynamic dyn = new DynamicEntity(x);
                        return dyn;
                    })
                    .ToList();
            }

            if (product.HasDiscountsApplied)
            {
                var appliedDiscounts = await ctx.ProductBatchContext.AppliedDiscounts.GetOrLoadAsync(product.Id);

                dynObject.AppliedDiscounts = appliedDiscounts
                    .Select(x => ToDynamic(x))
                    .ToList();
            }

            if (product.IsDownload)
            {
                var downloads = await ctx.ProductBatchContext.Downloads.GetOrLoadAsync(product.Id);

                dynObject.Downloads = downloads
                    .Select(x => ToDynamic(x))
                    .ToList();
            }

            dynObject.ProductTags = productTags
                .Select(x =>
                {
                    var translations = ctx.TranslationsPerPage[nameof(ProductTag)];
                    var localizedName = translations.GetValue(ctx.LanguageId, x.Id, nameof(x.Name)) ?? x.Name;
                    dynamic dyn = new DynamicEntity(x);
                    dyn.Name = localizedName;
                    dyn.SeName = SeoHelper.BuildSlug(localizedName, _seoSettings);
                    dyn._Localized = GetLocalized(ctx, translations, null, x, y => y.Name);

                    return dyn;
                })
                .ToList();

            dynObject.ProductSpecificationAttributes = specificationAttributes
                .Select(x => ToDynamic(x, ctx))
                .ToList();

            if (product.ProductType == ProductType.BundledProduct)
            {
                var bundleItems = await ctx.ProductBatchContext.ProductBundleItems.GetOrLoadAsync(product.Id);

                dynObject.ProductBundleItems = bundleItems
                    .Select(x =>
                    {
                        var translations = ctx.TranslationsPerPage[nameof(ProductBundleItem)];
                        dynamic dyn = new DynamicEntity(x);
                        dyn.Name = translations.GetValue(ctx.LanguageId, x.Id, nameof(x.Name)) ?? x.Name;
                        dyn.ShortDescription = translations.GetValue(ctx.LanguageId, x.Id, nameof(x.ShortDescription)) ?? x.ShortDescription;
                        dyn._Localized = GetLocalized(ctx, translations, null, x, y => y.Name, y => y.ShortDescription);

                        return dyn;
                    })
                    .ToList();
            }

            var mediaFiles = await ApplyMediaFiles(dynObject, product, ctx, productContext);
            await ApplyExportFeatures(dynObject, product, mediaFiles, ctx, productContext);

            return dynObject;
        }

        private async Task<IEnumerable<ProductMediaFile>> ApplyMediaFiles(dynamic dynObject, Product product, DataExporterContext ctx, DynamicProductContext productContext)
        {
            IEnumerable<ProductMediaFile> mediaFiles = await ctx.ProductBatchContext.ProductMediaFiles.GetOrLoadAsync(product.Id);
            var productPictureSize = ctx.Projection.PictureSize > 0 ? ctx.Projection.PictureSize : _mediaSettings.ProductDetailsPictureSize;

            if (productContext.Combination != null)
            {
                var mediaIds = productContext.Combination.GetAssignedMediaIds();
                if (mediaIds.Any())
                {
                    mediaFiles = mediaFiles.Where(x => mediaIds.Contains(x.MediaFileId));
                }
            }

            mediaFiles = mediaFiles.Take(ctx.Projection.NumberOfMediaFiles ?? int.MaxValue);

            dynObject.ProductPictures = mediaFiles
                .OrderBy(x => x.DisplayOrder)
                .Select(x =>
                {
                    dynamic dyn = new DynamicEntity(x);
                    dyn.Picture = ToDynamic(x.MediaFile, _mediaSettings.ProductThumbPictureSize, productPictureSize, ctx);
                    return dyn;
                })
                .ToList();

            return mediaFiles;
        }

        private async Task ApplyExportFeatures(
            dynamic dynObject,
            Product product, 
            IEnumerable<ProductMediaFile> mediaFiles, 
            DataExporterContext ctx, 
            DynamicProductContext productContext)
        {
            if (ctx.Supports(ExportFeatures.CanProjectDescription))
            {
                await ApplyProductDescription(dynObject, product, ctx);
            }

            if (ctx.Supports(ExportFeatures.OffersBrandFallback))
            {
                string brand = null;
                var productManus = await ctx.ProductBatchContext.ProductManufacturers.GetOrLoadAsync(product.Id);

                if (productManus != null && productManus.Any())
                {
                    var translations = ctx.Translations[nameof(Manufacturer)];
                    var manufacturer = productManus.First().Manufacturer;
                    brand = translations.GetValue(ctx.LanguageId, manufacturer.Id, nameof(manufacturer.Name)) ?? manufacturer.Name;
                }
                if (brand.IsEmpty())
                {
                    brand = ctx.Projection.Brand;
                }

                dynObject._Brand = brand;
            }

            if (ctx.Supports(ExportFeatures.CanIncludeMainPicture))
            {
                var imageQuery = ctx.Projection.PictureSize > 0 ? new ProcessImageQuery { MaxWidth = ctx.Projection.PictureSize } : null;

                if (mediaFiles?.Any() ?? false)
                {
                    var file = _mediaService.ConvertMediaFile(mediaFiles.Select(x => x.MediaFile).First());
                    
                    dynObject._MainPictureUrl = _mediaService.GetUrl(file, imageQuery, ctx.Store.GetHost());
                    dynObject._MainPictureRelativeUrl = _mediaService.GetUrl(file, imageQuery);
                }
                else if (!_catalogSettings.HideProductDefaultPictures)
                {
                    // Get fallback image URL.
                    dynObject._MainPictureUrl = _mediaService.GetUrl(null, imageQuery, ctx.Store.GetHost());
                    dynObject._MainPictureRelativeUrl = _mediaService.GetUrl(null, imageQuery);
                }
                else
                {
                    dynObject._MainPictureUrl = null;
                    dynObject._MainPictureRelativeUrl = null;
                }
            }

            if (ctx.Supports(ExportFeatures.UsesSkuAsMpnFallback) && product.ManufacturerPartNumber.IsEmpty())
            {
                dynObject.ManufacturerPartNumber = product.Sku;
            }

            if (ctx.Supports(ExportFeatures.OffersShippingTimeFallback))
            {
                dynamic deliveryTime = dynObject.DeliveryTime;
                dynObject._ShippingTime = deliveryTime == null ? ctx.Projection.ShippingTime : deliveryTime.Name;
            }

            if (ctx.Supports(ExportFeatures.OffersShippingCostsFallback))
            {
                dynObject._FreeShippingThreshold = ctx.Projection.FreeShippingThreshold;

                dynObject._ShippingCosts = product.IsFreeShipping || (ctx.Projection.FreeShippingThreshold.HasValue && (decimal)dynObject.Price >= ctx.Projection.FreeShippingThreshold.Value)
                    ? decimal.Zero
                    : ctx.Projection.ShippingCosts;
            }

            //...
        }

        private static async Task ApplyProductDescription(dynamic dynObject, Product product, DataExporterContext ctx)
        {
            try
            {
                var languageId = ctx.LanguageId;
                string description = "";

                // Description merging.
                if (ctx.Projection.DescriptionMerging == ExportDescriptionMerging.None)
                {
                    // Export empty description.
                }
                else if (ctx.Projection.DescriptionMerging == ExportDescriptionMerging.ShortDescriptionOrNameIfEmpty)
                {
                    description = dynObject.FullDescription;

                    if (description.IsEmpty())
                        description = dynObject.ShortDescription;
                    if (description.IsEmpty())
                        description = dynObject.Name;
                }
                else if (ctx.Projection.DescriptionMerging == ExportDescriptionMerging.ShortDescription)
                {
                    description = dynObject.ShortDescription;
                }
                else if (ctx.Projection.DescriptionMerging == ExportDescriptionMerging.Description)
                {
                    description = dynObject.FullDescription;
                }
                else if (ctx.Projection.DescriptionMerging == ExportDescriptionMerging.NameAndShortDescription)
                {
                    description = ((string)dynObject.Name).Grow((string)dynObject.ShortDescription, " ");
                }
                else if (ctx.Projection.DescriptionMerging == ExportDescriptionMerging.NameAndDescription)
                {
                    description = ((string)dynObject.Name).Grow((string)dynObject.FullDescription, " ");
                }
                else if (ctx.Projection.DescriptionMerging == ExportDescriptionMerging.ManufacturerAndNameAndShortDescription ||
                    ctx.Projection.DescriptionMerging == ExportDescriptionMerging.ManufacturerAndNameAndDescription)
                {
                    var productManus = await ctx.ProductBatchContext.ProductManufacturers.GetOrLoadAsync(product.Id);

                    if (productManus != null && productManus.Any())
                    {
                        var translations = ctx.Translations[nameof(Manufacturer)];
                        var manufacturer = productManus.First().Manufacturer;
                        description = translations.GetValue(languageId, manufacturer.Id, nameof(manufacturer.Name)) ?? manufacturer.Name;
                    }

                    description = description.Grow((string)dynObject.Name, " ");
                    description = ctx.Projection.DescriptionMerging == ExportDescriptionMerging.ManufacturerAndNameAndShortDescription
                        ? description.Grow((string)dynObject.ShortDescription, " ")
                        : description.Grow((string)dynObject.FullDescription, " ");
                }

                // Append text.
                if (ctx.Projection.AppendDescriptionText.HasValue() && ((string)dynObject.ShortDescription).IsEmpty() && ((string)dynObject.FullDescription).IsEmpty())
                {
                    var appendText = ctx.Projection.AppendDescriptionText.SplitSafe(",").ToArray();
                    if (appendText.Any())
                    {
                        var rnd = CommonHelper.GenerateRandomInteger(0, appendText.Length - 1);
                        description = description.Grow(appendText.ElementAtOrDefault(rnd), " ");
                    }
                }

                // Remove critical characters.
                if (description.HasValue() && ctx.Projection.RemoveCriticalCharacters)
                {
                    foreach (var str in ctx.Projection.CriticalCharacters.SplitSafe(","))
                    {
                        description = description.Replace(str, "");
                    }
                }

                // Convert to plain text.
                if (description.HasValue() && ctx.Projection.DescriptionToPlainText)
                {
                    //Regex reg = new Regex("<[^>]+>", RegexOptions.IgnoreCase);
                    //description = HttpUtility.HtmlDecode(reg.Replace(description, ""));

                    description = HtmlUtils.ConvertHtmlToPlainText(description);
                    description = HtmlUtils.StripTags(HttpUtility.HtmlDecode(description));
                }

                dynObject.FullDescription = description.TrimSafe();
            }
            catch { }
        }
    }

    internal class DynamicProductContext
    {
        public string SeName { get; set; }
        public string AbsoluteProductUrl { get; set; }
        public ICollection<ProductVariantAttributeCombination> Combinations { get; set; }
        public ProductVariantAttributeCombination Combination { get; set; }
    }
}
