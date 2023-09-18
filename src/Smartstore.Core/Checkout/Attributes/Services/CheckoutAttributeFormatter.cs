using System.Globalization;
using System.Web;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Web;
using Smartstore.Utilities;
using Smartstore.Utilities.Html;

namespace Smartstore.Core.Checkout.Attributes
{
    public partial class CheckoutAttributeFormatter : ICheckoutAttributeFormatter
    {
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        private readonly ICurrencyService _currencyService;
        private readonly ITaxService _taxService;
        private readonly IWorkContext _workContext;
        private readonly ITaxCalculator _taxCalculator;
        private readonly IWebHelper _webHelper;
        private readonly SmartDbContext _db;

        public CheckoutAttributeFormatter(
            ICheckoutAttributeMaterializer attributeMaterializer,
            ICurrencyService currencyService,
            IWorkContext workContext,
            ITaxService taxService,
            ITaxCalculator taxCalculator,
            IWebHelper webHelper,
            SmartDbContext db)
        {
            _checkoutAttributeMaterializer = attributeMaterializer;
            _currencyService = currencyService;
            _workContext = workContext;
            _taxService = taxService;
            _taxCalculator = taxCalculator;
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
            Guard.NotNull(selection);

            customer ??= _workContext.CurrentCustomer;

            using var psb = StringBuilderPool.Instance.Get(out var sb);
            var attributesList = await _checkoutAttributeMaterializer.MaterializeCheckoutAttributesAsync(selection);
            if (attributesList.IsNullOrEmpty())
                return null;

            var attributeValues = attributesList
                .Where(x => x.IsListTypeAttribute)
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
                    if (!currentAttribute.IsListTypeAttribute)
                    {
                        if (currentAttribute.AttributeControlType is AttributeControlType.MultilineTextbox)
                        {
                            // Multiline textbox input gets never encoded
                            var attributeName = currentAttribute.GetLocalized(a => a.Name, language).ToString();
                            if (htmlEncode)
                            {
                                attributeName = HttpUtility.HtmlEncode(attributeName);
                            }

                            attributeStr = $"{attributeName}: {HtmlUtility.ConvertPlainTextToHtml(currentValue.EmptyNull().Replace(":", string.Empty).HtmlEncode())}";
                        }
                        else if (currentAttribute.AttributeControlType is AttributeControlType.FileUpload)
                        {
                            Guid.TryParse(currentValue, out var downloadGuid);

                            var download = await _db.Downloads
                                .Include(x => x.MediaFile)
                                .Where(x => x.DownloadGuid == downloadGuid)
                                .FirstOrDefaultAsync();

                            if (download?.MediaFile != null)
                            {
                                // TODO: (mh) (core) add a method for getting URL (use routing because it handles all SEO friendly URLs) ?
                                //var genratedUrl = _mediaService.GenerateFileDownloadUrl(download.MediaFileId, 0);
                                var attributeText = string.Empty;
                                var fileName = download.MediaFile.Name;
                                if (htmlEncode)
                                {
                                    fileName = HttpUtility.HtmlEncode(fileName);
                                }

                                if (allowHyperlinks)
                                {
                                    var downloadLink = $"{_webHelper.GetStoreLocation()}download/getfileupload/?downloadId={download.DownloadGuid}";
                                    attributeText = $"<a href='{downloadLink}' class='fileuploadattribute'>{fileName}</a>";
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

                                attributeStr = $"{attributeName}: {attributeText}";
                            }
                        }
                        else
                        {
                            // TextBox, Datepicker
                            if (currentAttribute.AttributeControlType == AttributeControlType.Datepicker)
                            {
                                var culture = CommonHelper.TryAction(() => new CultureInfo(language.LanguageCulture));
                                currentValue = currentValue.ToDateTime(null)?.ToString("D", culture) ?? currentValue;
                            }

                            attributeStr = $"{currentAttribute.GetLocalized(a => a.Name, language)}: {currentValue}";

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
                                attributeStr = $"{currentAttribute.GetLocalized(x => x.Name, language)}: {attributeValue.GetLocalized(x => x.Name, language)}";

                                if (renderPrices)
                                {
                                    var adjustment = await _taxCalculator.CalculateCheckoutAttributeTaxAsync(attributeValue, customer: customer);
                                    if (adjustment.Price > 0m)
                                    {
                                        var convertedAdjustment = _currencyService.ConvertToWorkingCurrency(adjustment.Price);
                                        attributeStr += $" [+{_taxService.ApplyTaxFormat(convertedAdjustment).ToString()}]";
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
                            sb.Append(serapator);
                        }

                        sb.Append(attributeStr);
                    }
                }
            }

            return sb.ToString();
        }
    }
}
