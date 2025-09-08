using System.Threading;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.DataExchange;
using Smartstore.Core.DataExchange.Export;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Google.MerchantCenter.Components;
using Smartstore.Google.MerchantCenter.Domain;
using Smartstore.Google.MerchantCenter.Models;

namespace Smartstore.Google.MerchantCenter.Providers
{
    [SystemName("Feeds.GoogleMerchantCenterProductXml")]
    [FriendlyName("Google Merchant Center XML product feed")]
    [Order(1)]
    [ExportFeatures(Features =
        ExportFeatures.CreatesInitialPublicDeployment |
        ExportFeatures.CanOmitGroupedProducts |
        ExportFeatures.CanProjectAttributeCombinations |
        ExportFeatures.CanProjectDescription |
        ExportFeatures.UsesSkuAsMpnFallback |
        ExportFeatures.OffersBrandFallback |
        ExportFeatures.OffersShippingTimeFallback |
        ExportFeatures.UsesAttributeCombination)]
    public class GmcXmlExportProvider : ExportProviderBase
    {
        const int MaxImages = 11;   // One "image_link" plus up to ten "additional_image_link".
        const string GoogleNamespace = "http://base.google.com/ns/1.0";
        const string DateTimeFormat = "yyyy-MM-ddTHH:mmZ";
        const string DateFormat = "yyyy-MM-dd";

        public const string SystemName = "Feeds.GoogleMerchantCenterProductXml";
        public const string Unspecified = "__nospec__";

        private readonly SmartDbContext _db;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IRoundingHelper _roundingHelper;
        private readonly MeasureSettings _measureSettings;
        private readonly ShippingSettings _shippingSettings;

        private Multimap<string, int> _attributeMappings;

