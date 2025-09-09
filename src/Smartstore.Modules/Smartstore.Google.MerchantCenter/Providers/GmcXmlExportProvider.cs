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
        const string CargoDataKey = "GmcCargoData";
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
            _attributeMappings = await _productAttributeService.GetExportFieldMappingsAsync("gmc");

            var now = DateTime.UtcNow;
            var storeName = (string)context.Store.Name;
            Multimap<int, ProductVariantAttributeValue> attributeValues = null;

            var cargo = await GetCargoData(context, cancelToken);
            var config = cargo.Config;
            context.CustomProperties[CargoDataKey] = cargo;

            using var writer = XmlWriter.Create(context.DataStream, ExportXmlHelper.DefaultSettings);
            writer.WriteStartDocument();
            writer.WriteStartElement("rss");
            writer.WriteAttributeString("version", "2.0");
            writer.WriteAttributeString("xmlns", "g", null, GoogleNamespace);
            writer.WriteStartElement("channel");
            writer.WriteElementString("title", $"{storeName} - Google Merchant Center feed");
            writer.WriteElementString("link", "http://base.google.com/base/");
            writer.WriteElementString("description", $"Product data from \"{storeName}\" in Google Merchant Center format");

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

                        attributeValues = !isParent && product._AttributeCombinationValues != null
                            ? ((IList<ProductVariantAttributeValue>)product._AttributeCombinationValues).ToMultimap(x => x.ProductVariantAttribute.ProductAttributeId, x => x)
                            : [];

                        var category = googleProduct?.Taxonomy?.NullEmpty() ?? config.DefaultGoogleCategory;
                        if (category.IsEmpty())
                        {
                            context.Log.Error(T("Plugins.Feed.Froogle.MissingDefaultCategory"));
                        }

                        Write(writer, "id", (string)product._UniqueId);
                        writer.WriteCData("title", ((string)product.Name).Truncate(70));
                        writer.WriteCData("description", (string)product.FullDescription);
                        writer.WriteCData("google_product_category", category, "g", GoogleNamespace);
                        writer.WriteCData("product_type", productType.NullEmpty(), "g", GoogleNamespace);
                        writer.WriteElementString("link", (string)product._DetailUrl);

                        switch (entity.Condition)
                        {
                            case ProductCondition.Damaged:
                            case ProductCondition.Used:
                                Write(writer, "condition", "used");
                                break;
                            case ProductCondition.Refurbished:
                                Write(writer, "condition", "refurbished");
                                break;
                            case ProductCondition.New:
                            default:
                                Write(writer, "condition", "new");
                                break;
                        }

                        ExportAvailability(product, writer, cargo);
                        ExportImages(product, googleProduct, writer, cargo);
                        ExportPrices(product, writer, cargo);

                        Write(writer, "gtin", gtin);
                        Write(writer, "brand", brand);
                        Write(writer, "mpn", mpn);
                        Write(writer, "identifier_exists", identifierExists ? "yes" : "no");
                        Write(writer, "item_group_id", googleProduct?.ItemGroupId?.NullEmpty());

                        ExportAttribute("gender", googleProduct?.Gender, config.Gender);
                        ExportAttribute("age_group", googleProduct?.AgeGroup, config.AgeGroup);
                        ExportAttribute("color", googleProduct?.Color, config.Color);
                        ExportAttribute("size", googleProduct?.Size, config.Size);
                        ExportAttribute("material", googleProduct?.Material, config.Material);
                        ExportAttribute("pattern", googleProduct?.Pattern, config.Pattern);

                        if (config.ExpirationDays > 0)
                        {
                            Write(writer, "expiration_date", now.AddDays(config.ExpirationDays).ToString(DateFormat));
                        }

                        ExportShipping(product, writer, cargo);

                        if (googleProduct != null)
                        {
                            Write(writer, "multipack", googleProduct.Multipack > 1 ? googleProduct.Multipack.ToStringInvariant() : null);
                            Write(writer, "is_bundle", googleProduct.IsBundle.HasValue ? (googleProduct.IsBundle.Value ? "yes" : "no") : null);
                            Write(writer, "adult", googleProduct.IsAdult.HasValue ? (googleProduct.IsAdult.Value ? "yes" : "no") : null);

                            if (googleProduct.EnergyEfficiencyClass.HasValue())
                            {
                                writer.WriteStartElement("g", "certification", GoogleNamespace);
                                Write(writer, "certification_authority", "EC");
                                Write(writer, "certification_name", "EPREL");
                                // EPREL code: A, B, ... G. Rescaled version, no "+" signs anymore (like A+++).
                                Write(writer, "certification_code", googleProduct.EnergyEfficiencyClass.Trim());
                                writer.WriteEndElement();
                            }
                        }

                        ExportAttribute("custom_label_0", googleProduct?.CustomLabel0, null);
                        ExportAttribute("custom_label_1", googleProduct?.CustomLabel1, null);
                        ExportAttribute("custom_label_2", googleProduct?.CustomLabel2, null);
                        ExportAttribute("custom_label_3", googleProduct?.CustomLabel3, null);
                        ExportAttribute("custom_label_4", googleProduct?.CustomLabel4, null);

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

            void ExportAttribute(string fieldName, string googleProductValue, string defaultValue)
            {
                string value = null;

                // 1. attribute export mapping.
                if (attributeValues != null && _attributeMappings.TryGetValues(fieldName, out var attributeIds))
                {
                    foreach (var attributeId in attributeIds)
                    {
                        if (attributeValues.TryGetValues(attributeId, out var values))
                        {
                            var attributeValue = values.FirstOrDefault(x => x.ProductVariantAttribute.ProductAttributeId == attributeId);
                            if (attributeValue != null)
                            {
                                value = attributeValue.GetLocalized(x => x.Name, cargo.LanguageId, true, false).Value.EmptyNull();
                            }
                        }
                    }
                }

                // 2. explicit set to unspecified.
                if (value == null && defaultValue.EqualsNoCase(Unspecified))
                {
                    value = string.Empty;
                }

                // 3. product edit tab value.
                value ??= googleProductValue ?? defaultValue;

                if (!string.IsNullOrEmpty(value))
                {
                    writer.WriteElementString("g", fieldName, GoogleNamespace, value);
                }
            }
        }

        private static void ExportAvailability(dynamic product, XmlWriter writer, GmcCargoData cargo)
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

            availability ??= cargo.DefaultAvailability;

            Write(writer, "availability", availability);
            if (availability == "preorder" && entity.AvailableStartDateTimeUtc.HasValue && entity.AvailableStartDateTimeUtc.Value > DateTime.UtcNow)
            {
                Write(writer, "availability_date", entity.AvailableStartDateTimeUtc.Value.ToString(DateTimeFormat));
            }
        }

        private static void ExportImages(dynamic product, GoogleProduct googleProduct, XmlWriter writer, GmcCargoData cargo)
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
                .Take(cargo.Config.AdditionalImages ? MaxImages : 1)
                .ToList();

            for (var i = 0; i < images.Count; i++)
            {
                var image = images[i];
                Write(writer, i == 0 ? "image_link" : "additional_image_link", image.Url);
            }
        }

        private void ExportPrices(dynamic product, XmlWriter writer, GmcCargoData cargo)
        {
            var currency = cargo.Currency;
            Product entity = product.Entity;
            var price = (decimal)product.Price;
            var calculatedPrice = (CalculatedPrice)product._Price;
            var saving = calculatedPrice.Saving;

            if (cargo.Config.SpecialPrice && saving.HasSaving)
            {
                Write(writer, "sale_price", Round(price, currency).ToStringInvariant() + " " + currency.CurrencyCode);
                price = saving.SavingPrice.Amount;

                if (calculatedPrice.ValidUntilUtc.HasValue)
                {
                    var from = calculatedPrice.OfferPrice.HasValue && entity.SpecialPriceStartDateTimeUtc.HasValue
                        ? entity.SpecialPriceStartDateTimeUtc.Value
                        : DateTime.UtcNow.Date;

                    Write(writer, "sale_price_effective_date",
                        from.ToString(DateTimeFormat)
                        + "/"
                        + calculatedPrice.ValidUntilUtc.Value.ToString(DateTimeFormat));
                }
            }

            Write(writer, "price", Round(price, currency).ToStringInvariant() + " " + currency.CurrencyCode);

            if (cargo.Config.ExportBasePrice && entity.BasePriceHasValue)
            {
                // TODO: Product.BasePriceMeasureUnit should be localized
                var baseUnit = ((string)product.BasePriceMeasureUnit).NullEmpty() ?? "kg";
                var unit = baseUnit.ToLowerInvariant() switch
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

                var baseAmount = entity.BasePriceBaseAmount ?? 0;
                var isBasePriceSupported = baseAmount == 1 || baseAmount == 10 || baseAmount == 100
                    || (baseAmount == 75 && unit == "cl")
                    || ((baseAmount == 50 || baseAmount == 1000) && unit == "kg");

                if (isBasePriceSupported)
                {
                    Write(writer, "unit_pricing_measure", Round(entity.BasePriceAmount ?? 0, currency).ToStringInvariant() + " " + unit);
                    Write(writer, "unit_pricing_base_measure", $"{entity.BasePriceBaseAmount ?? 1} {unit}");
                }
            }
        }

        private void ExportShipping(dynamic product, XmlWriter writer, GmcCargoData cargo)
        {
            var currency = cargo.Currency;
            Product entity = product.Entity;

            if (!entity.IsShippingEnabled)
            {
                return;
            }

            if (cargo.Config.ExportShippingTime)
            {
                // INFO: Marked as deprecated by Google, but still working (as of 2024-06).
                Write(writer, "transit_time_label", (string)product._ShippingTime);
            }

            if (!cargo.Config.ExportShipping)
            {
                return;
            }

            if (cargo.BaseMeasureWeight != null)
            {
                var weight = Round((decimal)product.Weight, currency);
                if (weight > 0)
                {
                    Write(writer, "shipping_weight", weight.ToStringInvariant() + " " + cargo.BaseMeasureWeight);
                }
            }

            if (cargo.BaseMeasureDimension != null)
            {
                var length = Round((decimal)product.Length, currency);
                var width = Round((decimal)product.Width, currency);
                var height = Round((decimal)product.Height, currency);

                if (IsDimensionSupported(length))
                {
                    Write(writer, "shipping_length", length.ToStringInvariant() + " " + cargo.BaseMeasureDimension);
                }
                if (IsDimensionSupported(width))
                {
                    Write(writer, "shipping_width", width.ToStringInvariant() + " " + cargo.BaseMeasureDimension);
                }
                if (IsDimensionSupported(height))
                {
                    Write(writer, "shipping_height", height.ToStringInvariant() + " " + cargo.BaseMeasureDimension);
                }
            }

            if (cargo.ShippingOriginCountryCode != null)
            {
                Write(writer, "ships_from_country", cargo.ShippingOriginCountryCode);
            }

            if (cargo.DeliveryTimes.TryGetValue(entity.DeliveryTimeId.GetValueOrDefault(), out var dt))
            {
                if (dt.MinDays != null)
                {
                    Write(writer, "min_handling_time", dt.MinDays.Value.ToStringInvariant());
                }
                Write(writer, "max_handling_time", dt.MaxDays.ToStringInvariant());
            }

            bool IsDimensionSupported(decimal value)
            {
                return value >= 1 && ((cargo.BaseMeasureDimension == "in" && value <= 150) || (cargo.BaseMeasureDimension == "cm" && value <= 400));
            }
        }

        private async Task<GmcCargoData> GetCargoData(ExportExecuteContext context, CancellationToken cancelToken)
        {
            if (context.CustomProperties.TryGetValue(CargoDataKey, out object value))
            {
                return (GmcCargoData)value;
            }

            var config = (context.ConfigurationData as ProfileConfigurationModel) ?? new();
            var shippingOriginAddress = await _db.Addresses
                .Include(x => x.Country)
                .FindByIdAsync(_shippingSettings.ShippingOriginAddressId, false, cancelToken);

            string defaultAvailability = null;
            if (config.Availability.EqualsNoCase(Unspecified))
                defaultAvailability = string.Empty;
            else if (config.Availability.HasValue())
                defaultAvailability = config.Availability;
            else
                defaultAvailability = "in stock";

            var baseWeight = await _db.MeasureWeights.FindByIdAsync(_measureSettings.BaseWeightId, false, cancelToken);
            var weightKeyword = baseWeight?.SystemKeyword.EmptyNull().ToLower() ?? string.Empty;
            var baseMeasureWeight = weightKeyword switch
            {
                "kg" or "kilogramm" or "kilogram" => "kg",
                "g" or "gramm" or "gram" => "g",
                "lb" or "pound" => "lb",
                "ounce" or "oz" => "oz",
                _ => null
            };

            var baseDimension = await _db.MeasureDimensions.FindByIdAsync(_measureSettings.BaseDimensionId, false, cancelToken);
            var measureDimension = baseDimension?.SystemKeyword.EmptyNull().ToLower() ?? string.Empty;
            var baseMeasureDimension = measureDimension switch
            {
                "inch" or "in" or "zoll" or "\"" or "″" => "in",
                "cm" or "centimeter" or "centimetre" => "cm",
                _ => null
            };

            var cargoData = new GmcCargoData
            {
                Config = config,
                LanguageId = context.Projection.LanguageId ?? 0,
                Currency = (Currency)context.Currency.Entity,
                BaseMeasureWeight = baseMeasureWeight,
                BaseMeasureDimension = baseMeasureDimension,
                ShippingOriginCountryCode = shippingOriginAddress?.Country?.TwoLetterIsoCode.NullEmpty(),
                DeliveryTimes = await _db.DeliveryTimes
                    .AsNoTracking()
                    .Where(x => x.MaxDays != null && x.MaxDays <= 10)
                    .ToDictionaryAsync(x => x.Id, cancelToken),
                DefaultAvailability = defaultAvailability
            };

            context.CustomProperties[CargoDataKey] = cargoData;
            return cargoData;
        }

        private static void Write(XmlWriter writer, string fieldName, string value)
        {
            if (value != null)
            {
                writer.WriteElementString("g", fieldName, GoogleNamespace, value);
            }
        }

        private decimal Round(decimal value, Currency currency)
        {
            // INFO: GMC does not support more than 2 decimal places.
            return _roundingHelper.Round(value, 2, currency.MidpointRounding);
        }

        class GmcCargoData
        {
            public ProfileConfigurationModel Config { get; init; }
            public int LanguageId { get; init; }
            public Currency Currency { get; init; }
            public string BaseMeasureWeight { get; init; }
            public string BaseMeasureDimension { get; init; }
            public string ShippingOriginCountryCode { get; init; }
            public Dictionary<int, DeliveryTime> DeliveryTimes { get; init; }
            public string DefaultAvailability { get; init; }
        }

        record GmcImage
        {
            public int Id { get; init; }
            public string Url { get; init; }
        }
    }
}