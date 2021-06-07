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
            var attachment = new QueuedEmailAttachment
            {
                StorageLocation = EmailAttachmentStorageLocation.Blob
            };

            var path = _urlHelper.Action("Print", "Order", new { id = orderId, pdf = true, area = "" });

            var success = await DownloadManager.DownloadFileAsync(async response =>
            {
                attachment.Name = response.FileName;
                attachment.MimeType = response.ContentType;
                attachment.MediaStorage = new MediaStorage
                {
                    // INFO: response.Stream.Length not supported.
                    Data = await response.Stream.ToByteArrayAsync()
                };

                return attachment.MediaStorage.Data?.Length > 0;
            }, 
            path, _urlHelper.ActionContext.HttpContext.Request, 5000, true);

            if (!success)
            {
                throw new InvalidOperationException(T("Admin.System.QueuedEmails.ErrorEmptyAttachmentResult", path));
            }

            if (!attachment.MimeType.EqualsNoCase("application/pdf"))
            {
                throw new InvalidOperationException(T("Admin.System.QueuedEmails.ErrorNoPdfAttachment"));
            }

            return attachment;
        }
    }
}