        public GmcXmlExportProvider(
            SmartDbContext db,
            IProductAttributeService productAttributeService,
            IRoundingHelper roundingHelper,
            MeasureSettings measureSettings,
            ShippingSettings shippingSettings)
        {
            _db = db;
            _productAttributeService = productAttributeService;
            _roundingHelper = roundingHelper;
            _measureSettings = measureSettings;
            _shippingSettings = shippingSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public override ExportConfigurationInfo ConfigurationInfo => new()
        {
            ConfigurationWidget = new ComponentWidget(typeof(GmcConfigurationViewComponent)),
            ModelType = typeof(ProfileConfigurationModel)
        };

        public override string FileExtension => "XML";

        protected override async Task ExportAsync(ExportExecuteContext context, CancellationToken cancelToken)
        {
            Currency currency = context.Currency.Entity;
            var config = (context.ConfigurationData as ProfileConfigurationModel) ?? new ProfileConfigurationModel();
            var now = DateTime.UtcNow;
            var languageId = context.Projection.LanguageId ?? 0;
            var baseMeasureWeight = await GetBaseMeasureWeight();
            var baseMeasureDimension = await GetBaseMeasureDimension();
            var shippingOriginAddress = await _db.Addresses
                .Include(x => x.Country)
                .FindByIdAsync(_shippingSettings.ShippingOriginAddressId, false, cancelToken);
            var shippingOriginCountryCode = shippingOriginAddress?.Country?.TwoLetterIsoCode.NullEmpty();
            var deliveryTimes = await _db.DeliveryTimes
                .AsNoTracking()
                .Where(x => x.MaxDays != null && x.MaxDays <= 10)
                .ToDictionaryAsync(x => x.Id, cancelToken);

            _attributeMappings = await _productAttributeService.GetExportFieldMappingsAsync("gmc");

            var defaultAvailability = "in stock";
            if (config.Availability.EqualsNoCase(Unspecified))
            {
                defaultAvailability = string.Empty;
            }
            else if (config.Availability.HasValue())
            {
                defaultAvailability = config.Availability;
            }

            using var writer = XmlWriter.Create(context.DataStream, ExportXmlHelper.DefaultSettings);
            writer.WriteStartDocument();
            writer.WriteStartElement("rss");
            writer.WriteAttributeString("version", "2.0");
            writer.WriteAttributeString("xmlns", "g", null, GoogleNamespace);
            writer.WriteStartElement("channel");
            writer.WriteElementString("title", $"{(string)context.Store.Name} - Feed for Google Merchant Center");
            writer.WriteElementString("link", "http://base.google.com/base/");
            writer.WriteElementString("description", "Information about products");

            while (context.Abort == DataExchangeAbortion.None && await context.DataSegmenter.ReadNextSegmentAsync())
            {
                var segment = await context.DataSegmenter.GetCurrentSegmentAsync();
                int[] productIds = [.. segment.Select(x => (int)x.Id)];
                var googleProducts = (await _db.GoogleProducts()
                    .AsNoTracking()
                    .Where(x => productIds.Contains(x.ProductId))
                    .ToListAsync(cancelToken))
                    .ToDictionarySafe(x => x.ProductId);

                foreach (dynamic product in segment)
                {
                    if (context.Abort != DataExchangeAbortion.None)
                        break;

                    Product entity = product.Entity;
                    var googleProduct = googleProducts.Get(entity.Id);
                    if (googleProduct != null && !googleProduct.Export)
                        continue;

                    writer.WriteStartElement("item");

                    try
                    {
                        string productType = product._CategoryPath;
                        var isParent = (bool)product._IsParent;
                        string brand = product._Brand;
                        string gtin = product.Gtin;
                        string mpn = product.ManufacturerPartNumber;
                        var identifierExists = brand.HasValue() && (gtin.HasValue() || mpn.HasValue());
                        var attributeValues = !isParent && product._AttributeCombinationValues != null
                            ? ((IList<ProductVariantAttributeValue>)product._AttributeCombinationValues).ToMultimap(x => x.ProductVariantAttribute.ProductAttributeId, x => x)
                            : [];

                        var category = googleProduct?.Taxonomy?.NullEmpty() ?? config.DefaultGoogleCategory;
                        if (category.IsEmpty())
                        {
                            context.Log.Error(T("Plugins.Feed.Froogle.MissingDefaultCategory"));
                        }

                        WriteString(writer, "id", (string)product._UniqueId);
                        writer.WriteCData("title", ((string)product.Name).Truncate(70));
                        writer.WriteCData("description", (string)product.FullDescription);
                        writer.WriteCData("google_product_category", category, "g", GoogleNamespace);
                        writer.WriteCData("product_type", productType.NullEmpty(), "g", GoogleNamespace);
                        writer.WriteElementString("link", (string)product._DetailUrl);

                        switch (entity.Condition)
                        {
                            case ProductCondition.Damaged:
                            case ProductCondition.Used:
                                WriteString(writer, "condition", "used");
                                break;
                            case ProductCondition.Refurbished:
                                WriteString(writer, "condition", "refurbished");
                                break;
                            case ProductCondition.New:
                            default:
                                WriteString(writer, "condition", "new");
                                break;
                        }

                        ExportAvailability(writer, product, defaultAvailability);
                        ExportImages(writer, product, googleProduct, config);
                        ExportPrices(writer, product, config, currency);

                        WriteString(writer, "gtin", gtin);
                        WriteString(writer, "brand", brand);
                        WriteString(writer, "mpn", mpn);
                        WriteString(writer, "identifier_exists", identifierExists ? "yes" : "no");
                        WriteString(writer, "gender", GetAttribute(attributeValues, "gender", languageId, googleProduct?.Gender, config.Gender));
                        WriteString(writer, "age_group", GetAttribute(attributeValues, "age_group", languageId, googleProduct?.AgeGroup, config.AgeGroup));
                        WriteString(writer, "color", GetAttribute(attributeValues, "color", languageId, googleProduct?.Color, config.Color));
                        WriteString(writer, "size", GetAttribute(attributeValues, "size", languageId, googleProduct?.Size, config.Size));
                        WriteString(writer, "material", GetAttribute(attributeValues, "material", languageId, googleProduct?.Material, config.Material));
                        WriteString(writer, "pattern", GetAttribute(attributeValues, "pattern", languageId, googleProduct?.Pattern, config.Pattern));   
                        WriteString(writer, "item_group_id", googleProduct?.ItemGroupId?.NullEmpty());

                        if (config.ExpirationDays > 0)
                        {
                            WriteString(writer, "expiration_date", now.AddDays(config.ExpirationDays).ToString(DateFormat));
                        }

                        ExportShipping(writer, product, currency, config, deliveryTimes, baseMeasureWeight, baseMeasureDimension, shippingOriginCountryCode);

                        if (googleProduct != null)
                        {
                            WriteString(writer, "multipack", googleProduct.Multipack > 1 ? googleProduct.Multipack.ToString() : null);
                            WriteString(writer, "is_bundle", googleProduct.IsBundle.HasValue ? (googleProduct.IsBundle.Value ? "yes" : "no") : null);
                            WriteString(writer, "adult", googleProduct.IsAdult.HasValue ? (googleProduct.IsAdult.Value ? "yes" : "no") : null);

                            if (googleProduct.EnergyEfficiencyClass.HasValue())
                            {
                                writer.WriteStartElement("g", "certification", GoogleNamespace);
                                WriteString(writer, "certification_authority", "EC");
                                WriteString(writer, "certification_name", "EPREL");
                                // EPREL code: A, B, ... G. Rescaled version, no "+" signs anymore (like A+++).
                                WriteString(writer, "certification_code", googleProduct.EnergyEfficiencyClass.Trim());
                                writer.WriteEndElement();
                            }
                        }

                        WriteString(writer, "custom_label_0", GetAttribute(attributeValues, "custom_label_0", languageId, googleProduct?.CustomLabel0, null).NullEmpty());
                        WriteString(writer, "custom_label_1", GetAttribute(attributeValues, "custom_label_1", languageId, googleProduct?.CustomLabel1, null).NullEmpty());
                        WriteString(writer, "custom_label_2", GetAttribute(attributeValues, "custom_label_2", languageId, googleProduct?.CustomLabel2, null).NullEmpty());
                        WriteString(writer, "custom_label_3", GetAttribute(attributeValues, "custom_label_3", languageId, googleProduct?.CustomLabel3, null).NullEmpty());
                        WriteString(writer, "custom_label_4", GetAttribute(attributeValues, "custom_label_4", languageId, googleProduct?.CustomLabel4, null).NullEmpty());

                        ++context.RecordsSucceeded;
                    }
                    catch (OutOfMemoryException ex)
                    {
                        context.RecordOutOfMemoryException(ex, entity.Id, T);
                        context.Abort = DataExchangeAbortion.Hard;
                        throw;
                    }
                    catch (Exception ex)
                    {
                        context.RecordException(ex, entity.Id);
                    }

                    writer.WriteEndElement(); // item
                }
            }

            writer.WriteEndElement(); // channel
            writer.WriteEndElement(); // rss
            writer.WriteEndDocument();
        }

        private static void ExportAvailability(
            XmlWriter writer,
            dynamic product,
            string defaultAvailability)
        {
            Product entity = product.Entity;
            string availability = null;

            if (entity.ManageInventoryMethod == ManageInventoryMethod.ManageStock && entity.StockQuantity <= 0)
            {
                if (entity.BackorderMode == BackorderMode.NoBackorders)
                {
                    availability = "out of stock";
                }
                else if (entity.BackorderMode == BackorderMode.AllowQtyBelow0 || entity.BackorderMode == BackorderMode.AllowQtyBelow0OnBackorder)
                {
                    availability = entity.AvailableForPreOrder ? "preorder" : "out of stock";
                }
            }
            else if (entity.ManageInventoryMethod == ManageInventoryMethod.DontManageStock && entity.DisableBuyButton)
            {
                availability = "out of stock";
            }

            availability ??= defaultAvailability;

            WriteString(writer, "availability", availability);
            if (availability == "preorder" && entity.AvailableStartDateTimeUtc.HasValue && entity.AvailableStartDateTimeUtc.Value > DateTime.UtcNow)
            {
                WriteString(writer, "availability_date", entity.AvailableStartDateTimeUtc.Value.ToString(DateTimeFormat));
            }
        }

        private static void ExportImages(
            XmlWriter writer,
            dynamic product,
            GoogleProduct googleProduct,
            ProfileConfigurationModel config)
        {
            var fileIds = googleProduct?.MediaFileIds?.ToIntArray() ?? [];
            var images = ((List<dynamic>)product.ProductMediaFiles)
                .Select(x =>
                {
                    ProductMediaFile fileEntity = x.Entity;
                    string imageUrl = x.File._FullSizeImageUrl;

                    if (fileEntity.MediaFile.MediaType != "image"
                        || imageUrl.IsEmpty()
                        || (fileIds.Length > 0 && !fileIds.Contains(fileEntity.MediaFileId)))
                    {
                        return null;
                    }

                    return new GmcImage
                    {
                        Id = fileEntity.MediaFileId,
                        Url = imageUrl
                    };
                })
                .Where(x => x != null)
                .Take(config.AdditionalImages ? MaxImages : 1)
                .ToList();

            for (var i = 0; i < images.Count; i++)
            {
                var image = images[i];
                WriteString(writer, i == 0 ? "image_link" : "additional_image_link", image.Url);
            }
        }

        private void ExportPrices(
            XmlWriter writer,
            dynamic product,
            ProfileConfigurationModel config,
            Currency currency)
        {
            Product entity = product.Entity;
            var price = (decimal)product.Price;
            var calculatedPrice = (CalculatedPrice)product._Price;
            var saving = calculatedPrice.Saving;

            if (config.SpecialPrice && saving.HasSaving)
            {
                WriteString(writer, "sale_price", Round(price, currency).ToStringInvariant() + " " + currency.CurrencyCode);
                price = saving.SavingPrice.Amount;

                if (calculatedPrice.ValidUntilUtc.HasValue)
                {
                    var from = calculatedPrice.OfferPrice.HasValue && entity.SpecialPriceStartDateTimeUtc.HasValue
                        ? entity.SpecialPriceStartDateTimeUtc.Value
                        : DateTime.UtcNow.Date;

                    WriteString(writer, "sale_price_effective_date",
                        from.ToString(DateTimeFormat)
                        + "/"
                        + calculatedPrice.ValidUntilUtc.Value.ToString(DateTimeFormat));
                }
            }

            WriteString(writer, "price", Round(price, currency).ToStringInvariant() + " " + currency.CurrencyCode);

            if (config.ExportBasePrice && entity.BasePriceHasValue)
            {
                var measureUnit = BasePriceUnits((string)product.BasePriceMeasureUnit);

                if (IsBasePriceSupported(entity.BasePriceBaseAmount ?? 0, measureUnit))
                {
                    var basePriceMeasure = Round(entity.BasePriceAmount ?? decimal.Zero, currency).ToStringInvariant() + " " + measureUnit;
                    var basePriceBaseMeasure = $"{entity.BasePriceBaseAmount ?? 1} {measureUnit}";

                    WriteString(writer, "unit_pricing_measure", basePriceMeasure);
                    WriteString(writer, "unit_pricing_base_measure", basePriceBaseMeasure);
                }
            }
        }

        private void ExportShipping(
            XmlWriter writer,
            dynamic product,
            Currency currency,
            ProfileConfigurationModel config,
            Dictionary<int, DeliveryTime> deliveryTimes,
            string baseMeasureWeight,
            string baseMeasureDimension,
            string shippingOriginCountryCode)
        {
            Product entity = product.Entity;

            if (!entity.IsShippingEnabled)
            {
                return;
            }

            if (config.ExportShippingTime)
            {
                // INFO: Marked as deprecated by Google, but still working (as of 2024-06).
                WriteString(writer, "transit_time_label", (string)product._ShippingTime);
            }

            if (!config.ExportShipping)
            {
                return;
            }

            if (baseMeasureWeight != null)
            {
                var weight = Round((decimal)product.Weight, currency);
                if (weight > 0)
                {
                    WriteString(writer, "shipping_weight", weight.ToStringInvariant() + " " + baseMeasureWeight);
                }
            }

            if (baseMeasureDimension != null)
            {
                var length = Round((decimal)product.Length, currency);
                var width = Round((decimal)product.Width, currency);
                var height = Round((decimal)product.Height, currency);

                if (IsDimensionSupported(length))
                {
                    WriteString(writer, "shipping_length", length.ToStringInvariant() + " " + baseMeasureDimension);
                }
                if (IsDimensionSupported(width))
                {
                    WriteString(writer, "shipping_width", width.ToStringInvariant() + " " + baseMeasureDimension);
                }
                if (IsDimensionSupported(height))
                {
                    WriteString(writer, "shipping_height", height.ToStringInvariant() + " " + baseMeasureDimension);
                }
            }

            if (shippingOriginCountryCode != null)
            {
                WriteString(writer, "ships_from_country", shippingOriginCountryCode);
            }

            if (deliveryTimes.TryGetValue(entity.DeliveryTimeId.GetValueOrDefault(), out var dt))
            {
                if (dt.MinDays != null)
                {
                    WriteString(writer, "min_handling_time", dt.MinDays.Value.ToStringInvariant());
                }
                WriteString(writer, "max_handling_time", dt.MaxDays.ToStringInvariant());
            }

            bool IsDimensionSupported(decimal value)
            {
                return value >= 1 && ((baseMeasureDimension == "in" && value <= 150) || (baseMeasureDimension == "cm" && value <= 400));
            }
        }

        private static string BasePriceUnits(string value)
        {
            var val = value.NullEmpty() ?? "kg";

            // TODO: Product.BasePriceMeasureUnit should be localized
            return val.ToLowerInvariant() switch
            {
                "kg" or "kilogramm" or "kilogram" => "kg",
                "g" or "gramm" or "gram" => "g",
                "mg" or "milligramm" or "milligram" => "mg",
                "ml" or "milliliter" or "millilitre" => "ml",
                "l" or "liter" or "litre" => "l",
                "cl" or "zentiliter" or "centilitre" => "cl",
                "cbm" or "kubikmeter" or "cubic metre" => "cbm",
                "cm" or "zentimeter" or "centimetre" => "cm",
                "m" or "meter" => "m",
                "qm²" or "quadratmeter" or "square metre" => "sqm",
                _ => "kg",
            };
        }

        private static bool IsBasePriceSupported(int baseAmount, string unit)
        {
            if (baseAmount == 1 || baseAmount == 10 || baseAmount == 100)
                return true;

            if (baseAmount == 75 && unit == "cl")
                return true;

            if ((baseAmount == 50 || baseAmount == 1000) && unit == "kg")
                return true;

            return false;
        }

        private static void WriteString(XmlWriter writer, string fieldName, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                writer.WriteElementString("g", fieldName, GoogleNamespace, value);
            }
        }

