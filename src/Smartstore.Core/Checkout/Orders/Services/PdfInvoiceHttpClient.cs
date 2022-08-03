using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Localization;
using Smartstore.Http;

namespace Smartstore.Core.Checkout.Orders
{
    public class PdfInvoiceResult
    {
        public byte[] Buffer { get; init; }
        public string MimeType { get; init; }
        public string FileName { get; init; }
    }

    public class PdfInvoiceHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly IUrlHelper _urlHelper;

        public PdfInvoiceHttpClient(HttpClient httpClient, IUrlHelper urlHelper, Localizer localizer)
        {
            _httpClient = httpClient;
            _urlHelper = urlHelper;
            T = localizer;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task<PdfInvoiceResult> GetPdfInvoiceAsync(int orderId, CancellationToken cancelToken = default)
        {
            var request = _urlHelper.ActionContext.HttpContext.Request;
            var path = _urlHelper.Action("Print", "Order", new { id = orderId, pdf = true, area = string.Empty });
            var url = WebHelper.GetAbsoluteUrl(path, request);

            using var response = await _httpClient.GetAsync(url, cancelToken);
            using var stream = await response.Content.ReadAsStreamAsync(cancelToken);

            PdfInvoiceResult result = null;

            if (response.IsSuccessStatusCode)
            {
                string fileName = null;

                var contentDisposition = response.Content.Headers.ContentDisposition;
                if (contentDisposition != null)
                {
                    fileName = contentDisposition.FileName;
                }

                if (fileName.IsEmpty())
                {
                    fileName = WebHelper.GetFileNameFromUrl(url);
                }

                result = new PdfInvoiceResult
                {
                    MimeType = response.Content.Headers.ContentType?.MediaType,
                    Buffer = await stream.ToByteArrayAsync(),
                    FileName = fileName
                };
            }

            if (result == null)
            {
                throw new InvalidOperationException(T("Admin.System.QueuedEmails.ErrorEmptyAttachmentResult", path));
            }
            else if (!result.MimeType.EqualsNoCase("application/pdf"))
            {
                throw new InvalidOperationException(T("Admin.System.QueuedEmails.ErrorNoPdfAttachment"));
            }

            return result;
        }
    }
}
