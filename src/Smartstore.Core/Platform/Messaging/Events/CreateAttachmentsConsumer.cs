using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Events;
using Smartstore.Net;

namespace Smartstore.Core.Messages.Events
{
    public class CreateAttachmentsConsumer : IConsumer
    {
        private readonly PdfSettings _pdfSettings;
        private readonly Lazy<DownloadManager> _downloadManager;
        private readonly Lazy<IUrlHelper> _urlHelper;

        public CreateAttachmentsConsumer(
            PdfSettings pdfSettings,
            Lazy<DownloadManager> downloadManager,
            Lazy<IUrlHelper> urlHelper)
        {
            _pdfSettings = pdfSettings;
            _downloadManager = downloadManager;
            _urlHelper = urlHelper;

            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

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
            var path = _urlHelper.Value.Action("Print", "Order", new { id = orderId, pdf = true, area = "" });
            var fileResponse = await _downloadManager.Value.DownloadFileAsync(path, true, 5000);

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
