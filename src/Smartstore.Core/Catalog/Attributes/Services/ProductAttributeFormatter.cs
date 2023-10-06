using System.Globalization;
using System.Runtime.CompilerServices;
using Org.BouncyCastle.Security;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Web;
using Smartstore.Utilities;
using Smartstore.Utilities.Html;

namespace Smartstore.Core.Catalog.Attributes
{
    public partial class ProductAttributeFormatter : IProductAttributeFormatter
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IWebHelper _webHelper;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ILocalizationService _localizationService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly PriceSettings _priceSettings;

        public ProductAttributeFormatter(
            SmartDbContext db,
            IWorkContext workContext,
            IWebHelper webHelper,
            IProductAttributeMaterializer productAttributeMaterializer,
            ILocalizationService localizationService,
            IPriceCalculationService priceCalculationService,
            ShoppingCartSettings shoppingCartSettings,
            PriceSettings priceSettings)
        {
            _db = db;
            _workContext = workContext;
            _webHelper = webHelper;
            _productAttributeMaterializer = productAttributeMaterializer;
            _localizationService = localizationService;
            _priceCalculationService = priceCalculationService;
            _shoppingCartSettings = shoppingCartSettings;
            _priceSettings = priceSettings;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<string> FormatAttributesAsync(
            ProductVariantAttributeSelection selection,
            Product product,
            Customer customer = null,
            string separator = "<br />",
            bool htmlEncode = true,
            bool includePrices = true,
            bool includeProductAttributes = true,
            bool includeGiftCardAttributes = true,
            bool includeHyperlinks = true,
            ProductBatchContext batchContext = null)
        {
            var options = new ProductAttributeFormatOptions
            {
                ItemSeparator = separator,
                HtmlEncode = htmlEncode,
                IncludePrices = includePrices,
                IncludeProductAttributes = includeProductAttributes,
                IncludeGiftCardAttributes=includeGiftCardAttributes,
                IncludeHyperlinks = includeHyperlinks
            };

            return FormatAttributesAsync(selection, product, options, customer, batchContext);
        }

        public virtual async Task<string> FormatAttributesAsync(
            ProductVariantAttributeSelection selection,
            Product product,
            ProductAttributeFormatOptions options,
            Customer customer = null,
            ProductBatchContext batchContext = null)
        {
            Guard.NotNull(selection);
            Guard.NotNull(product);
            Guard.NotNull(options);

            customer ??= _workContext.CurrentCustomer;

            using var psb = StringBuilderPool.Instance.Get(out var sb);

            if (options.IncludeProductAttributes)
            {
                var language = _workContext.WorkingLanguage;
                var attributes = await _productAttributeMaterializer.MaterializeProductVariantAttributesAsync(selection);

                // Key: ProductVariantAttributeValue.Id, value: calculated attribute price adjustment.
                var priceAdjustments = options.IncludePrices && _priceSettings.ShowVariantCombinationPriceAdjustment
                    ? await _priceCalculationService.CalculateAttributePriceAdjustmentsAsync(product, selection, 1, _priceCalculationService.CreateDefaultOptions(false, customer, null, batchContext))
                    : new Dictionary<int, CalculatedPriceAdjustment>();

                foreach (var pva in attributes)
                {
                    if (TryGenerateVariantTokens(pva, selection, priceAdjustments, options, out var name, out var value, out var price))
                    {
                        if (price.HasValue())
                        {
                            value += options.PriceFormatTemplate.FormatInvariant(price);
                        }

                        var formattedItem = options.FormatTemplate.FormatInvariant(name, value);

                        // Append formatted item to StringBuilder
                        sb.Grow(formattedItem, options.ItemSeparator);
                    }
                }
            }

            if (options.IncludeGiftCardAttributes && product.IsGiftCard)
            {
                var gci = selection.GetGiftCardInfo();
                if (gci != null)
                {
                    var cardType = product.GiftCardType.ToString();

                    // Sender
                    var senderName = TryEncode(_localizationService.GetResource($"GiftCardAttribute.From.{cardType}"));
                    var senderValue = TryEncode(product.GiftCardType == GiftCardType.Physical ? gci.SenderName : $"{gci.SenderName} <{gci.SenderEmail}>");
                    sb.Grow(options.FormatTemplate.FormatInvariant(senderName, senderValue), options.ItemSeparator);

                    // Recipient
                    var recipientName = TryEncode(_localizationService.GetResource($"GiftCardAttribute.For.{cardType}"));
                    var recipientValue = TryEncode(product.GiftCardType == GiftCardType.Physical ? gci.RecipientName : $"{gci.RecipientName} <{gci.RecipientEmail}>");
                    sb.Grow(options.FormatTemplate.FormatInvariant(recipientName, recipientValue), options.ItemSeparator);

                    //// Sender.
                    //var giftCardFrom = product.GiftCardType == GiftCardType.Virtual
                    //    ? _localizationService.GetResource("GiftCardAttribute.From.Virtual").FormatInvariant(gci.SenderName, gci.SenderEmail)
                    //    : _localizationService.GetResource("GiftCardAttribute.From.Physical").FormatInvariant(gci.SenderName);

                    //// Recipient.
                    //var giftCardFor = product.GiftCardType == GiftCardType.Virtual
                    //    ? _localizationService.GetResource("GiftCardAttribute.For.Virtual").FormatInvariant(gci.RecipientName, gci.RecipientEmail)
                    //    : _localizationService.GetResource("GiftCardAttribute.For.Physical").FormatInvariant(gci.RecipientName);

                    //if (options.HtmlEncode)
                    //{
                    //    giftCardFrom = giftCardFrom.HtmlEncode();
                    //    giftCardFor = giftCardFor.HtmlEncode();
                    //}

                    //sb.Grow(giftCardFrom, options.ItemSeparator);
                    //sb.Grow(giftCardFor, options.ItemSeparator);
                }
            }

            return sb.ToString();

            string TryEncode(string input)
            {
                return options.HtmlEncode ? input.HtmlEncode() : input;
            }
        }

        private bool TryGenerateVariantTokens(
            ProductVariantAttribute pva,
            ProductVariantAttributeSelection selection,
            IDictionary<int, CalculatedPriceAdjustment> priceAdjustments,
            ProductAttributeFormatOptions options,
            out string name, 
            out string value, 
            out string price)
        {
            name = null; 
            value = null; 
            price = null;

            var language = _workContext.WorkingLanguage;

            var pair = selection.AttributesMap.FirstOrDefault(x => x.Key == pva.Id);
            if (pair.Key == 0)
            {
                return false;
            }

            foreach (var attrValue in pair.Value)
            {
                var valueStr = attrValue.ToString().EmptyNull();

                if (pva.IsListTypeAttribute())
                {
                    var pvaValue = pva.ProductVariantAttributeValues.FirstOrDefault(x => x.Id == valueStr.ToInt());
                    if (pvaValue != null)
                    {
                        //pvaAttribute = $"{pva.ProductAttribute.GetLocalized(x => x.Name, language.Id)}: {pvaValue.GetLocalized(x => x.Name, language.Id)}";
                        name = TryEncode(pva.ProductAttribute.GetLocalized(x => x.Name, language.Id));
                        value = TryEncode(pvaValue.GetLocalized(x => x.Name, language.Id));

                        if (options.IncludePrices)
                        {
                            if (_shoppingCartSettings.ShowLinkedAttributeValueQuantity &&
                                pvaValue.ValueType == ProductVariantAttributeValueType.ProductLinkage &&
                                pvaValue.Quantity > 1)
                            {
                                //pvaAttribute = pvaAttribute + " × " + pvaValue.Quantity;
                                value = value + " × " + pvaValue.Quantity;
                            }

                            if (priceAdjustments.TryGetValue(pvaValue.Id, out var adjustment))
                            {
                                if (adjustment.Price > 0)
                                {
                                    //pvaAttribute += $" (+{adjustment.Price})";
                                    price = $" +{adjustment.Price}";
                                }
                                else if (adjustment.Price < 0)
                                {
                                    //pvaAttribute += $" (-{adjustment.Price * -1})";
                                    price = $" -{adjustment.Price * -1}";
                                }
                            }
                        }

                        if (options.HtmlEncode)
                        {
                            //pvaAttribute = pvaAttribute.HtmlEncode();
                        }
                    }
                }
                else if (pva.AttributeControlType == AttributeControlType.MultilineTextbox)
                {
                    //var attributeName = pva.ProductAttribute.GetLocalized(x => x.Name, language.Id);
                    //pvaAttribute = $"{(options.HtmlEncode ? attributeName.HtmlEncode() : attributeName)}: {HtmlUtility.ConvertPlainTextToHtml(valueStr.HtmlEncode())}";
                    name = TryEncode(pva.ProductAttribute.GetLocalized(x => x.Name, language.Id));
                    value = HtmlUtility.ConvertPlainTextToHtml(valueStr.HtmlEncode());
                }
                else if (pva.AttributeControlType == AttributeControlType.FileUpload)
                {
                    if (Guid.TryParse(valueStr, out var downloadGuid) && downloadGuid != Guid.Empty)
                    {
                        var download = _db.Downloads
                            .AsNoTracking()
                            .Include(x => x.MediaFile)
                            .Where(x => x.DownloadGuid == downloadGuid)
                            .FirstOrDefault();

                        if (download?.MediaFile != null)
                        {
                            var attributeText = string.Empty;
                            var fileName = options.HtmlEncode
                                ? download.MediaFile.Name.HtmlEncode()
                                : download.MediaFile.Name;

                            if (options.IncludeHyperlinks)
                            {
                                // TODO: (core) add a method for getting URL (use routing because it handles all SEO friendly URLs).
                                var downloadLink = _webHelper.GetStoreLocation() + "download/getfileupload/?downloadId=" + download.DownloadGuid;
                                attributeText = $"<a href='{downloadLink}' class='fileuploadattribute'>{fileName}</a>";
                            }
                            else
                            {
                                attributeText = fileName;
                            }

                            //string attributeName = pva.ProductAttribute.GetLocalized(x => x.Name, language.Id);
                            //pvaAttribute = $"{(options.HtmlEncode ? attributeName.HtmlEncode() : attributeName)}: {attributeText}";
                            name = TryEncode(pva.ProductAttribute.GetLocalized(x => x.Name, language.Id));
                            value = attributeText;
                        }
                    }
                }
                else
                {
                    // TextBox, Datepicker
                    if (pva.AttributeControlType == AttributeControlType.Datepicker)
                    {
                        var culture = CommonHelper.TryAction(() => new CultureInfo(language.LanguageCulture));
                        valueStr = valueStr.ToDateTime(null)?.ToString("D", culture) ?? valueStr;
                    }

                    //pvaAttribute = $"{pva.ProductAttribute.GetLocalized(x => x.Name, language.Id)}: {valueStr}";
                    name = TryEncode(pva.ProductAttribute.GetLocalized(x => x.Name, language.Id));
                    value = TryEncode(valueStr);

                    //if (options.HtmlEncode)
                    //{
                    //    pvaAttribute = pvaAttribute.HtmlEncode();
                    //}
                }

                //result.Grow(pvaAttribute, options.ItemSeparator);
            }

            return name != null && value != null;

            string TryEncode(string input)
            {
                return options.HtmlEncode ? input.HtmlEncode() : input;
            }
        }
    }
}
