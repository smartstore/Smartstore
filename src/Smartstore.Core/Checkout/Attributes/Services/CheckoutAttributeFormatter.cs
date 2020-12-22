using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Customers;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Web;
using Smartstore.Utilities.Html;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Smartstore.Core.Checkout.Attributes
{
    // TODO: (Core) (ms) needs download & tax service and price formatter
    public partial class CheckoutAttributeFormatter : ICheckoutAttributeFormatter
    {
        private readonly ICheckoutAttributeParser _attributeParser;
        private readonly ICurrencyService _currencyService;
        //private readonly IDownloadService _downloadService;
        //private readonly IPriceFormatter _priceFormatter;
        private readonly IWorkContext _workContext;
        //private readonly ITaxService _taxService;
        private readonly IWebHelper _webHelper;
        private readonly SmartDbContext _db;

        public CheckoutAttributeFormatter(
            ICheckoutAttributeParser attributeParser,
            ICurrencyService currencyService,
            //IDownloadService downloadService,
            //IPriceFormatter priceFormatter,
            IWorkContext workContext,
            //ITaxService taxService,
            IWebHelper webHelper,
            SmartDbContext db)
        {
            _attributeParser = attributeParser;
            _currencyService = currencyService;
            //_downloadService = downloadService;
            //_priceFormatter = priceFormatter;
            _workContext = workContext;
            //_taxService = taxService;
            _webHelper = webHelper;
            _db = db;
        }

        //public async Task<string> FormatAttributesAsync(string attributes)
        //{
        //    Guard.NotNull(attributes, nameof(attributes));

        //    return await FormatAttributesAsync(attributes, _workContext.CurrentCustomer);
        //}


        //public async Task<string> FormatAttributesAsync(
        //    string attributes,
        //    Customer customer,
        //    string serapator = "<br />",
        //    bool htmlEncode = true,
        //    bool renderPrices = true,
        //    bool allowHyperlinks = true)
        //{
        //    Guard.NotNull(attributes, nameof(attributes));
        //    Guard.NotNull(customer, nameof(customer));

        //    var result = new StringBuilder();
        //    var attributesList = (await _attributeParser.ParseCheckoutAttributesAsync(attributes)).ToList();
        //    if (attributesList.IsNullOrEmpty())
        //        return null;

        //    var language = _workContext.WorkingLanguage;
        //    for (var i = 0; i < attributesList.Count; i++)
        //    {
        //        var currentAttribute = attributesList[i];
        //        var currentAttributeValues = (await _attributeParser.ParseValuesAsync(attributes, currentAttribute.Id)).ToList();
        //        for (var j = 0; j < currentAttributeValues.Count; j++)
        //        {
        //            var currentValue = currentAttributeValues[j];
        //            var attribute = string.Empty;
        //            if (!currentAttribute.ShouldHaveValues())
        //            {
        //                if (currentAttribute.AttributeControlType is AttributeControlType.MultilineTextbox)
        //                {
        //                    // Multiline textbox input gets never encoded
        //                    var attributeName = currentAttribute.GetLocalized(a => a.Name, language).ToString();
        //                    if (htmlEncode)
        //                    {
        //                        attributeName = HttpUtility.HtmlEncode(attributeName);
        //                    }

        //                    attribute = string.Format(
        //                        "{0}: {1}",
        //                        attributeName,
        //                        HtmlUtils.ConvertPlainTextToHtml(currentValue.EmptyNull().Replace(":", "").HtmlEncode()));
        //                }
        //                else if (currentAttribute.AttributeControlType is AttributeControlType.FileUpload)
        //                {
        //                    Guid.TryParse(currentValue, out var downloadGuid);
        //                    var download = _downloadService.GetDownloadByGuid(downloadGuid);
        //                    if (download?.MediaFile is not null)
        //                    {
        //                        // TODO: (core) (ms) add a method for getting URL (use routing because it handles all SEO friendly URLs) ?
        //                        var attributeText = string.Empty;
        //                        var fileName = download.MediaFile.Name;
        //                        if (htmlEncode)
        //                        {
        //                            fileName = HttpUtility.HtmlEncode(fileName);
        //                        }

        //                        if (allowHyperlinks)
        //                        {
        //                            var downloadLink = string.Format(
        //                                "{0}download/getfileupload/?downloadId={1}",
        //                                _webHelper.GetStoreLocation(false),
        //                                download.DownloadGuid);

        //                            attributeText = string.Format(
        //                                "<a href=\"{0}\" class=\"fileuploadattribute\">{1}</a>",
        //                                downloadLink,
        //                                fileName);
        //                        }
        //                        else
        //                        {
        //                            attributeText = fileName;
        //                        }

        //                        var attributeName = currentAttribute.GetLocalized(a => a.Name, language).ToString();
        //                        if (htmlEncode)
        //                        {
        //                            attributeName = HttpUtility.HtmlEncode(attributeName);
        //                        }

        //                        attribute = string.Format("{0}: {1}", attributeName, attributeText);
        //                    }
        //                }
        //                else
        //                {
        //                    // Other attributes (textbox, datepicker...)
        //                    attribute = string.Format(
        //                        "{0}: {1}",
        //                        currentAttribute.GetLocalized(a => a.Name, language),
        //                        currentValue);

        //                    if (htmlEncode)
        //                    {
        //                        attribute = HttpUtility.HtmlEncode(attribute);
        //                    }
        //                }

        //            }
        //            else
        //            {
        //                if (int.TryParse(currentValue, out var id))
        //                {
        //                    var attributeValue = await _db.CheckoutAttributeValues.FindByIdAsync(id);
        //                    if (attributeValue is not null)
        //                    {
        //                        attribute = string.Format(
        //                            "{0}: {1}",
        //                            currentAttribute.GetLocalized(a => a.Name, language),
        //                            attributeValue.GetLocalized(a => a.Name, language));

        //                        if (renderPrices)
        //                        {
        //                            var priceAdjustmentBase = _taxService.GetCheckoutAttributePrice(attributeValue, customer);
        //                            var priceAdjustment = _currencyService.ConvertFromPrimaryStoreCurrency(priceAdjustmentBase, _workContext.WorkingCurrency);
        //                            if (priceAdjustmentBase > 0)
        //                            {
        //                                attribute += string.Format(" [+{0}]", _priceFormatter.FormatPrice(priceAdjustment));
        //                            }
        //                        }
        //                    }

        //                    if (htmlEncode)
        //                    {
        //                        attribute = HttpUtility.HtmlEncode(attribute);
        //                    }
        //                }
        //            }

        //            if (attribute.HasValue())
        //            {
        //                if (i != 0 || j != 0)
        //                {
        //                    result.Append(serapator);
        //                }

        //                result.Append(attribute);
        //            }
        //        }
        //    }

        //    return result.ToString();
        //}
    }
}
