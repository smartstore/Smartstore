using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Http;

namespace Smartstore.Core.Checkout.Orders
{
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

        public async Task<QueuedEmailAttachment> GetPdfInvoiceAsync(int orderId, CancellationToken cancelToken = default)
        {
            var request = _urlHelper.ActionContext.HttpContext.Request;
            var path = _urlHelper.Action("Print", "Order", new { id = orderId, pdf = true, area = string.Empty });
            var url = WebHelper.GetAbsoluteUrl(path, request);

            using var response = await _httpClient.GetAsync(url, cancelToken);
            using var stream = await response.Content.ReadAsStreamAsync(cancelToken);

            QueuedEmailAttachment attachment = null;

            if (response.IsSuccessStatusCode)
            {
                attachment = new QueuedEmailAttachment
                {
                    StorageLocation = EmailAttachmentStorageLocation.Blob,
                    MimeType = response.Content.Headers.ContentType?.MediaType,
                    MediaStorage = new MediaStorage
                    {
                        // INFO: stream.Length not supported here.
                        Data = await stream.ToByteArrayAsync()
                    }
                };

                var contentDisposition = response.Content.Headers.ContentDisposition;
                if (contentDisposition != null)
                {
                    attachment.Name = contentDisposition.FileName;
                }

                if (attachment.Name.IsEmpty())
                {
                    attachment.Name = WebHelper.GetFileNameFromUrl(url);
                }
            }

            if (attachment == null)
            {
                throw new InvalidOperationException(T("Admin.System.QueuedEmails.ErrorEmptyAttachmentResult", path));
            }
            else if (!attachment.MimeType.EqualsNoCase("application/pdf"))
            {
                throw new InvalidOperationException(T("Admin.System.QueuedEmails.ErrorNoPdfAttachment"));
            }

            return attachment;
        }
    }
}
