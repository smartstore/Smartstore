using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Customers;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Web;
using Smartstore.Utilities.Html;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Smartstore.Core.Checkout.Attributes
{
    public partial class CheckoutAttributeFormatter : ICheckoutAttributeFormatter
    {
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ICurrencyService _currencyService;
        private readonly IDownloadService _downloadService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IMediaService _mediaService;
        private readonly IWorkContext _workContext;
        private readonly ITaxService _taxService;
        private readonly IWebHelper _webHelper;
        private readonly SmartDbContext _db;

        public CheckoutAttributeFormatter(
            ICheckoutAttributeMaterializer attributeMaterializer,
            ICurrencyService currencyService,
            IDownloadService downloadService,
            IPriceFormatter priceFormatter,
            IMediaService mediaService,
            IWorkContext workContext,
            ITaxService taxService,
            IWebHelper webHelper,
            SmartDbContext db)
        {
            _checkoutAttributeMaterializer = attributeMaterializer;
            _currencyService = currencyService;
            _downloadService = downloadService;
            _priceFormatter = priceFormatter;
            _mediaService = mediaService;
            _workContext = workContext;
            _taxService = taxService;
            _webHelper = webHelper;
            _db = db;
        }

        public async Task<string> FormatAttributesAsync(
            CheckoutAttributeSelection selection,
            Customer customer = null,
            string serapator = "<br />",
            bool htmlEncode = true,
            bool renderPrices = true,
            bool allowHyperlinks = true)
        {
            Guard.NotNull(selection, nameof(selection));

            customer ??= _workContext.CurrentCustomer;

            var result = new StringBuilder();            
            var attributesList = await _checkoutAttributeMaterializer.MaterializeCheckoutAttributesAsync(selection);
            if (attributesList.IsNullOrEmpty())
                return null;

            var attributeValues = attributesList
                .Where(x => x.ShouldHaveValues())
                .SelectMany(x => x.CheckoutAttributeValues);

            var language = _workContext.WorkingLanguage;
            for (var i = 0; i < attributesList.Count; i++)
            {
                var currentAttribute = attributesList[i];
                var currentAttributeValues = selection.GetAttributeValues(currentAttribute.Id).ToList();

                for (var j = 0; j < currentAttributeValues.Count; j++)
                {
                    var currentValue = currentAttributeValues[j].ToString();
                    var attributeStr = string.Empty;
                    if (!currentAttribute.ShouldHaveValues())
                    {
                        if (currentAttribute.AttributeControlType is AttributeControlType.MultilineTextbox)
                        {
                            // Multiline textbox input gets never encoded
                            var attributeName = currentAttribute.GetLocalized(a => a.Name, language).ToString();
                            if (htmlEncode)
                            {
                                attributeName = HttpUtility.HtmlEncode(attributeName);
                            }

                            attributeStr = string.Format(
                                "{0}: {1}",
                                attributeName,
                                HtmlUtils.ConvertPlainTextToHtml(currentValue.EmptyNull().Replace(":", "").HtmlEncode()));
                        }
                        else if (currentAttribute.AttributeControlType is AttributeControlType.FileUpload)
                        {
                            Guid.TryParse(currentValue, out var downloadGuid);
                            var download = await _db.Downloads.Where(x=>x.DownloadGuid == downloadGuid).FirstOrDefaultAsync();
                            if (download?.MediaFile != null)
                            {
                                var genratedUrl = _mediaService.GetUrlAsync(download.MediaFileId, 0);
                                // TODO: (ms) (core) add a method for getting URL (use routing because it handles all SEO friendly URLs) ?
                                var attributeText = string.Empty;
                                var fileName = download.MediaFile.Name;
                                if (htmlEncode)
                                {
                                    fileName = HttpUtility.HtmlEncode(fileName);
                                }

                                if (allowHyperlinks)
                                {
                                    var downloadLink = string.Format(
                                        "{0}download/getfileupload/?downloadId={1}",
                                        _webHelper.GetStoreLocation(false),
                                        download.DownloadGuid);

                                    attributeText = string.Format(
                                        "<a href=\"{0}\" class=\"fileuploadattribute\">{1}</a>",
                                        downloadLink,
                                        fileName);
                                }
                                else
                                {
                                    attributeText = fileName;
                                }

                                var attributeName = currentAttribute.GetLocalized(a => a.Name, language).ToString();
                                if (htmlEncode)
                                {
                                    attributeName = HttpUtility.HtmlEncode(attributeName);
                                }

                                attributeStr = string.Format("{0}: {1}", attributeName, attributeText);
                            }
                        }
                        else
                        {
                            // Other attributes (textbox, datepicker...)
                            attributeStr = string.Format(
                                "{0}: {1}",
                                currentAttribute.GetLocalized(a => a.Name, language),
                                currentValue);

                            if (htmlEncode)
                            {
                                attributeStr = HttpUtility.HtmlEncode(attributeStr);
                            }
                        }

                    }
                    else
                    {
                        if (int.TryParse(currentValue, out var id))
                        {
                            var attributeValue = attributeValues.Where(x => x.Id == id).FirstOrDefault();
                            if (attributeValue != null)
                            {
                                attributeStr = string.Format(
                                    "{0}: {1}",
                                    currentAttribute.GetLocalized(x => x.Name, language),
                                    attributeValue.GetLocalized(x => x.Name, language));

                                if (renderPrices)
                                {
                                    var priceAdjustment = await _taxService.GetCheckoutAttributePriceAsync(attributeValue, customer);
                                    var priceAdjustmentConverted = _currencyService.ConvertFromPrimaryStoreCurrency(priceAdjustment, _workContext.WorkingCurrency);
                                    if (priceAdjustment > 0)
                                    {
                                        attributeStr += string.Format(" [+{0}]", _priceFormatter.FormatPrice(priceAdjustmentConverted));
                                    }
                                }
                            }

                            if (htmlEncode)
                            {
                                attributeStr = HttpUtility.HtmlEncode(attributeStr);
                            }
                        }
                    }

                    if (attributeStr.HasValue())
                    {
                        if (i != 0 || j != 0)
                        {
                            result.Append(serapator);
                        }

                        result.Append(attributeStr);
                    }
                }
            }

            return result.ToString();
        }
    }
}
