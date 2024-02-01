using System.Threading;
using System.Xml;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
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
        ExportFeatures.UsesAttributeCombination)]
    public class GmcXmlExportProvider : ExportProviderBase
    {
        public const string SystemName = "Feeds.GoogleMerchantCenterProductXml";
        public const string Unspecified = "__nospec__";

        private const string _googleNamespace = "http://base.google.com/ns/1.0";

        private readonly SmartDbContext _db;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IRoundingHelper _roundingHelper;
        private readonly MeasureSettings _measureSettings;

        private Multimap<string, int> _attributeMappings;

        public GmcXmlExportProvider(
            SmartDbContext db,
            IProductAttributeService productAttributeService,
            IRoundingHelper roundingHelper,
            MeasureSettings measureSettings)
        {
            _db = db;
            _productAttributeService = productAttributeService;
            _roundingHelper = roundingHelper;
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
            ConfigurationWidget = new ComponentWidget(typeof(GmcConfigurationViewComponent)),
            ModelType = typeof(ProfileConfigurationModel)
        };

        public override string FileExtension => "XML";

        protected override async Task ExportAsync(ExportExecuteContext context, CancellationToken cancelToken)
        {
            Currency currency = context.Currency.Entity;
            var now = DateTime.UtcNow;
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
                        var isParent = (bool)product._IsParent;
                        string brand = product._Brand;
                        string gtin = product.Gtin;
                        string mpn = product.ManufacturerPartNumber;
                        var availability = defaultAvailability;
                        List<dynamic> productFiles = product.ProductMediaFiles;
                        List<string> imageUrls = new List<string>();

                        // Get all files which are images.
                        foreach (var file in productFiles)
                        {
                            ProductMediaFile fileEntity = file.Entity;
                            string imageUrl = file.File._FullSizeImageUrl;

                            if (fileEntity.MediaFile.MediaType == "image" && imageUrl.HasValue())
                            {
                                imageUrls.Add(imageUrl);
                            }
                        }   

                        var attributeValues = !isParent && product._AttributeCombinationValues != null
                            ? ((IList<ProductVariantAttributeValue>)product._AttributeCombinationValues).ToMultimap(x => x.ProductVariantAttribute.ProductAttributeId, x => x)
                            : new Multimap<int, ProductVariantAttributeValue>();

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
                            else if (entity.BackorderMode == BackorderMode.AllowQtyBelow0 || entity.BackorderMode == BackorderMode.AllowQtyBelow0OnBackorder)
                            {
                                availability = entity.AvailableForPreOrder ? "preorder" : "out of stock";
                            }
                        }
                        else if (entity.ManageInventoryMethod == ManageInventoryMethod.DontManageStock && entity.DisableBuyButton)
                        {
                            availability = "out of stock";
                        }

                        WriteString(writer, "id", (string)product._UniqueId);
                        writer.WriteCData("title", ((string)product.Name).Truncate(70));
                        writer.WriteCData("description", (string)product.FullDescription);
                        writer.WriteCData("google_product_category", category, "g", _googleNamespace);
                        writer.WriteCData("product_type", productType.NullEmpty(), "g", _googleNamespace);
                        writer.WriteElementString("link", (string)product._DetailUrl);

                        if (imageUrls.Any())
                        {
                            WriteString(writer, "image_link", imageUrls.First());

                            if (config.AdditionalImages)
                            {
                                var imageCount = 0;
                                foreach (var url in imageUrls.Skip(1))
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

                        if (availability == "preorder" && entity.AvailableStartDateTimeUtc.HasValue && entity.AvailableStartDateTimeUtc.Value > now)
                        {
                            WriteString(writer, "availability_date", entity.AvailableStartDateTimeUtc.Value.ToString(dateFormat));
                        }

                        // Price.
                        var price = (decimal)product.Price;
                        var calculatedPrice = (CalculatedPrice)product._Price;
                        var saving = calculatedPrice.Saving;

                        if (config.SpecialPrice && saving.HasSaving)
                        {
                            WriteString(writer, "sale_price", Round(price).ToStringInvariant() + " " + currency.CurrencyCode);
                            price = saving.SavingPrice.Amount;

                            if (calculatedPrice.ValidUntilUtc.HasValue)
                            {
                                var from = calculatedPrice.OfferPrice.HasValue && entity.SpecialPriceStartDateTimeUtc.HasValue
                                    ? entity.SpecialPriceStartDateTimeUtc.Value
                                    : now.Date;

                                WriteString(writer, "sale_price_effective_date",
                                    from.ToString(dateFormat)
                                    + "/"
                                    + calculatedPrice.ValidUntilUtc.Value.ToString(dateFormat));
                            }
                        }

                        WriteString(writer, "price", Round(price).ToStringInvariant() + " " + currency.CurrencyCode);
                        WriteString(writer, "gtin", gtin);
                        WriteString(writer, "brand", brand);
                        WriteString(writer, "mpn", mpn);

                        var identifierExists = brand.HasValue() && (gtin.HasValue() || mpn.HasValue());
                        WriteString(writer, "identifier_exists", identifierExists ? "yes" : "no");

                        WriteString(writer, "gender", GetAttribute(attributeValues, "gender", languageId, gmc?.Gender, config.Gender));
                        WriteString(writer, "age_group", GetAttribute(attributeValues, "age_group", languageId, gmc?.AgeGroup, config.AgeGroup));
                        WriteString(writer, "color", GetAttribute(attributeValues, "color", languageId, gmc?.Color, config.Color));
                        WriteString(writer, "size", GetAttribute(attributeValues, "size", languageId, gmc?.Size, config.Size));
                        WriteString(writer, "material", GetAttribute(attributeValues, "material", languageId, gmc?.Material, config.Material));
                        WriteString(writer, "pattern", GetAttribute(attributeValues, "pattern", languageId, gmc?.Pattern, config.Pattern));

                        WriteString(writer, "item_group_id", gmc?.ItemGroupId?.NullEmpty());

                        if (config.ExpirationDays > 0)
                        {
                            WriteString(writer, "expiration_date", now.AddDays(config.ExpirationDays).ToString("yyyy-MM-dd"));
                        }

                        if (config.ExportShipping)
                        {
                            var weight = (decimal)product.Weight;
                            if (weight > 0)
                            {
                                WriteString(writer, "shipping_weight", decimal.Round(weight, 2).ToStringInvariant() + " " + measureWeight);
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
                                var basePriceMeasure = Round(entity.BasePriceAmount ?? decimal.Zero).ToStringInvariant() + " " + measureUnit;
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
                            WriteString(writer, "energy_efficiency_class", gmc.EnergyEfficiencyClass.NullEmpty());
                        }

                        WriteString(writer, "custom_label_0", GetAttribute(attributeValues, "custom_label_0", languageId, gmc?.CustomLabel0, null).NullEmpty());
                        WriteString(writer, "custom_label_1", GetAttribute(attributeValues, "custom_label_1", languageId, gmc?.CustomLabel1, null).NullEmpty());
                        WriteString(writer, "custom_label_2", GetAttribute(attributeValues, "custom_label_2", languageId, gmc?.CustomLabel2, null).NullEmpty());
                        WriteString(writer, "custom_label_3", GetAttribute(attributeValues, "custom_label_3", languageId, gmc?.CustomLabel3, null).NullEmpty());
                        WriteString(writer, "custom_label_4", GetAttribute(attributeValues, "custom_label_4", languageId, gmc?.CustomLabel4, null).NullEmpty());

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

            decimal Round(decimal amount)
            {
                // INFO: GMC does not support more than 2 decimal places.
                return _roundingHelper.Round(amount, 2, currency.MidpointRounding);
            }
        }
    }
}