using System.IO;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Configuration;
using Smartstore.Core.Widgets;
using Smartstore.IO;
using Smartstore.Pdf;
using Smartstore.Web.Controllers;
using Smartstore.Web.Models.Orders;

namespace Smartstore.Web.Api
{
    // TODO: (mg) (core) (DRY) Implement IPdfGenerator in Smartstore.Web that is responsible for
    // generating PDF documents for orders. Or at least put that stuff in OrderHelper.
    // But after 5.0.1 has been released.
    public class ApiPdfHelper
    {
        private readonly IWorkContext _workContext;
        private readonly IUrlHelper _urlHelper;
        private readonly OrderHelper _orderHelper;
        private readonly ISettingFactory _settingFactory;
        private readonly IPdfConverter _pdfConverter;
        private readonly IViewInvoker _viewInvoker;

        public ApiPdfHelper(
            IWorkContext workContext,
            IUrlHelper urlHelper,
            OrderHelper orderHelper,
            ISettingFactory settingFactory,
            IPdfConverter pdfConverter,
            IViewInvoker viewInvoker)
        {
            _workContext = workContext;
            _urlHelper = urlHelper;
            _orderHelper = orderHelper;
            _settingFactory = settingFactory;
            _pdfConverter = pdfConverter;
            _viewInvoker = viewInvoker;
        }

        public async Task<Stream> GeneratePdfAsync(Order order)
        {
            Guard.NotNull(order, nameof(order));

            var routeValues = new RouteValueDictionary
            {
                ["storeId"] = order.StoreId,
                ["lid"] = _workContext.WorkingLanguage.Id
            };

            var pdfSettings = _settingFactory.LoadSettings<PdfSettings>(order.StoreId);
            var model = new List<OrderDetailsModel> { await _orderHelper.PrepareOrderDetailsModelAsync(order) };
            
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = model,
                ["PdfMode"] = true
            };

            var orderHtml = await _viewInvoker.InvokeViewAsync("~/Views/Order/Details.Print.cshtml", null, viewData);

            var conversionSettings = new PdfConversionSettings
            {
                Size = pdfSettings.LetterPageSizeEnabled ? PdfPageSize.Letter : PdfPageSize.A4,
                Margins = new PdfPageMargins { Top = 35, Bottom = 35 },
                Header = _pdfConverter.CreateFileInput(_urlHelper.Action("ReceiptHeader", "Pdf", routeValues)),
                Footer = _pdfConverter.CreateFileInput(_urlHelper.Action("ReceiptFooter", "Pdf", routeValues)),
                Page = _pdfConverter.CreateHtmlInput(orderHtml.ToString())
            };

            return await _pdfConverter.GeneratePdfAsync(conversionSettings);
        }

        public string GetFileName(Order order)
        {
            return PathUtility.SanitizeFileName(_orderHelper.T("Order.PdfInvoiceFileName", order.Id));
        }
    }
}
