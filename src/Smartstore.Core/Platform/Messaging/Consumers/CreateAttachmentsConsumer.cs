using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging.Events;
using Smartstore.Events;
using Smartstore.Net;

namespace Smartstore.Core.Messaging
{
    internal class CreateAttachmentsConsumer : IConsumer
    {
        public ILogger Logger { get; set; } = NullLogger.Instance;
        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task HandleEventAsync(MessageQueuingEvent message, PdfSettings pdfSettings, IUrlHelper urlHelper)
        {
            var qe = message.QueuedEmail;
            var ctx = message.MessageContext;
            var model = message.MessageModel;

            var handledTemplates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                { "OrderPlaced.CustomerNotification", pdfSettings.AttachOrderPdfToOrderPlacedEmail },
                { "OrderCompleted.CustomerNotification", pdfSettings.AttachOrderPdfToOrderCompletedEmail }
            };

            if (handledTemplates.TryGetValue(ctx.MessageTemplate.Name, out var shouldHandle) && shouldHandle)
            {
                if (model.Get("Order") is IDictionary<string, object> order && order.Get("ID") is int orderId)
                {
                    try
                    {
                        var qea = await CreatePdfInvoiceAttachmentAsync(orderId, urlHelper);
                        qe.Attachments.Add(qea);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, T("Admin.System.QueuedEmails.ErrorCreatingAttachment"));
                    }
                }
            }
        }

        private async Task<QueuedEmailAttachment> CreatePdfInvoiceAttachmentAsync(int orderId, IUrlHelper urlHelper)
        {
            // TODO: (mh) (core) Ensure that this path is correct after all routes has been ported.
            var path = urlHelper.Action("Print", "Order", new { id = orderId, pdf = true, area = "" });
            var downloadManager = new DownloadManager(urlHelper.ActionContext.HttpContext.Request);
            var fileResponse = await downloadManager.DownloadFileAsync(path, true, 5000);

            if (fileResponse == null)
            {
                throw new InvalidOperationException(T("Admin.System.QueuedEmails.ErrorEmptyAttachmentResult", path));
            }

            if (fileResponse.ContentType != "application/pdf")
            {
                throw new InvalidOperationException(T("Admin.System.QueuedEmails.ErrorNoPdfAttachment"));
            }

            return new QueuedEmailAttachment
            {
                StorageLocation = EmailAttachmentStorageLocation.Blob,
                MediaStorage = new MediaStorage { Data = fileResponse.Data },
                MimeType = fileResponse.ContentType,
                Name = fileResponse.FileName
            };
        }
    }
}