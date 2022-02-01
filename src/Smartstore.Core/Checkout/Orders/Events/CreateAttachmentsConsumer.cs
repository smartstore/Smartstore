using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Messaging.Events;
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Orders.Events
{
    internal class CreateAttachmentsConsumer : IConsumer
    {
        private readonly PdfInvoiceHttpClient _client;
        private readonly PdfSettings _pdfSettings;

        public CreateAttachmentsConsumer(PdfInvoiceHttpClient client, PdfSettings pdfSettings)
        {
            _client = client;
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
                { MessageTemplateNames.OrderPlacedCustomer, _pdfSettings.AttachOrderPdfToOrderPlacedEmail },
                { MessageTemplateNames.OrderCompletedCustomer, _pdfSettings.AttachOrderPdfToOrderCompletedEmail }
            };

            if (handledTemplates.TryGetValue(ctx.MessageTemplate.Name, out var shouldHandle) && shouldHandle)
            {
                if (model.Get("Order") is IDictionary<string, object> order && order.Get("ID") is int orderId)
                {
                    try
                    {
                        var result = await _client.GetPdfInvoiceAsync(orderId);
                        var qea = new QueuedEmailAttachment
                        {
                            StorageLocation = EmailAttachmentStorageLocation.Blob,
                            MimeType = result.MimeType,
                            Name = result.FileName,
                            MediaStorage = new MediaStorage { Data = result.Buffer }
                        };
                        qe.Attachments.Add(qea);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, T("Admin.System.QueuedEmails.ErrorCreatingAttachment"));
                    }
                }
            }
        }
    }
}