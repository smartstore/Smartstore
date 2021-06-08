using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging.Events;
using Smartstore.Events;
using Smartstore.Http;

namespace Smartstore.Core.Messaging
{
    internal class CreateAttachmentsConsumer : IConsumer
    {
        private readonly IUrlHelper _urlHelper;
        private readonly PdfSettings _pdfSettings;

        public CreateAttachmentsConsumer(IUrlHelper urlHelper, PdfSettings pdfSettings)
        {
            _urlHelper = urlHelper;
            _pdfSettings = pdfSettings;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;
        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task HandleEventAsync(MessageQueuingEvent message)
        {
            var qe = message.QueuedEmail;
            var ctx = message.MessageContext;
            var model = message.MessageModel;

            var handledTemplates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                { "OrderPlaced.CustomerNotification", _pdfSettings.AttachOrderPdfToOrderPlacedEmail },
                { "OrderCompleted.CustomerNotification", _pdfSettings.AttachOrderPdfToOrderCompletedEmail }
            };

            if (handledTemplates.TryGetValue(ctx.MessageTemplate.Name, out var shouldHandle) && shouldHandle)
            {
                if (model.Get("Order") is IDictionary<string, object> order && order.Get("ID") is int orderId)
                {
                    try
                    {
                        var qea = await CreatePdfInvoiceAttachmentAsync(orderId);
                        qe.Attachments.Add(qea);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, T("Admin.System.QueuedEmails.ErrorCreatingAttachment"));
                    }
                }
            }
        }

        private async Task<QueuedEmailAttachment> CreatePdfInvoiceAttachmentAsync(int orderId)
        {
            QueuedEmailAttachment attachment = null;
            var request = _urlHelper.ActionContext.HttpContext.Request;
            var path = _urlHelper.Action("Print", "Order", new { id = orderId, pdf = true, area = string.Empty });
            var url = WebHelper.GetAbsoluteUrl(path, request);
            
            var downloadRequest = WebRequest.CreateHttp(url);
            downloadRequest.UserAgent = "Smartstore";
            downloadRequest.Timeout = 5000;
            downloadRequest.SetAuthenticationCookie(request);
            downloadRequest.SetVisitorCookie(request);

            using (var response = (HttpWebResponse)await downloadRequest.GetResponseAsync())
            using (var stream = response.GetResponseStream())
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    attachment = new QueuedEmailAttachment
                    {
                        StorageLocation = EmailAttachmentStorageLocation.Blob,
                        MimeType = response.ContentType,
                        MediaStorage = new MediaStorage
                        {
                            // INFO: stream.Length not supported here.
                            Data = await stream.ToByteArrayAsync()
                        }
                    };

                    var contentDisposition = response.Headers["Content-Disposition"];
                    if (contentDisposition.HasValue())
                    {
                        attachment.Name = new ContentDisposition(contentDisposition).FileName;
                    }

                    if (attachment.Name.IsEmpty())
                    {
                        attachment.Name = WebHelper.GetFileNameFromUrl(url);
                    }
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