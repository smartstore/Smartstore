using System.Threading;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.DataExchange;
using Smartstore.Core.DataExchange.Export;
using Smartstore.Core.Platform.DataExchange.Export;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Google.MerchantCenter.Components;
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
        ExportFeatures.UsesSpecialPrice |
        ExportFeatures.UsesAttributeCombination)]
    public class GmcXmlExportProvider : ExportProviderBase
    {
        public const string SystemName = "Feeds.GoogleMerchantCenterProductXml";
        public const string Unspecified = "__nospec__";

        private const string _googleNamespace = "http://base.google.com/ns/1.0";

        private readonly SmartDbContext _db;
        private readonly IProductAttributeService _productAttributeService;
        private readonly MeasureSettings _measureSettings;

        private Multimap<string, int> _attributeMappings;

        public GmcXmlExportProvider(
            SmartDbContext db,
            IProductAttributeService productAttributeService,
            MeasureSettings measureSettings)
        {
            _db = db;
            _productAttributeService = productAttributeService;
            _measureSettings = measureSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        private static string BasePriceUnits(string value)
        {
            const string defaultValue = "kg";

            if (value.IsEmpty())
                return defaultValue;

            // TODO: Product.BasePriceMeasureUnit should be localized
            return value.ToLowerInvariant() switch
            {
                "mg" or "milligramm" or "milligram" => "mg",
                "g" or "gramm" or "gram" => "g",
                "kg" or "kilogramm" or "kilogram" => "kg",
                "ml" or "milliliter" or "millilitre" => "ml",
                "cl" or "zentiliter" or "centilitre" => "cl",
                "l" or "liter" or "litre" => "l",
                "cbm" or "kubikmeter" or "cubic metre" => "cbm",
                "cm" or "zentimeter" or "centimetre" => "cm",
                "m" or "meter" => "m",
                "qm²" or "quadratmeter" or "square metre" => "sqm",
                _ => defaultValue,
            };
        }

        private static bool BasePriceSupported(int baseAmount, string unit)
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
            if (value != null)
            {
                writer.WriteElementString("g", fieldName, _googleNamespace, value);
            }
        }

        private string GetAttributeValue(
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

        private async Task<string> GetBaseMeasureWeightAsync()
        {
            var measureWeightEntity = await _db.MeasureWeights.FindByIdAsync(_measureSettings.BaseWeightId, false);
            var measureWeight = measureWeightEntity != null
                ? measureWeightEntity.SystemKeyword.EmptyNull().ToLower()
                : string.Empty;

            return measureWeight switch
            {
                "gram" or "gramme" => "g",
                "mg" or "milligramme" or "milligram" => "mg",
                "lb" => "lb",
                "ounce" or "oz" => "oz",
                _ => "kg",
            };
        }

        public override ExportConfigurationInfo ConfigurationInfo => new()
        {
            ConfigurationWidget = new ComponentWidgetInvoker(typeof(GmcConfigurationViewComponent)),
            ModelType = typeof(ProfileConfigurationModel)
        };

        public override string FileExtension => "XML";

        protected override async Task ExportAsync(ExportExecuteContext context, CancellationToken cancelToken)
        {
            Currency currency = context.Currency.Entity;
            var languageId = context.Projection.LanguageId ?? 0;
            var dateFormat = "yyyy-MM-ddTHH:mmZ";
            var defaultAvailability = "in stock";
            var measureWeight = await GetBaseMeasureWeightAsync();
            _attributeMappings = await _productAttributeService.GetExportFieldMappingsAsync("gmc");

            var config = (context.ConfigurationData as ProfileConfigurationModel) ?? new ProfileConfigurationModel();

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
            writer.WriteAttributeString("xmlns", "g", null, _googleNamespace);
            writer.WriteStartElement("channel");
            writer.WriteElementString("title", $"{(string)context.Store.Name} - Feed for Google Merchant Center");
            writer.WriteElementString("link", "http://base.google.com/base/");
            writer.WriteElementString("description", "Information about products");

            while (context.Abort == DataExchangeAbortion.None && await context.DataSegmenter.ReadNextSegmentAsync())
            {
                var segment = await context.DataSegmenter.GetCurrentSegmentAsync();

                int[] productIds = segment.Select(x => (int)((dynamic)x).Id).ToArray();

                var googleProducts = (await _db.GoogleProducts()
                    .Where(x => productIds.Contains(x.ProductId))
                    .ToListAsync(cancelToken))
                    .ToDictionarySafe(x => x.ProductId);

                foreach (dynamic product in segment)
                {
                    if (context.Abort != DataExchangeAbortion.None)
                        break;

                    Product entity = product.Entity;
                    var gmc = googleProducts.Get(entity.Id);

                    if (gmc != null && !gmc.Export)
                        continue;

                    writer.WriteStartElement("item");

                    try
                    {
                        string productType = product._CategoryPath;
                        var price = (decimal)product.Price;
                        var uniqueId = (string)product._UniqueId;
                        var isParent = (bool)product._IsParent;
                        string brand = product._Brand;
                        string gtin = product.Gtin;
                        string mpn = product.ManufacturerPartNumber;
                        var availability = defaultAvailability;
                        List<dynamic> productPictures = product.ProductPictures;
                        var pictureUrls = productPictures
                            .Select(x => (string)x.Picture._FullSizeImageUrl)
                            .Where(x => x.HasValue())
                            .ToList();

                        var attributeValues = !isParent && product._AttributeCombinationValues != null
                            ? ((IList<ProductVariantAttributeValue>)product._AttributeCombinationValues).ToMultimap(x => x.ProductVariantAttribute.ProductAttributeId, x => x)
                            : new Multimap<int, ProductVariantAttributeValue>();

                        var specialPrice = product._FutureSpecialPrice as decimal?;
                        if (!specialPrice.HasValue)
                        {
                            specialPrice = product._SpecialPrice;
                        }

                        var category = gmc?.Taxonomy?.NullEmpty() ?? config.DefaultGoogleCategory;
                        if (category.IsEmpty())
                        {
                            context.Log.Error(T("Plugins.Feed.Froogle.MissingDefaultCategory"));
                        }

                        if (entity.ManageInventoryMethod == ManageInventoryMethod.ManageStock && entity.StockQuantity <= 0)
                        {
                            if (entity.BackorderMode == BackorderMode.NoBackorders)
                            {
                                availability = "out of stock";
                            }
                            else if (entity.BackorderMode == BackorderMode.AllowQtyBelow0 || entity.BackorderMode == BackorderMode.AllowQtyBelow0AndNotifyCustomer)
                            {
                                availability = entity.AvailableForPreOrder ? "preorder" : "out of stock";
                            }
                        }

                        WriteString(writer, "id", uniqueId);

                        writer.WriteStartElement("title");
                        writer.WriteCData(((string)product.Name).Truncate(70).RemoveInvalidXmlChars());
                        writer.WriteEndElement();

                        writer.WriteStartElement("description");
                        writer.WriteCData(((string)product.FullDescription).RemoveInvalidXmlChars());
                        writer.WriteEndElement();

                        writer.WriteStartElement("g", "google_product_category", _googleNamespace);
                        writer.WriteCData(category.RemoveInvalidXmlChars());
                        writer.WriteFullEndElement();

                        if (productType.HasValue())
                        {
                            writer.WriteStartElement("g", "product_type", _googleNamespace);
                            writer.WriteCData(productType.RemoveInvalidXmlChars());
                            writer.WriteFullEndElement();
                        }

                        writer.WriteElementString("link", (string)product._DetailUrl);

                        if (pictureUrls.Any())
                        {
                            WriteString(writer, "image_link", pictureUrls.First());

                            if (config.AdditionalImages)
                            {
                                var imageCount = 0;
                                foreach (var url in pictureUrls.Skip(1))
                                {
                                    if (++imageCount <= 10)
                                    {
                                        WriteString(writer, "additional_image_link", url);
                                    }
                                }
                            }
                        }

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

                        WriteString(writer, "availability", availability);

                        if (availability == "preorder" && entity.AvailableStartDateTimeUtc.HasValue && entity.AvailableStartDateTimeUtc.Value > DateTime.UtcNow)
                        {
                            var availabilityDate = entity.AvailableStartDateTimeUtc.Value.ToString(dateFormat);
                            WriteString(writer, "availability_date", availabilityDate);
                        }

                        if (config.SpecialPrice && specialPrice.HasValue)
                        {
                            WriteString(writer, "sale_price", string.Concat(specialPrice.Value.FormatInvariant(), " ", currency.CurrencyCode));

                            if (entity.SpecialPriceStartDateTimeUtc.HasValue && entity.SpecialPriceEndDateTimeUtc.HasValue)
                            {
                                var specialPriceDate = "{0}/{1}".FormatInvariant(
                                    entity.SpecialPriceStartDateTimeUtc.Value.ToString(dateFormat),
                                    entity.SpecialPriceEndDateTimeUtc.Value.ToString(dateFormat));

                                WriteString(writer, "sale_price_effective_date", specialPriceDate);
                            }

                            price = (product._RegularPrice as decimal?) ?? price;
                        }

                        WriteString(writer, "price", string.Concat(price.FormatInvariant(), " ", currency.CurrencyCode));

                        WriteString(writer, "gtin", gtin);
                        WriteString(writer, "brand", brand);
                        WriteString(writer, "mpn", mpn);

                        var identifierExists = brand.HasValue() && (gtin.HasValue() || mpn.HasValue());
                        WriteString(writer, "identifier_exists", identifierExists ? "yes" : "no");

                        var gender = GetAttributeValue(attributeValues, "gender", languageId, gmc?.Gender, config.Gender);
                        WriteString(writer, "gender", gender);

                        var ageGroup = GetAttributeValue(attributeValues, "age_group", languageId, gmc?.AgeGroup, config.AgeGroup);
                        WriteString(writer, "age_group", ageGroup);

                        var color = GetAttributeValue(attributeValues, "color", languageId, gmc?.Color, config.Color);
                        WriteString(writer, "color", color);

                        var size = GetAttributeValue(attributeValues, "size", languageId, gmc?.Size, config.Size);
                        WriteString(writer, "size", size);

                        var material = GetAttributeValue(attributeValues, "material", languageId, gmc?.Material, config.Material);
                        WriteString(writer, "material", material);

                        var pattern = GetAttributeValue(attributeValues, "pattern", languageId, gmc?.Pattern, config.Pattern);
                        WriteString(writer, "pattern", pattern);

                        var itemGroupId = gmc != null && gmc.ItemGroupId.HasValue() ? gmc.ItemGroupId : string.Empty;
                        if (itemGroupId.HasValue())
                        {
                            WriteString(writer, "item_group_id", itemGroupId);
                        }

                        if (config.ExpirationDays > 0)
                        {
                            WriteString(writer, "expiration_date", DateTime.UtcNow.AddDays(config.ExpirationDays).ToString("yyyy-MM-dd"));
                        }

                        if (config.ExportShipping)
                        {
                            var weight = (decimal)product.Weight;
                            if (weight > 0)
                            {
                                WriteString(writer, "shipping_weight", weight.FormatInvariant() + " " + measureWeight);
                            }
                        }

                        if (config.ExportShippingTime)
                        {
                            WriteString(writer, "transit_time_label", (string)product._ShippingTime);
                        }

                        if (config.ExportBasePrice && entity.BasePriceHasValue)
                        {
                            var measureUnit = BasePriceUnits((string)product.BasePriceMeasureUnit);

                            if (BasePriceSupported(entity.BasePriceBaseAmount ?? 0, measureUnit))
                            {
                                var basePriceMeasure = $"{(entity.BasePriceAmount ?? decimal.Zero).FormatInvariant()} {measureUnit}";
                                var basePriceBaseMeasure = $"{entity.BasePriceBaseAmount ?? 1} {measureUnit}";

                                WriteString(writer, "unit_pricing_measure", basePriceMeasure);
                                WriteString(writer, "unit_pricing_base_measure", basePriceBaseMeasure);
                            }
                        }

                        if (gmc != null)
                        {
                            WriteString(writer, "multipack", gmc.Multipack > 1 ? gmc.Multipack.ToString() : null);
                            WriteString(writer, "is_bundle", gmc.IsBundle.HasValue ? (gmc.IsBundle.Value ? "yes" : "no") : null);
                            WriteString(writer, "adult", gmc.IsAdult.HasValue ? (gmc.IsAdult.Value ? "yes" : "no") : null);
                            WriteString(writer, "energy_efficiency_class", gmc.EnergyEfficiencyClass.HasValue() ? gmc.EnergyEfficiencyClass : null);
                        }

                        var customLabel0 = GetAttributeValue(attributeValues, "custom_label_0", languageId, gmc?.CustomLabel0, null);
                        WriteString(writer, "custom_label_0", gmc?.CustomLabel0.NullEmpty());

                        var customLabel1 = GetAttributeValue(attributeValues, "custom_label_1", languageId, gmc?.CustomLabel1, null);
                        WriteString(writer, "custom_label_1", gmc?.CustomLabel1.NullEmpty());

                        var customLabel2 = GetAttributeValue(attributeValues, "custom_label_2", languageId, gmc?.CustomLabel2, null);
                        WriteString(writer, "custom_label_2", gmc?.CustomLabel2.NullEmpty());

                        var customLabel3 = GetAttributeValue(attributeValues, "custom_label_3", languageId, gmc?.CustomLabel3, null);
                        WriteString(writer, "custom_label_3", gmc?.CustomLabel3.NullEmpty());

                        var customLabel4 = GetAttributeValue(attributeValues, "custom_label_4", languageId, gmc?.CustomLabel4, null);
                        WriteString(writer, "custom_label_4", gmc?.CustomLabel4.NullEmpty());

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
    }
}