        private string GetAttribute(
            Multimap<int, ProductVariantAttributeValue> attributeValues,
            string fieldName,
            int languageId,
            string productEditTabValue,
            string defaultValue)
        {
            // 1. attribute export mapping.
            if (attributeValues != null && _attributeMappings.ContainsKey(fieldName))
            {
                foreach (var attributeId in _attributeMappings[fieldName])
                {
                    if (attributeValues.ContainsKey(attributeId))
                    {
                        var attributeValue = attributeValues[attributeId].FirstOrDefault(x => x.ProductVariantAttribute.ProductAttributeId == attributeId);
                        if (attributeValue != null)
                        {
                            return attributeValue.GetLocalized(x => x.Name, languageId, true, false).Value.EmptyNull();
                        }
                    }
                }
            }

            // 2. explicit set to unspecified.
            if (defaultValue.EqualsNoCase(Unspecified))
            {
                return string.Empty;
            }

            // 3. product edit tab value.
            if (productEditTabValue.HasValue())
            {
                return productEditTabValue;
            }

            return defaultValue.EmptyNull();
        }

        private async Task<string> GetBaseMeasureWeight()
        {
            var baseMeasureWeight = await _db.MeasureWeights.FindByIdAsync(_measureSettings.BaseWeightId, false);
            var measureWeight = baseMeasureWeight != null
                ? baseMeasureWeight.SystemKeyword.EmptyNull().ToLower()
                : string.Empty;

            return measureWeight switch
            {
                "kg" => "kg",
                "g" or "gram" or "gramme" => "g",
                "lb" => "lb",
                "ounce" or "oz" => "oz",
                _ => null
            };
        }

        private async Task<string> GetBaseMeasureDimension()
        {
            var baseMeasureDimension = await _db.MeasureDimensions.FindByIdAsync(_measureSettings.BaseDimensionId);
            var measureDimension = baseMeasureDimension != null
                ? baseMeasureDimension.SystemKeyword.EmptyNull().ToLower()
                : string.Empty;

            return measureDimension switch
            {
                "inch" or "in" or "zoll" or "\"" or "″" => "in",
                "cm" or "centimeter" or "centimetre" => "cm",
                _ => null
            };
        }

        private decimal Round(decimal value, Currency currency)
        {
            // INFO: GMC does not support more than 2 decimal places.
            return _roundingHelper.Round(value, 2, currency.MidpointRounding);
        }
    }

    internal record GmcImage
    {
        public int Id { get; set; }
        public string Url { get; set; }
    }
}