using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Customers;
using Smartstore.Core.Data;
using Smartstore.Core.Domain.Catalog;
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
        private readonly IPriceFormatter _priceFormatter;
        private readonly ITaxService _taxService;
        private readonly ICurrencyService _currencyService;
        private readonly ILocalizationService _localizationService;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly CatalogSettings _catalogSettings;

        public ProductAttributeFormatter(
            SmartDbContext db,
            IWorkContext workContext,
            IWebHelper webHelper,
            IProductAttributeMaterializer productAttributeMaterializer,
            IPriceFormatter priceFormatter,
            ITaxService taxService,
            ICurrencyService currencyService,
            ILocalizationService localizationService,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings)
        {
            _db = db;
            _workContext = workContext;
            _webHelper = webHelper;
            _productAttributeMaterializer = productAttributeMaterializer;
            _priceFormatter = priceFormatter;
            _taxService = taxService;
            _currencyService = currencyService;
            _localizationService = localizationService;
            _shoppingCartSettings = shoppingCartSettings;
            _catalogSettings = catalogSettings;
        }

        public virtual async Task<string> FormatAttributesAsync(
            ProductVariantAttributeSelection selection,
            Product product,
            Customer customer = null,
            string separator = "<br />",
            bool htmlEncode = true,
            bool includePrices = true,
            bool includeProductAttributes = true,
            bool includeGiftCardAttributes = true,
            bool includeHyperlinks = true)
        {
            Guard.NotNull(selection, nameof(selection));
            Guard.NotNull(product, nameof(product));

            using var pool = StringBuilderPool.Instance.Get(out var result);

            if (includeProductAttributes)
            {
                var languageId = _workContext.WorkingLanguage.Id;
                var attributes = await _productAttributeMaterializer.MaterializeProductVariantAttributesAsync(selection);
                var attributesDic = attributes.ToDictionary(x => x.Id);

                foreach (var kvp in selection.AttributesMap)
                {
                    if (!attributesDic.TryGetValue(kvp.Key, out var pva))
                    {
                        continue;
                    }

                    foreach (var value in kvp.Value)
                    {
                        var valueStr = value.ToString().EmptyNull();
                        var pvaAttribute = string.Empty;

                        if (pva.IsListTypeAttribute())
                        {
                            var pvaValue = pva.ProductVariantAttributeValues.FirstOrDefault(x => x.Id == valueStr.ToInt());
                            if (pvaValue != null)
                            {
                                pvaAttribute = "{0}: {1}".FormatInvariant(
                                    pva.ProductAttribute.GetLocalized(x => x.Name, languageId),
                                    pvaValue.GetLocalized(x => x.Name, languageId));

                                if (includePrices)
                                {
                                    // TODO: (mg) (core) Complete ProductAttributeFormatter.FormatAttributesAsync (IPriceCalculationService required).
                                    //var attributeValuePriceAdjustment = _priceCalculationService.GetProductVariantAttributeValuePriceAdjustment(pvaValue, product, customer ?? _workContext.CurrentCustomer, null, 1);
                                    var attributeValuePriceAdjustment = decimal.Zero;
                                    var priceAdjustmentBase = await _taxService.GetProductPriceAsync(product, attributeValuePriceAdjustment, customer: customer ?? _workContext.CurrentCustomer);
                                    var priceAdjustment = _currencyService.ConvertFromPrimaryStoreCurrency(priceAdjustmentBase, _workContext.WorkingCurrency);

                                    if (_shoppingCartSettings.ShowLinkedAttributeValueQuantity &&
                                        pvaValue.ValueType == ProductVariantAttributeValueType.ProductLinkage &&
                                        pvaValue.Quantity > 1)
                                    {
                                        pvaAttribute = pvaAttribute + " × " + pvaValue.Quantity;
                                    }

                                    if (_catalogSettings.ShowVariantCombinationPriceAdjustment)
                                    {
                                        if (priceAdjustmentBase > 0)
                                        {
                                            pvaAttribute += " (+{0})".FormatInvariant(_priceFormatter.FormatPrice(priceAdjustment, true, displayTax: false));
                                        }
                                        else if (priceAdjustmentBase < decimal.Zero)
                                        {
                                            pvaAttribute += " (-{0})".FormatInvariant(_priceFormatter.FormatPrice(-priceAdjustment, true, displayTax: false));
                                        }
                                    }
                                }

                                if (htmlEncode)
                                {
                                    pvaAttribute = HttpUtility.HtmlEncode(pvaAttribute);
                                }
                            }
                        }
                        else if (pva.AttributeControlType == AttributeControlType.MultilineTextbox)
                        {
                            string attributeName = pva.ProductAttribute.GetLocalized(x => x.Name, languageId);

                            pvaAttribute = "{0}: {1}".FormatInvariant(
                                htmlEncode ? HttpUtility.HtmlEncode(attributeName) : attributeName,
                                HtmlUtils.ConvertPlainTextToHtml(valueStr.HtmlEncode()));
                        }
                        else if (pva.AttributeControlType == AttributeControlType.FileUpload)
                        {
                            if (Guid.TryParse(valueStr, out var downloadGuid) && downloadGuid != Guid.Empty)
                            {
                                var download = await _db.Downloads
                                    .AsNoTracking()
                                    .Include(x => x.MediaFile)
                                    .Where(x => x.DownloadGuid == downloadGuid)
                                    .FirstOrDefaultAsync();

                                if (download?.MediaFile != null)
                                {
                                    var attributeText = string.Empty;
                                    var fileName = htmlEncode
                                        ? HttpUtility.HtmlEncode(download.MediaFile.Name)
                                        : download.MediaFile.Name;

                                    if (includeHyperlinks)
                                    {
                                        // TODO: (core) add a method for getting URL (use routing because it handles all SEO friendly URLs).
                                        var downloadLink = _webHelper.GetStoreLocation(false) + "download/getfileupload/?downloadId=" + download.DownloadGuid;
                                        attributeText = $"<a href=\"{downloadLink}\" class=\"fileuploadattribute\">{fileName}</a>";
                                    }
                                    else
                                    {
                                        attributeText = fileName;
                                    }

                                    string attributeName = pva.ProductAttribute.GetLocalized(a => a.Name, languageId);

                                    pvaAttribute = "{0}: {1}".FormatInvariant(
                                        htmlEncode ? HttpUtility.HtmlEncode(attributeName) : attributeName,
                                        attributeText);
                                }
                            }
                        }
                        else
                        {
                            // TextBox, Datepicker
                            pvaAttribute = "{0}: {1}".FormatInvariant(pva.ProductAttribute.GetLocalized(x => x.Name, languageId), valueStr);

                            if (htmlEncode)
                            {
                                pvaAttribute = HttpUtility.HtmlEncode(pvaAttribute);
                            }
                        }

                        result.Grow(pvaAttribute, separator);
                    }
                }
            }

            if (includeGiftCardAttributes && product.IsGiftCard)
            {
                var gca = selection.GetGiftCardAttributes();
                if (gca != null)
                {
                    // Sender.
                    var giftCardFrom = product.GiftCardType == GiftCardType.Virtual
                        ? (await _localizationService.GetResourceAsync("GiftCardAttribute.From.Virtual")).FormatInvariant(gca.SenderName, gca.SenderEmail)
                        : (await _localizationService.GetResourceAsync("GiftCardAttribute.From.Physical")).FormatInvariant(gca.SenderName);

                    // Recipient.
                    var giftCardFor = product.GiftCardType == GiftCardType.Virtual
                        ? (await _localizationService.GetResourceAsync("GiftCardAttribute.For.Virtual")).FormatInvariant(gca.RecipientName, gca.RecipientEmail)
                        : (await _localizationService.GetResourceAsync("GiftCardAttribute.For.Physical")).FormatInvariant(gca.RecipientName);

                    if (htmlEncode)
                    {
                        giftCardFrom = HttpUtility.HtmlEncode(giftCardFrom);
                        giftCardFor = HttpUtility.HtmlEncode(giftCardFor);
                    }

                    result.Grow(giftCardFrom, separator);
                    result.Grow(giftCardFor, separator);
                }
            }

            return result.ToString();
        }
    }
}
